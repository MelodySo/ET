namespace ET.Server
{
    [MessageSessionHandler(SceneType.Gate)]
    public sealed class C2G_BrawlNormalAttackHandler : MessageSessionHandler<C2G_BrawlNormalAttack>
    {
        protected override async ETTask Run(Session session, C2G_BrawlNormalAttack message)
        {
            BrawlSkillCastHelper.Cast(session, message);
            await ETTask.CompletedTask;
        }
    }
}

