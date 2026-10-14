namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public sealed class G2C_BrawlProjectileSnapshotHandler : MessageHandler<Scene, G2C_BrawlProjectileSnapshot>
    {
        protected override async ETTask Run(Scene scene, G2C_BrawlProjectileSnapshot message)
        {
            BrawlPracticeClientComponent client = scene.GetComponent<BrawlPracticeClientComponent>();
            if (client != null) BrawlProjectileViewHelper.Receive(client.Session, message);
            await ETTask.CompletedTask;
        }
    }
}
