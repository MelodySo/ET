namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public sealed class G2C_BrawlSkillUseHandler : MessageHandler<Scene, G2C_BrawlSkillUse>
    {
        protected override async ETTask Run(Scene scene, G2C_BrawlSkillUse message)
        {
            BrawlPracticeClientComponent client = scene.GetComponent<BrawlPracticeClientComponent>();
            if (client != null) BrawlSkillUseViewHelper.Receive(client.Session, message);
            await ETTask.CompletedTask;
        }
    }
}
