namespace ET
{
    public static class LoginAccountPolicy
    {
        public const int MinAccountLength = 3;
        public const int MaxAccountLength = 32;
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 128;
        public const int PasswordIterations = 600000;
        public const int PasswordSaltBytes = 16;
        public const int PasswordHashBytes = 32;
        public const int GateKeyLifetimeMilliseconds = 20000;
    }
}
