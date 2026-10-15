namespace ET.Server
{
    public sealed class BTBrawlIdleHandler : ABTHandler<BTBrawlIdle>
    {
        protected override int Run(BTBrawlIdle node, BTEnv env)
        {
            BrawlAIHelper.Idle(env.GetEntity<Unit>(node.Unit));
            return 0;
        }
    }
}