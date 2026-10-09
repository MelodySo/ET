namespace ET
{
    public static partial class ErrorCode
    {
        public const int ERR_ConnectGateKeyError = ErrorCode.ERR_WithException + PackageType.Login * 1000 + 1; // 100009001
        public const int ERR_LoginInvalidInput = ERR_WithoutException + PackageType.Login * 1000 + 1;
        public const int ERR_LoginCredentials = ERR_WithoutException + PackageType.Login * 1000 + 2;
        public const int ERR_LoginUnavailable = ERR_WithoutException + PackageType.Login * 1000 + 3;
    }
}
