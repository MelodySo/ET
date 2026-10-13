namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public sealed class C2G_BrawlMoveInputHandler : MessageSessionHandler<C2G_BrawlMoveInput>
    {
        protected override async ETTask Run(Session session, C2G_BrawlMoveInput message)
        {
            BrawlPracticeMovementHelper.SetInput(session, message);
            await ETTask.CompletedTask;
        }
    }
}
