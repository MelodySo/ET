using Unity.Mathematics;
namespace ET.Server
{
    public sealed class BTBrawlPlayerWithinDistanceHandler : ABTHandler<BTBrawlPlayerWithinDistance>
    {
        protected override int Run(BTBrawlPlayerWithinDistance node, BTEnv env)
        {
            Unit unit = env.GetEntity<Unit>(node.Unit);
            Unit target = BrawlAIHelper.GetTarget(unit);
            return target != null && math.isfinite(node.Distance) && node.Distance >= 0 &&
                math.distancesq(unit.Position.xz, target.Position.xz) <= node.Distance * node.Distance ? 0 : 1;
        }
    }
}