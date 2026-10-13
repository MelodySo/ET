using System;

namespace ET.Server
{
    [Invoke(SceneType.Gate)]
    public class NetComponentOnReadInvoker_Gate: AInvokeHandler<NetComponentOnRead>
    {
        public override void Handle(NetComponentOnRead args)
        {
            HandleAsync(args).Coroutine();
        }

        private async ETTask HandleAsync(NetComponentOnRead args)
        {
            Session session = args.Session;
            EntityRef<Session> sessionRef = session;
            object message = args.Message;
            Scene root = session.Root();

            MessageStatisticsComponent messageStatisticsComponent = session.GetComponent<MessageStatisticsComponent>();
            BrawlPracticeSessionComponent practice = session.GetComponent<BrawlPracticeSessionComponent>();
            bool allowed = message is C2G_BrawlMoveInput && practice != null
                ? BrawlPracticeMovementHelper.CheckInputRate(practice)
                : messageStatisticsComponent.Check(message.GetType(), 20);
            if (!allowed)
            {
                session.Error = ErrorCode.ERR_MessageCountTooMany;
                session.Dispose();
                return;
            }
            
            // 根据消息接口判断是不是Actor消息，不同的接口做不同的处理,比如需要转发给Chat Scene，可以做一个IChatMessage接口
            switch (message)
            {
                case ISessionMessage:
                {
                    MessageSessionDispatcher.Instance.Handle(session, message);
                    break;
                }
                case ILocationMessage actorLocationMessage:
                {
                    long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                    root.GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit).Send(unitId, actorLocationMessage);
                    break;
                }
                case ILocationRequest actorLocationRequest: // gate session收到actor rpc消息，先向actor 发送rpc请求，再将请求结果返回客户端
                {
                    long unitId = session.GetComponent<SessionPlayerComponent>().Player.Id;
                    int rpcId = actorLocationRequest.RpcId; // 这里要保存客户端的rpcId
                    IResponse iResponse = await root.GetComponent<MessageLocationSenderComponent>().Get(LocationType.Unit).Call(unitId, actorLocationRequest);
                    
                    iResponse.RpcId = rpcId;
                    // session可能已经断开了，所以这里需要判断
                    session = sessionRef;
                    if (session != null)
                    {
                        session.Send(iResponse);
                    }
                    break;
                }
                case IRequest actorRequest:  // 分发IActorRequest消息，目前没有用到，需要的自己添加
                {
                    break;
                }
                case IMessage actorMessage:  // 分发IActorMessage消息，目前没有用到，需要的自己添加
                {
                    break;
                }
				
                default:
                {
                    throw new Exception($"not found handler: {message}");
                }
            }
        }
    }
}
