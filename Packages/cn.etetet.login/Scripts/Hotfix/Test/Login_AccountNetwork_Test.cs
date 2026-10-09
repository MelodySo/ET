using System;
using System.Collections.Generic;
using ET.Client;
using ET.Server;
using MongoDB.Driver;

namespace ET.Test
{
    [TestExecution(TestExecutionMode.Exclusive)]
    public class Login_AccountNetwork_Test : ATestHandler
    {
        private const string TestPassword = "NetworkTest-Password1";

        public override async ETTask<int> Handle(TestContext context)
        {
            TestFiberScope holder = await TestFiberScope.Create(context.Fiber, SceneType.TestEmpty, nameof(Login_AccountNetwork_Test));
            string databasePrefix = $"LoginAccountTest_{Guid.NewGuid():N}";
            Dictionary<int, StartZoneConfig> activeZones = holder.TestFiber.GetSingleton<StartZoneConfigCategory>().GetAll();
            Dictionary<int, StartZoneConfig> originalZones = new(activeZones);
            Dictionary<int, StartZoneConfig> zones = new();
            foreach (StartZoneConfig config in originalZones.Values)
            {
                zones.Add(config.Id, new StartZoneConfig(config.Id, config.ZoneType, config.DBConnection, databasePrefix));
            }
            // 独占测试进程内替换数据，不修改配置文件；销毁全部场景后恢复。
            foreach (var pair in zones) activeZones[pair.Key] = pair.Value;
            try
            {
                Fiber server = await holder.TestFiber.CreateFiber(holder.TestFiber.Zone, IdGenerater.Instance.GenerateId(),
                    SceneType.TestCase, "LoginAccountNetworkServer");
                Fiber client = await server.CreateFiber(IdGenerater.Instance.GenerateId(), SceneType.Client, "AccountNetworkClient");
                string address = TestHelper.GetRouterManagerAddress(server);
                EntityRef<Scene> clientRootRef = client.Root;
                await LoginHelper.Login(client.Root, address, "network_player", TestPassword);
                Scene clientRoot = clientRootRef;
                long accountId = clientRoot.GetComponent<ET.Client.PlayerComponent>().MyId;
                if (accountId <= 0)
                {
                    Log.Console("Network login did not return an account ID.");
                    return 1;
                }

                Fiber realm = server.GetFiber("Realm");
                IMongoDatabase database = realm.Root.GetComponent<DBManagerComponent>().GetZoneDB(realm.Zone).database;
                if (!database.DatabaseNamespace.DatabaseName.StartsWith(databasePrefix, StringComparison.Ordinal))
                    throw new Exception("Network test database isolation failed.");
                LoginAccount record = await database.GetCollection<LoginAccount>(nameof(LoginAccount))
                    .Find(a => a.Account == "network_player").SingleAsync();
                if (record.Id != accountId)
                {
                    Log.Console("Gate player ID differs from the persisted account ID.");
                    return 2;
                }

                // 一个全新的客户端尝试错误密码，不得返回身份或保留失败网络 Fiber。
                Fiber retryClient = await server.CreateFiber(IdGenerater.Instance.GenerateId(), SceneType.Client, "AccountRetryClient");
                EntityRef<Scene> retryRootRef = retryClient.Root;
                try
                {
                    await LoginHelper.Login(retryClient.Root, address, "network_player", "Wrong-Password1");
                    Log.Console("Wrong password passed network authentication.");
                    return 3;
                }
                catch (RpcException e) when (e.Error == ErrorCode.ERR_LoginCredentials)
                {
                }
                Scene retryRoot = retryRootRef;
                if (retryRoot.GetComponent<ClientSenderComponent>() != null || retryRoot.GetComponent<ET.Client.PlayerComponent>().MyId != 0)
                {
                    Log.Console("Failed login left an authenticated client or a sender component.");
                    return 4;
                }

                await LoginHelper.Login(retryRoot, address, "NETWORK_PLAYER", TestPassword);
                retryRoot = retryRootRef;
                if (retryRoot.GetComponent<ET.Client.PlayerComponent>().MyId != accountId)
                {
                    Log.Console("Retry login changed account identity.");
                    return 5;
                }
                await EnterMapHelper.EnterMapAsync(retryRoot);
                Unit unit = TestHelper.GetServerUnit(server, retryClient);
                if (unit.Id != accountId)
                {
                    Log.Console("Entering the existing map lost the authenticated identity.");
                    return 6;
                }

                // 用既有机器人辅助入口做回归，确认测试账号也遵守正式密码规则。
                await TestHelper.CreateRobot(server, nameof(Login_AccountNetwork_Test));
                return ErrorCode.ERR_Success;
            }
            finally
            {
                try
                {
                    await holder.DisposeAsync();
                }
                finally
                {
                    activeZones.Clear();
                    foreach (var pair in originalZones) activeZones.Add(pair.Key, pair.Value);
                }
                // 仅清理本次随机前缀下的数据库，场景完全销毁后执行。
                foreach (StartZoneConfig config in zones.Values)
                {
                    if (string.IsNullOrEmpty(config.DBConnection)) continue;
                    string name = $"{databasePrefix}_zone_{config.Id:D4}";
                    await new MongoClient(config.DBConnection).DropDatabaseAsync(name);
                }
            }
        }
    }
}
