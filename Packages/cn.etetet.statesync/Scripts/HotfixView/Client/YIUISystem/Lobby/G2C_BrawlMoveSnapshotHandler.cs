namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public sealed class G2C_BrawlMoveSnapshotHandler : MessageHandler<Scene, G2C_BrawlMoveSnapshot>
    {
        protected override async ETTask Run(Scene scene, G2C_BrawlMoveSnapshot message)
        {
            // 进入前或退出后收到的快照合法地没有接收者。
            BrawlPracticeClientComponent client = scene.GetComponent<BrawlPracticeClientComponent>();
            if (client != null) BrawlPracticeMovementViewHelper.Receive(client.Session, message);
            await ETTask.CompletedTask;
        }
    }
}
