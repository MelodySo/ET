namespace ET
{
    public static class LoginAccountInputHelper
    {
        public static string Normalize(string account)
        {
            return account?.Trim().ToLowerInvariant();
        }

        public static bool IsValid(string account, string password)
        {
            if (account == null || account.Length < LoginAccountPolicy.MinAccountLength ||
                account.Length > LoginAccountPolicy.MaxAccountLength || string.IsNullOrWhiteSpace(password) ||
                password.Length < LoginAccountPolicy.MinPasswordLength || password.Length > LoginAccountPolicy.MaxPasswordLength)
            {
                return false;
            }

            foreach (char c in account)
            {
                if ((c < 'a' || c > 'z') && (c < '0' || c > '9') && c != '_') return false;
            }
            return true;
        }

        public static string ErrorMessage(int error)
        {
            return error switch
            {
                ErrorCode.ERR_LoginInvalidInput => $"账号需为 {LoginAccountPolicy.MinAccountLength}–{LoginAccountPolicy.MaxAccountLength} 位英文字母、数字或下划线；密码需为 {LoginAccountPolicy.MinPasswordLength}–{LoginAccountPolicy.MaxPasswordLength} 位。",
                ErrorCode.ERR_LoginCredentials => "账号或密码错误。",
                ErrorCode.ERR_LoginUnavailable => "账号服务暂时不可用，请稍后重试。",
                _ => "登录失败，请重试。",
            };
        }
    }
}
