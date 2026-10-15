using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>同一分轴步内同时约束地形与 NPC，避免先推地形再推 NPC 导致穿回另一障碍。</summary>
    public static class BrawlUnitMovementHelper
    {
        public static bool CanOccupy(UnitComponent units, long movingUnitId, float2 position, float radius)
        {
            foreach (Entity child in units.Children.Values)
            {
                Unit obstacle = (Unit)child;
                if (obstacle.Id == movingUnitId) continue;
                StateComponent states = obstacle.GetComponent<StateComponent>();
                if (states != null && (states.Has(StateId.Dead) || states.Has(StateId.Spawning))) continue;
                // 阻挡是可选能力；英雄和未配置阻挡的 NPC 不参与。
                BrawlPracticeUnitComponent block = obstacle.GetComponent<BrawlPracticeUnitComponent>();
                if (!block.BlocksMovement) continue;
                if (BrawlBlockGeometryHelper.Overlaps(position, radius, obstacle.Position.xz + block.CollisionPivot, block.CollisionLength, block.CollisionWidth))
                    return false;
            }
            return true;
        }

        public static float2 Move(UnitComponent units, BrawlLevelConfig level, float cellSize, long movingUnitId,
            float2 position, float2 displacement, float radius)
        {
            float maxStep = math.min(cellSize, radius) * 0.25f;
            int steps = math.max(1, (int)math.ceil(math.length(displacement) / maxStep));
            float2 step = displacement / steps;
            for (int i = 0; i < steps; ++i)
            {
                position.x = MoveAxis(units, level, cellSize, movingUnitId, position, step.x, radius, true);
                position.y = MoveAxis(units, level, cellSize, movingUnitId, position, step.y, radius, false);
            }
            return position;
        }

        private static float MoveAxis(UnitComponent units, BrawlLevelConfig level, float cellSize, long movingUnitId,
            float2 position, float delta, float radius, bool xAxis)
        {
            float current = xAxis ? position.x : position.y;
            if (delta == 0) return current;
            float target = BrawlTerrainMovementHelper.MoveAxis(level, cellSize, position, delta, radius, xAxis);
            foreach (Entity child in units.Children.Values)
            {
                Unit obstacle = (Unit)child;
                if (obstacle.Id == movingUnitId) continue;
                StateComponent states = obstacle.GetComponent<StateComponent>();
                if (states != null && (states.Has(StateId.Dead) || states.Has(StateId.Spawning))) continue;
                BrawlPracticeUnitComponent block = obstacle.GetComponent<BrawlPracticeUnitComponent>();
                if (!block.BlocksMovement) continue;
                target = BrawlBlockGeometryHelper.ClampAxis(position, target - current, radius,
                    obstacle.Position.xz + block.CollisionPivot, block.CollisionLength, block.CollisionWidth, xAxis);
            }
            return target;
        }
    }
}
