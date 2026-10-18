using Unity.Mathematics;
namespace ET.Server
{
    public static class BrawlAIHelper
    {
        public static void Tick(Unit unit, int levelId, float cellSize, long targetId, long now)
        {
            // AI is optional: only the configured practice Bot owns this component.
            BrawlAIComponent ai = unit.GetComponent<BrawlAIComponent>();
            if (ai == null) return;
            ai.LevelId = levelId; ai.CellSize = cellSize; ai.TargetId = targetId; ai.Now = now;
            unit.GetComponent<BrawlMoveComponent>().DesiredDirection = default;
            if (BrawlUnitLifeHelper.CannotAct(unit) || GetTarget(unit) == null) { Idle(unit); return; }
            if (!unit.Fiber().GetSingleton<BrawlAIConfigCategory>().Trees.TryGetValue(unit.ConfigId, out BrawlAIRoot tree))
            { Idle(unit); return; }
            using BTEnv env = BTEnv.Create(unit.Scene(), unit.Id);
            env.AddEntity(tree.Unit, unit);
            if (BTHelper.RunTree(tree, env) != 0) Idle(unit);
        }

        public static Unit GetTarget(Unit unit)
        {
            BrawlAIComponent ai = unit.GetComponent<BrawlAIComponent>();
            Unit target = unit.Scene().GetComponent<UnitComponent>().Get(ai.TargetId);
            if (target == null || target.Id == unit.Id || target.GetComponent<BrawlMoveComponent>() == null) return null;
            StateComponent states = target.GetComponent<StateComponent>();
            return states.Has(StateId.Dead) ? null : target;
        }

        public static void Idle(Unit unit)
        {
            BrawlAIComponent ai = unit.GetComponent<BrawlAIComponent>();
            ai.Path.Clear(); ai.Waypoint = 0; ai.NextPlanAt = 0; ai.PlannedTargetId = 0; ai.Chasing = false;
            unit.GetComponent<BrawlMoveComponent>().DesiredDirection = default;
        }

        public static void Chase(Unit unit, BTBrawlChase node)
        {
            Unit target = GetTarget(unit);
            if (target == null || BrawlUnitLifeHelper.CannotAct(unit)) { Idle(unit); return; }
            BrawlAIComponent ai = unit.GetComponent<BrawlAIComponent>();
            BrawlMoveComponent move = unit.GetComponent<BrawlMoveComponent>();
            BrawlLevelConfig level = unit.Fiber().GetSingleton<BrawlLevelConfigCategory>().Get(ai.LevelId);
            UnitComponent units = unit.Scene().GetComponent<UnitComponent>();
            ai.Chasing = true;
            if (ai.Now >= ai.NextPlanAt || ai.PlannedTargetId != target.Id)
            {
                ai.NextPlanAt = ai.Now + math.max(BrawlMovementSettings.TickMilliseconds, node.RepathMilliseconds);
                ai.PlannedTargetId = target.Id; ai.Waypoint = 0; ++ai.Plans;
                BrawlPathfindingHelper.Find(units, unit, level, ai.CellSize, target.Position.xz, node.MaxSearchNodes, ai.Path);
            }
            if (ai.Waypoint >= ai.Path.Count) return;
            float2 position = unit.Position.xz;
            while (ai.Waypoint < ai.Path.Count && math.distancesq(position, ai.Path[ai.Waypoint]) < 0.000001f) ++ai.Waypoint;
            if (ai.Waypoint >= ai.Path.Count) return;
            // Skip corners only when the same movement collision solver permits the entire segment.
            while (ai.Waypoint + 1 < ai.Path.Count && BrawlPathfindingHelper.CanTraverse(units, unit.Id, level,
                       ai.CellSize, position, ai.Path[ai.Waypoint + 1], move.Radius)) ++ai.Waypoint;
            float2 delta = ai.Path[ai.Waypoint] - position;
            float length = math.length(delta);
            float step = move.Speed * BrawlMovementSettings.TickSeconds;
            if (step <= 0 || length <= 0) return;
            move.DesiredDirection = delta / math.max(length, step);
            // No attacks, spell calls, facing changes or direct Position assignment here.
        }
    }
}
