using System;


namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public class C2G_LoginGateHandler : MessageSessionHandler<C2G_LoginGate, G2C_LoginGate>
    {
        protected override async ETTask Run(Session session, C2G_LoginGate request, G2C_LoginGate response)
        {
            Scene root = session.Root();
            if (request.GateId != root.Id || session.GetComponent<SessionPlayerComponent>() != null)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                return;
            }
            var ticket = root.GetComponent<GateSessionKeyComponent>().Consume(request.Key);
            string account = ticket.Account;
            if (account == null || ticket.AccountId <= 0)
            {
                response.Error = ErrorCode.ERR_ConnectGateKeyError;
                response.Message = "Gate key验证失败!";
                return;
            }
            
            session.RemoveComponent<SessionAcceptTimeoutComponent>();

            PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
            Player player = playerComponent.GetByAccount(account);
            EntityRef<Session> sessionRef = session;
            
            if (player == null)
            {
                player = playerComponent.AddChildWithId<Player, string>(ticket.AccountId, account);
                EntityRef<Player> playerRef = player;
                playerComponent.Add(player);
                PlayerSessionComponent playerSessionComponent = player.AddComponent<PlayerSessionComponent>();
                playerSessionComponent.AddComponent<MailBoxComponent, int>(MailBoxType.GateSession);
                
                player = playerRef;
                session = sessionRef;
                session.AddComponent<SessionPlayerComponent>().Player = player;
                playerSessionComponent.Session = session;
            }
            else
            {
                // 先解除旧连接关联，避免其关闭回调将新连接对应的 Player 标记为下线。
                Session previousSession = player.GetComponent<PlayerSessionComponent>().Session;
                if (previousSession != null && previousSession != session)
                {
                    previousSession.GetComponent<SessionPlayerComponent>().Player = null;
                    previousSession.Dispose();
                }
                player.RemoveComponent<WaitLogoutComponent>();
                session.AddComponent<SessionPlayerComponent>().Player = player;
                player.GetComponent<PlayerSessionComponent>().Session = session;
            }

            response.PlayerId = player.Id;
            await ETTask.CompletedTask;
        }
    }
}
