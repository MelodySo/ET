using System;
using ET.Server;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ET.Test
{
    public class Login_AccountPersistence_Test : ATestHandler
    {
        private const string TestPassword = "LoginTest-Password1";

        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, SceneType.TestEmpty,
                nameof(Login_AccountPersistence_Test));
            string connection = scope.TestFiber.GetSingleton<StartZoneConfigCategory>().Get(scope.TestFiber.Zone).DBConnection;
            string databaseName = $"LoginAccountTest_{Guid.NewGuid():N}";
            MongoClient client = new(connection);
            IMongoDatabase database = client.GetDatabase(databaseName);
            try
            {
                await LoginAccountHelper.InitializeAsync(database);
                var first = await LoginAccountHelper.AuthenticateAsync(database, "  Demo_Player  ", TestPassword);
                if (first.Error != ErrorCode.ERR_Success || first.AccountId <= 0)
                {
                    Log.Console("First login did not create a persistent account.");
                    return 1;
                }

                var again = await LoginAccountHelper.AuthenticateAsync(database, "demo_player", TestPassword);
                if (again.Error != ErrorCode.ERR_Success || again.AccountId != first.AccountId)
                {
                    Log.Console("Normalized login did not preserve account identity.");
                    return 2;
                }

                var wrong = await LoginAccountHelper.AuthenticateAsync(database, "demo_player", "Wrong-Password1");
                if (wrong.Error != ErrorCode.ERR_LoginCredentials || wrong.AccountId != 0)
                {
                    Log.Console("Wrong password was not rejected.");
                    return 3;
                }

                IMongoCollection<LoginAccount> accounts = database.GetCollection<LoginAccount>(nameof(LoginAccount));
                if (await accounts.CountDocumentsAsync(FilterDefinition<LoginAccount>.Empty) != 1)
                {
                    Log.Console("Repeated or failed login created duplicate accounts.");
                    return 4;
                }

                LoginAccount record = await accounts.Find(a => a.Account == "demo_player").SingleAsync();
                BsonDocument raw = await database.GetCollection<BsonDocument>(nameof(LoginAccount))
                    .Find(new BsonDocument("Account", "demo_player")).SingleAsync();
                if (raw.ToJson().Contains(TestPassword) || raw.Contains("Password") || record.PasswordSalt.Length == 0 ||
                    record.PasswordHash.Length == 0 || record.PasswordIterations < LoginAccountPolicy.PasswordIterations)
                {
                    Log.Console("Stored credentials are not salted password hashes.");
                    return 5;
                }

                var second = await LoginAccountHelper.AuthenticateAsync(database, "second_player", TestPassword);
                LoginAccount secondRecord = await accounts.Find(a => a.Account == "second_player").SingleAsync();
                if (second.Error != ErrorCode.ERR_Success || second.AccountId == first.AccountId ||
                    record.PasswordSalt == secondRecord.PasswordSalt || record.PasswordHash == secondRecord.PasswordHash)
                {
                    Log.Console("Different accounts did not receive independent identities and salts.");
                    return 6;
                }

                // 新建访问对象，不借助任何内存账号缓存，验证数据可以重新加载。
                IMongoDatabase reopened = new MongoClient(connection).GetDatabase(databaseName);
                await LoginAccountHelper.InitializeAsync(reopened);
                var reloaded = await LoginAccountHelper.AuthenticateAsync(reopened, "demo_player", TestPassword);
                if (reloaded.Error != ErrorCode.ERR_Success || reloaded.AccountId != first.AccountId)
                {
                    Log.Console("Reloaded database did not preserve account identity.");
                    return 7;
                }

                return ErrorCode.ERR_Success;
            }
            finally
            {
                await client.DropDatabaseAsync(databaseName);
            }
        }
    }
}
