using System;
using System.Security.Cryptography;

namespace ET.Server
{
    public static class LoginPasswordHelper
    {
        public static (string Salt, string Hash) Hash(string password)
        {
            byte[] salt = new byte[LoginAccountPolicy.PasswordSaltBytes];
            using (RandomNumberGenerator random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            byte[] hash = Derive(password, salt, LoginAccountPolicy.PasswordIterations);
            return (Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool Verify(string password, LoginAccount account)
        {
            // 只接受当前版本写入的参数，损坏的数据不能造成无界 CPU 消耗。
            if (account.PasswordIterations != LoginAccountPolicy.PasswordIterations) return false;
            try
            {
                byte[] salt = Convert.FromBase64String(account.PasswordSalt);
                byte[] expected = Convert.FromBase64String(account.PasswordHash);
                if (salt.Length != LoginAccountPolicy.PasswordSaltBytes || expected.Length != LoginAccountPolicy.PasswordHashBytes)
                    return false;
                byte[] actual = Derive(password, salt, account.PasswordIterations);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
#if DOTNET
            return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, LoginAccountPolicy.PasswordHashBytes);
#else
            using Rfc2898DeriveBytes derive = new(password, salt, iterations, HashAlgorithmName.SHA256);
            return derive.GetBytes(LoginAccountPolicy.PasswordHashBytes);
#endif
        }
    }
}
