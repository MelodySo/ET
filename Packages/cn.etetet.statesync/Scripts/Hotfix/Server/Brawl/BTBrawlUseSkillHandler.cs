namespace ET.Server
{
    public sealed class BTBrawlUseSkillHandler : ABTHandler<BTBrawlUseSkill>
    {
        protected override int Run(BTBrawlUseSkill node, BTEnv env)
            => BrawlAICombatHelper.TryUseSkill(env.GetEntity<Unit>(node.Unit), node.Slot) ? 0 : 1;
    }
}
