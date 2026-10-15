using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    public static class BrawlProjectileCollisionHelper
    {
        public static void Advance(BrawlProjectile projectile, UnitComponent units, BrawlLevelConfig level, int cellSize, float seconds)
        {
            if (projectile.Finished) return;
            float stepSeconds = projectile.Lifetime > 0 ? math.min(seconds, math.max(0, projectile.Lifetime - projectile.Age)) : seconds;
            float distance = math.min(projectile.Speed * stepSeconds, projectile.RemainingDistance);
            float2 start = projectile.Position;
            float2 delta = projectile.Direction * distance;
            List<BrawlProjectileContact> contacts = new();
            Terrain(projectile, level, cellSize, start, delta, contacts);
            float targetTimeFraction = seconds > 0 && projectile.Speed > 0 ? distance / (projectile.Speed * seconds) : 0;
            foreach (Entity child in units.Children.Values)
            {
                Unit unit = (Unit)child;
                if (unit.Id == projectile.OwnerUnitId || projectile.HitUnits.Contains(unit.Id)) continue;
                StateComponent states = unit.GetComponent<StateComponent>();
                if (states != null && (states.Has(StateId.Dead) || states.Has(StateId.Spawning))) continue;
                BrawlPracticeUnitComponent data = unit.GetComponent<BrawlPracticeUnitComponent>();
                BrawlCollisionRuleHelper.Match(projectile.Rules, data.UnitLayerMask, projectile.Faction, data.Faction,
                    out bool blocked, out bool effective);
                if (!blocked && !effective) continue;
                bool wall = (data.UnitLayerMask & BrawlUnitLayer.Tile) != 0;
                float2 center = unit.Position.xz;
                float2 relativeDelta = delta;
                float2 targetDelta = float2.zero;
                if (seconds > 0 && !wall)
                {
                    center = data.PreviousPosition;
                    targetDelta = (unit.Position.xz - data.PreviousPosition) * targetTimeFraction;
                    relativeDelta -= targetDelta;
                }
                center += data.CollisionPivot;
                float fraction;
                bool hit = data.CollisionWidth == 0
                    ? BrawlProjectileGeometryHelper.Circle(start, relativeDelta, center, data.CollisionLength / 2 + projectile.Radius, out fraction)
                    : BrawlProjectileGeometryHelper.Rectangle(start, relativeDelta, center,
                        new float2(data.CollisionLength, data.CollisionWidth) / 2, projectile.Radius, out fraction);
                if (hit) contacts.Add(new BrawlProjectileContact
                {
                    Fraction = fraction, UnitId = unit.Id, TerrainIndex = -1, Blocked = blocked, Effective = effective, Wall = wall,
                    Normal = Normal(start + delta * fraction, center + targetDelta * fraction,
                        data.CollisionLength, data.CollisionWidth, projectile.Direction),
                });
            }
            contacts.Sort(Compare);
            foreach (BrawlProjectileContact contact in contacts)
            {
                // 收集与执行分离；较早的接触可能已经改变目标状态。
                if (contact.UnitId != 0)
                {
                    Unit target = units.Get(contact.UnitId);
                    if (target == null) continue;
                    StateComponent states = target.GetComponent<StateComponent>();
                    if (states != null && (states.Has(StateId.Dead) || states.Has(StateId.Spawning))) continue;
                    if (!projectile.HitUnits.Add(contact.UnitId)) continue;
                    if (contact.Effective) BrawlProjectileHitHelper.Apply(projectile, target);
                }
                else
                {
                    if (!projectile.HitTerrain.Add(contact.TerrainIndex)) continue;
                    if (contact.Effective && level.MapGrid[contact.TerrainIndex] == (int)BrawlTileType.Grass &&
                        projectile.EffectValue > 0 && (projectile.HitEvent1 == BrawlHitEventType.Damage || projectile.HitEvent2 == BrawlHitEventType.Damage))
                        projectile.GetParent<BrawlProjectileRoomComponent>().ClearedGrass.Add(contact.TerrainIndex);
                }
                if (contact.Effective) ++projectile.HitCount;
                BrawlProjectileNoticeHelper.Impact(projectile, contact, start + delta * contact.Fraction);
                if (!contact.Blocked && (projectile.MaxHitCount == 0 || projectile.HitCount < projectile.MaxHitCount)) continue;
                projectile.Position = start + delta * contact.Fraction;
                projectile.RemainingDistance = math.max(0, projectile.RemainingDistance - distance * contact.Fraction);
                projectile.Age += stepSeconds * contact.Fraction;
                BrawlProjectileNoticeHelper.End(projectile, contact.Blocked ? BrawlProjectileEndReason.Blocked : BrawlProjectileEndReason.HitLimit);
                return;
            }
            projectile.Position = start + delta;
            projectile.RemainingDistance = math.max(0, projectile.RemainingDistance - distance);
            projectile.Age += stepSeconds;
            if (projectile.RemainingDistance <= 0) BrawlProjectileNoticeHelper.End(projectile, BrawlProjectileEndReason.Range);
            else if (projectile.Lifetime > 0 && projectile.Age >= projectile.Lifetime)
                BrawlProjectileNoticeHelper.End(projectile, BrawlProjectileEndReason.Lifetime);
        }

        private static int Compare(BrawlProjectileContact left, BrawlProjectileContact right)
        {
            int order = left.Fraction.CompareTo(right.Fraction);
            if (order != 0) return order;
            order = right.Wall.CompareTo(left.Wall);
            if (order != 0) return order;
            order = right.Blocked.CompareTo(left.Blocked);
            if (order != 0) return order;
            order = left.UnitId.CompareTo(right.UnitId);
            return order != 0 ? order : left.TerrainIndex.CompareTo(right.TerrainIndex);
        }

        private static float2 Normal(float2 position, float2 center, float length, float width, float2 direction)
        {
            float2 offset = position - center;
            if (width > 0)
            {
                float2 half = new float2(length, width) / 2;
                offset -= math.clamp(offset, -half, half);
            }
            // 出生时已在形状内部，无唯一表面法线，约定使用飞行反方向。
            return math.normalizesafe(offset, -direction);
        }

        private static void Terrain(BrawlProjectile projectile, BrawlLevelConfig level, int cellSize, float2 start, float2 delta,
            List<BrawlProjectileContact> contacts)
        {
            BrawlProjectileRoomComponent room = projectile.GetParent<BrawlProjectileRoomComponent>();
            float half = cellSize / 2f;
            float2 offset = BrawlTerrainMovementHelper.GridOffset(level, cellSize);
            float2 minimum = math.min(start, start + delta) - projectile.Radius + offset;
            float2 maximum = math.max(start, start + delta) + projectile.Radius + offset;
            int minX = math.max(0, (int)math.ceil((minimum.x - half) / cellSize));
            int maxX = math.min(level.ColumnCount - 1, (int)math.floor((maximum.x + half) / cellSize));
            int minZ = math.max(0, (int)math.ceil((minimum.y - half) / cellSize));
            int maxZ = math.min(level.RowCount - 1, (int)math.floor((maximum.y + half) / cellSize));
            for (int z = minZ; z <= maxZ; ++z)
            for (int x = minX; x <= maxX; ++x)
            {
                int index = z * level.ColumnCount + x;
                if (projectile.HitTerrain.Contains(index)) continue;
                BrawlUnitLayer layers;
                switch ((BrawlTileType)level.MapGrid[index])
                {
                    case BrawlTileType.Wall: layers = BrawlUnitLayer.Tile; break;
                    case BrawlTileType.Grass:
                        if (room.ClearedGrass.Contains(index)) continue;
                        layers = BrawlUnitLayer.Grass | BrawlUnitLayer.Destroyable;
                        break;
                    default: continue;
                }
                BrawlCollisionRuleHelper.Match(projectile.Rules, layers, projectile.Faction, BrawlFaction.NeutralHostile,
                    out bool blocked, out bool effective);
                if (!blocked && !effective) continue;
                float2 center = BrawlTerrainMovementHelper.CellPosition(level, index, cellSize);
                if (BrawlProjectileGeometryHelper.Rectangle(start, delta, center, new float2(half), projectile.Radius, out float fraction))
                    contacts.Add(new BrawlProjectileContact
                    {
                        Fraction = fraction, TerrainIndex = index, Blocked = blocked, Effective = effective,
                        Wall = (layers & BrawlUnitLayer.Tile) != 0,
                        Normal = Normal(start + delta * fraction, center, cellSize, cellSize, projectile.Direction),
                    });
            }
        }
    }
}
