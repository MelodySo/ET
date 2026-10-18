using Unity.Mathematics;
namespace ET.Server
{
    /// <summary>固定宽度矩形视线，仅查逻辑墙体；不模拟发射器、子弹或破墙结果。</summary>
    public static class BrawlAISightHelper
    {
        public static bool IsClear(UnitComponent units, BrawlLevelConfig level, float cellSize,
            float2 start, float2 end, float width, BrawlCollisionRuleConfig[] rules, BrawlFaction sourceFaction)
        {
            float half = cellSize / 2;
            float2 offset = BrawlTerrainMovementHelper.GridOffset(level, cellSize);
            float2 minimum = math.min(start, end) - width / 2 + offset;
            float2 maximum = math.max(start, end) + width / 2 + offset;
            int minX = math.max(0, (int)math.ceil((minimum.x - half) / cellSize));
            int maxX = math.min(level.ColumnCount - 1, (int)math.floor((maximum.x + half) / cellSize));
            int minZ = math.max(0, (int)math.ceil((minimum.y - half) / cellSize));
            int maxZ = math.min(level.RowCount - 1, (int)math.floor((maximum.y + half) / cellSize));
            // 地形墙标签与实际弹丸检测一致；这里只读取 BlockProjectile，不触发 HitEvent。
            BrawlCollisionRuleHelper.Match(rules, BrawlUnitLayer.Tile, sourceFaction, BrawlFaction.NeutralHostile,
                out bool terrainBlocked, out _);
            for (int z = minZ; terrainBlocked && z <= maxZ; ++z)
            for (int x = minX; x <= maxX; ++x)
            {
                int index = z * level.ColumnCount + x;
                if (level.MapGrid[index] != (int)BrawlTileType.Wall) continue;
                if (BrawlAISightGeometryHelper.Rectangle(start, end, width,
                    BrawlTerrainMovementHelper.CellPosition(level, index, cellSize), new float2(half))) return false;
            }
            foreach (Entity child in units.Children.Values)
            {
                Unit wall = (Unit)child;
                BrawlPracticeUnitComponent data = wall.GetComponent<BrawlPracticeUnitComponent>();
                StateComponent states = wall.GetComponent<StateComponent>();
                if ((data.UnitLayerMask & BrawlUnitLayer.Tile) == 0 || states.Has(StateId.Dead) || states.Has(StateId.Spawning)) continue;
                BrawlCollisionRuleHelper.Match(rules, data.UnitLayerMask, sourceFaction, data.Faction, out bool blocked, out _);
                if (!blocked) continue;
                float2 center = wall.Position.xz + data.CollisionPivot;
                bool hit = data.CollisionWidth > 0
                    ? BrawlAISightGeometryHelper.Rectangle(start, end, width, center, new float2(data.CollisionLength, data.CollisionWidth) / 2)
                    : BrawlAISightGeometryHelper.Circle(start, end, width, center, data.CollisionLength / 2);
                if (hit) return false;
            }
            return true;
        }
    }
}
