namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public sealed class C2G_BrawlCastSkillHandler : MessageSessionHandler<C2G_BrawlCastSkill>
    {
        protected override async ETTask Run(Session session, C2G_BrawlCastSkill message)
        {
            BrawlSkillCastHelper.Cast(session, message);
            await ETTask.CompletedTask;
        }
    }
}

