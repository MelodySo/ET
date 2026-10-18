namespace ET.Server
{
    public sealed class BTBrawlAttackPositionHandler : ABTHandler<BTBrawlAttackPosition>
    {
        protected override int Run(BTBrawlAttackPosition node, BTEnv env)
            => BrawlAICombatHelper.AtAttackPosition(env.GetEntity<Unit>(node.Unit), node) ? 0 : 1;
    }
}
