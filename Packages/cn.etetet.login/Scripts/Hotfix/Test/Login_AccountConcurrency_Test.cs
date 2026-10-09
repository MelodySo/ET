using System;
using System.Collections.Generic;
using ET.Server;
using MongoDB.Driver;

namespace ET.Test
{
    public class Login_AccountConcurrency_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, SceneType.TestEmpty,
                nameof(Login_AccountConcurrency_Test));
            string connection = scope.TestFiber.GetSingleton<StartZoneConfigCategory>().Get(scope.TestFiber.Zone).DBConnection;
            string databaseName = $"LoginAccountTest_{Guid.NewGuid():N}";
            MongoClient client = new(connection);
            IMongoDatabase database = client.GetDatabase(databaseName);
            try
            {
                await LoginAccountHelper.InitializeAsync(database);
                List<(int Error, long AccountId)> results = new();
                await ETTask.WaitAll(new List<ETTask>
                {
                    LoginAsync(database, "Concurrent-Password1", results),
                    LoginAsync(database, "Concurrent-Password2", results),
                });
                int successes = 0;
                int failures = 0;
                foreach (var result in results)
                {
                    if (result.Error == ErrorCode.ERR_Success && result.AccountId > 0) ++successes;
                    if (result.Error == ErrorCode.ERR_LoginCredentials && result.AccountId == 0) ++failures;
                }
                if (successes != 1 || failures != 1)
                {
                    Log.Console("Concurrent first logins with different passwords must have exactly one winner.");
                    return 1;
                }

                IMongoCollection<LoginAccount> accounts = database.GetCollection<LoginAccount>(nameof(LoginAccount));
                if (await accounts.CountDocumentsAsync(FilterDefinition<LoginAccount>.Empty) != 1)
                {
                    Log.Console("Unique account index did not prevent duplicates.");
                    return 2;
                }

                // 直接插入重复账号，确认唯一性由 MongoDB 而不只是应用层查询保证。
                LoginAccount record = await accounts.Find(FilterDefinition<LoginAccount>.Empty).SingleAsync();
                record.Id = IdGenerater.Instance.GenerateId();
                try
                {
                    await accounts.InsertOneAsync(record);
                    Log.Console("Database accepted a duplicate account name.");
                    return 3;
                }
                catch (MongoWriteException e) when (e.WriteError.Category == ServerErrorCategory.DuplicateKey)
                {
                }
                return ErrorCode.ERR_Success;
            }
            finally
            {
                await client.DropDatabaseAsync(databaseName);
            }
        }

        private static async ETTask LoginAsync(IMongoDatabase database, string password, List<(int Error, long AccountId)> results)
        {
            results.Add(await LoginAccountHelper.AuthenticateAsync(database, "concurrent_player", password));
        }
    }
}
