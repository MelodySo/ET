using System;
using ET.Server;
using MongoDB.Driver;

namespace ET.Test
{
    public class Login_AccountValidation_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            // 不可达端点只用于模拟断库，不停止或修改正在运行的 MongoDB。
            MongoClientSettings settings = new()
            {
                Server = new MongoServerAddress("127.0.0.1", 1),
                ServerSelectionTimeout = TimeSpan.FromMilliseconds(300),
                ConnectTimeout = TimeSpan.FromMilliseconds(300),
            };
            IMongoDatabase unavailable = new MongoClient(settings).GetDatabase("LoginAccountTest_Unavailable");
            (string Account, string Password)[] invalid =
            {
                (null, "Valid-Password1"), ("", "Valid-Password1"), ("ab", "Valid-Password1"),
                (new string('a', LoginAccountPolicy.MaxAccountLength + 1), "Valid-Password1"),
                ("invalid/name", "Valid-Password1"), ("valid_player", null), ("valid_player", ""),
                ("valid_player", "short"), ("valid_player", new string('p', LoginAccountPolicy.MaxPasswordLength + 1)),
            };
            for (int i = 0; i < invalid.Length; ++i)
            {
                var result = await LoginAccountHelper.AuthenticateAsync(unavailable, invalid[i].Account, invalid[i].Password);
                if (result.Error != ErrorCode.ERR_LoginInvalidInput || result.AccountId != 0)
                {
                    Log.Console($"Invalid input was accepted at index {i}.");
                    return i + 1;
                }
            }

            var failed = await LoginAccountHelper.AuthenticateAsync(unavailable, "valid_player", "Valid-Password1");
            if (failed.Error != ErrorCode.ERR_LoginUnavailable || failed.AccountId != 0)
            {
                Log.Console("Unavailable database did not fail closed.");
                return 20;
            }

            using C2R_Login request = C2R_Login.Create();
            request.Password = "DoNotLogThisPassword";
            using Main2NetClient_Login internalRequest = Main2NetClient_Login.Create();
            internalRequest.Password = request.Password;
            if (request.ToString().Contains(request.Password) || internalRequest.ToString().Contains(request.Password))
            {
                Log.Console("Login message text exposed credentials.");
                return 21;
            }
            return ErrorCode.ERR_Success;
        }
    }
}
