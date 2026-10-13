namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public sealed class C2G_BrawlPracticeExitHandler : MessageSessionHandler<C2G_BrawlPracticeExit, G2C_BrawlPracticeExit>
    {
        protected override async ETTask Run(Session session, C2G_BrawlPracticeExit request, G2C_BrawlPracticeExit response)
        {
            BrawlPracticeHelper.Exit(session, request.EntryId);
            await ETTask.CompletedTask;
        }
    }
}
