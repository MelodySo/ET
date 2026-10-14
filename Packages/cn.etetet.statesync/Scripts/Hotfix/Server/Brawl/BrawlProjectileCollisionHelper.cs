using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public static class BrawlProjectileCollisionHelper
    {
        public static void Advance(BrawlProjectile projectile, UnitComponent units, BrawlLevelConfig level, int cellSize, float seconds)
        {
            if (projectile.Finished) return;
            float distance = math.min(projectile.Speed * seconds, projectile.RemainingDistance);
            float2 start = projectile.Position;
            float2 delta = projectile.Direction * distance;
            List<BrawlProjectileContact> contacts = new();
            Terrain(projectile, level, cellSize, start, delta, contacts);
            float targetTimeFraction = seconds > 0 ? distance / (projectile.Speed * seconds) : 0;
            foreach (Entity child in units.Children.Values)
            {
                Unit unit = (Unit)child;
                if (unit.Id == projectile.OwnerUnitId) continue;
                StateComponent states = unit.GetComponent<StateComponent>();
                if (states != null && (states.Has(StateId.Dead) || states.Has(StateId.Spawning))) continue;
                BrawlPracticeUnitComponent data = unit.GetComponent<BrawlPracticeUnitComponent>();
                BrawlProjectileBlockMask block;
                BrawlProjectileTargetMask target;
                bool wall = data.Kind == BrawlPracticeUnitKind.NPC && data.NPCType == BrawlNPCType.Obstacle;
                float length = data.CollisionLength;
                float width = data.CollisionWidth;
                float2 pivot = data.CollisionPivot;
                if (wall)
                {
                    BrawlUnitComponentBlock obstacle = unit.GetComponent<BrawlUnitComponentBlock>();
                    if (obstacle == null) continue;
                    block = obstacle.CanBeDestroyed ? BrawlProjectileBlockMask.DestructibleWall : BrawlProjectileBlockMask.IndestructibleWall;
                    target = obstacle.CanBeDestroyed ? BrawlProjectileTargetMask.DestructibleWall : BrawlProjectileTargetMask.None;
                    length = obstacle.Length;
                    width = obstacle.Width;
                    pivot = obstacle.Pivot;
                }
                else
                {
                    if (data.Kind == BrawlPracticeUnitKind.NPC && data.NPCType != BrawlNPCType.Combatant) continue;
                    bool enemy = BrawlFactionHelper.IsEnemy(projectile.Faction, data.Faction);
                    block = enemy ? BrawlProjectileBlockMask.Enemy : BrawlProjectileBlockMask.Ally;
                    target = enemy ? BrawlProjectileTargetMask.Enemy : BrawlProjectileTargetMask.Ally;
                }
                bool blocked = (projectile.BlockMask & block) != 0;
                bool effective = (projectile.TargetMask & target) != 0 && !projectile.HitUnits.Contains(unit.Id);
                if (!blocked && !effective) continue;
                float2 center = unit.Position.xz;
                float2 relativeDelta = delta;
                if (seconds > 0 && !wall)
                {
                    // 相对运动扫掠：目标本步横穿弹道也不能漏掉；到射程终点的短步按对应时长裁剪。
                    center = data.PreviousPosition;
                    relativeDelta -= (unit.Position.xz - data.PreviousPosition) * targetTimeFraction;
                }
                center += pivot;
                float fraction;
                bool hit = width == 0
                    ? BrawlProjectileGeometryHelper.Circle(start, relativeDelta, center, length / 2 + projectile.Radius, out fraction)
                    : BrawlProjectileGeometryHelper.Rectangle(start, relativeDelta, center, new float2(length, width) / 2, projectile.Radius, out fraction);
                if (hit) contacts.Add(new BrawlProjectileContact { Fraction = fraction, UnitId = unit.Id, Blocked = blocked, Effective = effective, Wall = wall });
            }
            contacts.Sort(Compare);
            foreach (BrawlProjectileContact contact in contacts)
            {
                if (contact.Effective && projectile.HitUnits.Add(contact.UnitId))
                {
                    BrawlProjectileHitHelper.Apply(projectile, units.Get(contact.UnitId));
                    ++projectile.HitCount;
                }
                if (!contact.Blocked && (projectile.MaxHitCount == 0 || projectile.HitCount < projectile.MaxHitCount)) continue;
                projectile.Position = start + delta * contact.Fraction;
                projectile.RemainingDistance -= distance * contact.Fraction;
                projectile.Finished = true;
                return;
            }
            projectile.Position = start + delta;
            projectile.RemainingDistance -= distance;
            projectile.Finished = projectile.RemainingDistance <= 0;
        }

        private static int Compare(BrawlProjectileContact left, BrawlProjectileContact right)
        {
            int order = left.Fraction.CompareTo(right.Fraction);
            if (order != 0) return order;
            // 同时接触：墙优先，其次阻挡目标；最后按实体 ID 保证顺序确定。
            order = right.Wall.CompareTo(left.Wall);
            if (order != 0) return order;
            order = right.Blocked.CompareTo(left.Blocked);
            return order != 0 ? order : left.UnitId.CompareTo(right.UnitId);
        }

        private static void Terrain(BrawlProjectile projectile, BrawlLevelConfig level, int cellSize, float2 start, float2 delta,
            List<BrawlProjectileContact> contacts)
        {
            if ((projectile.BlockMask & BrawlProjectileBlockMask.IndestructibleWall) == 0) return;
            float half = cellSize / 2f;
            float2 minimum = math.min(start, start + delta) - projectile.Radius;
            float2 maximum = math.max(start, start + delta) + projectile.Radius;
            int minX = math.max(0, (int)math.ceil((minimum.x - half) / cellSize));
            int maxX = math.min(level.ColumnCount - 1, (int)math.floor((maximum.x + half) / cellSize));
            int minZ = math.max(0, (int)math.ceil((minimum.y - half) / cellSize));
            int maxZ = math.min(level.RowCount - 1, (int)math.floor((maximum.y + half) / cellSize));
            for (int z = minZ; z <= maxZ; ++z)
            for (int x = minX; x <= maxX; ++x)
            {
                if (level.MapGrid[z * level.ColumnCount + x] != (int)BrawlTileType.Wall) continue;
                if (BrawlProjectileGeometryHelper.Rectangle(start, delta, new float2(x, z) * cellSize,
                        new float2(half), projectile.Radius, out float fraction))
                    contacts.Add(new BrawlProjectileContact { Fraction = fraction, Blocked = true, Wall = true });
            }
        }
    }
}
