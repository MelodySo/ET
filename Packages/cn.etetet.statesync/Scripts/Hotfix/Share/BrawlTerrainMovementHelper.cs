using Unity.Mathematics;

namespace ET
{
    /// <summary>纯 XZ 网格几何，不查询 Unity 物理、NPC 或模型缩放；玩家与 Bot 共用。</summary>
    public static class BrawlTerrainMovementHelper
    {
        public static bool IsBlocked(int tile) => tile == (int)BrawlTileType.Wall || tile == (int)BrawlTileType.Water;

        public static bool CanOccupy(BrawlLevelConfig level, float cellSize, float2 position, float radius)
        {
            float half = cellSize * 0.5f;
            float epsilon = BrawlMovementSettings.ContactTolerance;
            float2 minimum = new float2(-half + radius);
            float2 maximum = new float2(level.ColumnCount * cellSize - half - radius, level.RowCount * cellSize - half - radius);
            if (math.any(position < minimum - epsilon) || math.any(position > maximum + epsilon)) return false;
            int2 first = math.max((int2)math.floor((position - radius + half) / cellSize), 0);
            int2 last = math.min((int2)math.floor((position + radius + half) / cellSize), new int2(level.ColumnCount - 1, level.RowCount - 1));
            for (int row = first.y; row <= last.y; ++row)
            for (int column = first.x; column <= last.x; ++column)
            {
                if (!IsBlocked(level.MapGrid[row * level.ColumnCount + column])) continue;
                float2 center = new float2(column, row) * cellSize;
                float2 nearest = math.clamp(position, center - half, center + half);
                // 相切合法：直径一米的角色可以通过恰好一格宽的通道。
                if (math.lengthsq(position - nearest) < (radius - epsilon) * (radius - epsilon)) return false;
            }
            return true;
        }

        public static float2 Move(BrawlLevelConfig level, float cellSize, float2 position, float2 displacement, float radius)
        {
            // 每个分轴都是连续限位；再细分斜向位移，减少拐角处分轴顺序造成的偏差。
            float maxStep = math.min(cellSize, radius) * 0.25f;
            int steps = math.max(1, (int)math.ceil(math.length(displacement) / maxStep));
            float2 step = displacement / steps;
            for (int i = 0; i < steps; ++i)
            {
                position.x = MoveAxis(level, cellSize, position, step.x, radius, true);
                position.y = MoveAxis(level, cellSize, position, step.y, radius, false);
            }
            return position;
        }

        public static float MoveAxis(BrawlLevelConfig level, float cellSize, float2 position, float delta, float radius, bool xAxis)
        {
            float current = xAxis ? position.x : position.y;
            if (delta == 0) return current;
            float half = cellSize * 0.5f;
            float limit = (xAxis ? level.ColumnCount : level.RowCount) * cellSize - half - radius;
            float target = math.clamp(current + delta, -half + radius, limit);
            float2 end = position;
            if (xAxis) end.x = target;
            else end.y = target;
            int2 first = math.max((int2)math.floor((math.min(position, end) - radius + half) / cellSize), 0);
            int2 last = math.min((int2)math.floor((math.max(position, end) + radius + half) / cellSize), new int2(level.ColumnCount - 1, level.RowCount - 1));
            float perpendicular = xAxis ? position.y : position.x;
            float epsilon = BrawlMovementSettings.ContactTolerance;
            for (int row = first.y; row <= last.y; ++row)
            for (int column = first.x; column <= last.x; ++column)
            {
                if (!IsBlocked(level.MapGrid[row * level.ColumnCount + column])) continue;
                float otherCenter = (xAxis ? row : column) * cellSize;
                float distance = math.max(0, math.abs(perpendicular - otherCenter) - half);
                if (distance >= radius - epsilon) continue;
                // 圆与矩形的圆角接触距离，不把角色误当成正方形。
                float reach = math.sqrt(math.max(0, radius * radius - distance * distance));
                float center = (xAxis ? column : row) * cellSize;
                float near = center - half - reach;
                float far = center + half + reach;
                if (delta > 0 && current <= near + epsilon) target = math.min(target, math.max(current, near));
                else if (delta < 0 && current >= far - epsilon) target = math.max(target, math.min(current, far));
            }
            return target;
        }
    }
}
