using System;

namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public sealed class C2G_BrawlPracticeEnterHandler : MessageSessionHandler<C2G_BrawlPracticeEnter, G2C_BrawlPracticeEnter>
    {
        protected override async ETTask Run(Session session, C2G_BrawlPracticeEnter request, G2C_BrawlPracticeEnter response)
        {
            try { BrawlPracticeHelper.Enter(session, request, response); }
            catch (InvalidOperationException exception)
            {
                response.Error = ErrorCode.ERR_RpcFail;
                response.Message = exception.Message;
                Log.Warning($"Brawl practice rejected: {exception.Message}");
            }
            await ETTask.CompletedTask;
        }
    }
}
