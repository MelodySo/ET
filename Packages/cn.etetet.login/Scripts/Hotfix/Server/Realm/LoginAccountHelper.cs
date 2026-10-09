using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace ET.Server
{
    public static class LoginAccountHelper
    {
        public static async ETTask InitializeAsync(IMongoDatabase database)
        {
            IMongoCollection<LoginAccount> accounts = database.GetCollection<LoginAccount>(nameof(LoginAccount));
            await accounts.Indexes.CreateOneAsync(new CreateIndexModel<LoginAccount>(
                Builders<LoginAccount>.IndexKeys.Ascending(a => a.Account), new CreateIndexOptions { Unique = true }));
        }

        public static async ETTask<(int Error, long AccountId)> AuthenticateAsync(IMongoDatabase database, string account, string password)
        {
            account = LoginAccountInputHelper.Normalize(account);
            if (!LoginAccountInputHelper.IsValid(account, password)) return (ErrorCode.ERR_LoginInvalidInput, 0);

            try
            {
                IMongoCollection<LoginAccount> accounts = database.GetCollection<LoginAccount>(nameof(LoginAccount));
                LoginAccount record = await accounts.Find(a => a.Account == account).SingleOrDefaultAsync();
                if (record == null)
                {
                    // KDF 只在工作线程处理纯数据，不在工作线程访问 Entity / Fiber。
                    var credentials = await Task.Run(() => LoginPasswordHelper.Hash(password));
                    record = new LoginAccount
                    {
                        Id = IdGenerater.Instance.GenerateId(),
                        Account = account,
                        PasswordSalt = credentials.Salt,
                        PasswordHash = credentials.Hash,
                        PasswordIterations = LoginAccountPolicy.PasswordIterations,
                        CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    };
                    try
                    {
                        await accounts.InsertOneAsync(record);
                        return (ErrorCode.ERR_Success, record.Id);
                    }
                    catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
                    {
                        // 其他 Realm / 请求先创建成功时，必须读取胜者记录验证，禁止覆盖密码。
                        record = await accounts.Find(a => a.Account == account).SingleOrDefaultAsync();
                        if (record == null) return (ErrorCode.ERR_LoginUnavailable, 0);
                    }
                }

                bool valid = await Task.Run(() => LoginPasswordHelper.Verify(password, record));
                return valid ? (ErrorCode.ERR_Success, record.Id) : (ErrorCode.ERR_LoginCredentials, 0);
            }
            catch (MongoException)
            {
                Log.Warning("Account database request failed.");
                return (ErrorCode.ERR_LoginUnavailable, 0);
            }
            catch (TimeoutException)
            {
                Log.Warning("Account database request timed out.");
                return (ErrorCode.ERR_LoginUnavailable, 0);
            }
        }
    }
}
