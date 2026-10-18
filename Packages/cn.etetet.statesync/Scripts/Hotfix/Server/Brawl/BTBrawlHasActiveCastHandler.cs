namespace ET.Server
{
    public sealed class BTBrawlHasActiveCastHandler : ABTHandler<BTBrawlHasActiveCast>
    {
        protected override int Run(BTBrawlHasActiveCast node, BTEnv env)
            => env.GetEntity<Unit>(node.Unit).GetComponent<BrawlSkillCastComponent>().Launches != null ? 0 : 1;
    }
}
