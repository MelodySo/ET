namespace ET.Server
{
    public sealed class BTBrawlChaseHandler : ABTHandler<BTBrawlChase>
    {
        protected override int Run(BTBrawlChase node, BTEnv env)
        {
            BrawlAIHelper.Chase(env.GetEntity<Unit>(node.Unit), node);
            return 0;
        }
    }
}