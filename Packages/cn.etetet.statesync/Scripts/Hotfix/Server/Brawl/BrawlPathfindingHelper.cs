using System;
using System.Collections.Generic;
using Unity.Mathematics;
namespace ET.Server
{
    /// <summary>Bounded A* over terrain cell centres; every edge uses the actual radius/NPC movement constraints.</summary>
    public static class BrawlPathfindingHelper
    {
        public static bool CanTraverse(UnitComponent units, long id, BrawlLevelConfig level, float cellSize,
            float2 start, float2 end, float radius)
        {
            float distance = math.distance(start, end);
            int steps = math.max(1, (int)math.ceil(distance / (math.min(cellSize, radius) * 0.25f)));
            float2 current = start;
            for (int i = 1; i <= steps; ++i)
            {
                float2 wanted = math.lerp(start, end, (float)i / steps);
                if (!BrawlTerrainMovementHelper.CanOccupy(level, cellSize, wanted, radius) ||
                    !BrawlUnitMovementHelper.CanOccupy(units, id, wanted, radius)) return false;
                current = BrawlUnitMovementHelper.Move(units, level, cellSize, id, current, wanted - current, radius);
                if (math.distancesq(current, wanted) > 0.000001f) return false;
            }
            return true;
        }

        public static bool Find(UnitComponent units, Unit unit, BrawlLevelConfig level, float cellSize,
            float2 target, int maxSearchNodes, List<float2> path)
        {
            path.Clear();
            float radius = unit.GetComponent<BrawlMoveComponent>().Radius;
            float2 start = unit.Position.xz;
            if (CanTraverse(units, unit.Id, level, cellSize, start, target, radius)) { path.Add(target); return true; }
            int count = level.MapGrid.Length;
            float[] costs = new float[count]; int[] parents = new int[count]; bool[] closed = new bool[count];
            Array.Fill(costs, float.PositiveInfinity); Array.Fill(parents, -1);
            SortedSet<(float Cost, int Index)> open = new();
            float2 grid = (start + BrawlTerrainMovementHelper.GridOffset(level, cellSize)) / cellSize;
            int2 origin = (int2)math.round(grid);
            // Connect the real position to nearby visible centres, never snap the Unit to a grid point.
            for (int y = math.max(0, origin.y - 1); y <= math.min(level.RowCount - 1, origin.y + 1); ++y)
            for (int x = math.max(0, origin.x - 1); x <= math.min(level.ColumnCount - 1, origin.x + 1); ++x)
            {
                int index = y * level.ColumnCount + x;
                float2 point = BrawlTerrainMovementHelper.CellPosition(level, index, cellSize);
                if (!CanTraverse(units, unit.Id, level, cellSize, start, point, radius)) continue;
                costs[index] = math.distance(start, point);
                open.Add((costs[index] + math.distance(point, target), index));
            }
            int visited = 0;
            while (open.Count > 0 && visited < math.clamp(maxSearchNodes, 1, count))
            {
                var entry = open.Min; open.Remove(entry);
                int index = entry.Index;
                if (closed[index]) continue;
                closed[index] = true; ++visited;
                float2 current = BrawlTerrainMovementHelper.CellPosition(level, index, cellSize);
                if (math.distance(current, target) <= cellSize * 1.5f && CanTraverse(units, unit.Id, level, cellSize, current, target, radius))
                {
                    for (int p = index; p >= 0; p = parents[p]) path.Add(BrawlTerrainMovementHelper.CellPosition(level, p, cellSize));
                    path.Reverse(); path.Add(target); return true;
                }
                int column = index % level.ColumnCount, row = index / level.ColumnCount;
                for (int dy = -1; dy <= 1; ++dy)
                for (int dx = -1; dx <= 1; ++dx)
                {
                    if (dx == 0 && dy == 0) continue;
                    int x = column + dx, y = row + dy;
                    if (x < 0 || y < 0 || x >= level.ColumnCount || y >= level.RowCount) continue;
                    int next = y * level.ColumnCount + x;
                    if (closed[next]) continue;
                    float2 point = BrawlTerrainMovementHelper.CellPosition(level, next, cellSize);
                    float cost = costs[index] + math.distance(current, point);
                    if (cost >= costs[next] || !CanTraverse(units, unit.Id, level, cellSize, current, point, radius)) continue;
                    costs[next] = cost; parents[next] = index;
                    open.Add((cost + math.distance(point, target), next));
                }
            }
            return false; // Unreachable or budget exhausted: remain stopped and retry at the next planning deadline.
        }
    }
}