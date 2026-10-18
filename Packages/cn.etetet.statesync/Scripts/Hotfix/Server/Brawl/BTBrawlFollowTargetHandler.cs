namespace ET.Server
{
    public sealed class BTBrawlFollowTargetHandler : ABTHandler<BTBrawlFollowTarget>
    {
        protected override int Run(BTBrawlFollowTarget node, BTEnv env)
        {
            BrawlAICombatHelper.FollowTarget(env.GetEntity<Unit>(node.Unit), node);
            return 0;
        }
    }
}
