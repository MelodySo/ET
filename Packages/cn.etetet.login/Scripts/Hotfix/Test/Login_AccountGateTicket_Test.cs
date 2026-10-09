using ET.Server;

namespace ET.Test
{
    public class Login_AccountGateTicket_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, SceneType.TestEmpty,
                nameof(Login_AccountGateTicket_Test));
            Scene root = scope.TestFiber.Root;
            root.AddComponent<TimerComponent>();
            GateSessionKeyComponent keys = root.AddComponent<GateSessionKeyComponent>();
            long key = IdGenerater.Instance.GenerateId();
            long accountId = IdGenerater.Instance.GenerateId();
            keys.Add(key, "ticket_player", accountId);
            var ticket = keys.Consume(key);
            if (ticket.Account != "ticket_player" || ticket.AccountId != accountId)
            {
                Log.Console("Gate ticket did not carry the authenticated identity.");
                return 1;
            }
            if (keys.Consume(key).AccountId != 0)
            {
                Log.Console("Gate ticket was accepted more than once.");
                return 2;
            }
            if (keys.Consume(IdGenerater.Instance.GenerateId()).AccountId != 0)
            {
                Log.Console("Unknown Gate ticket was accepted.");
                return 3;
            }
            keys.Add(key, "ticket_player", accountId);
            keys.Remove(key);
            if (keys.Consume(key).AccountId != 0)
            {
                Log.Console("Removed Gate ticket was accepted.");
                return 4;
            }
            return ErrorCode.ERR_Success;
        }
    }
}
