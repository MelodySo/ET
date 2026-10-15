using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;
namespace ET.Test
{
    public class Statesync_BrawlCollisionRules_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            BrawlCollisionRuleConfig[] normal = BrawlCollisionTestHelper.Rules(1);
            BrawlCollisionRuleConfig[] super = BrawlCollisionTestHelper.Rules(2);
            BrawlCollisionRuleHelper.Match(normal, BrawlUnitLayer.Tile | BrawlUnitLayer.Destroyable,
                BrawlFaction.Red, BrawlFaction.Red, out bool block, out bool effect);
            if (!block || effect) return Fail(101301, "Wall blocking incorrectly depends on faction.");
            BrawlCollisionRuleHelper.Match(super, BrawlUnitLayer.Battle, BrawlFaction.NeutralHostile, BrawlFaction.NeutralHostile, out block, out effect);
            if (block || effect) return Fail(101302, "Neutral units of same faction became enemies.");
            try { BrawlCollisionRuleHelper.Resolve(BrawlCollisionTestHelper.Category(), 99); return Fail(101303, "Missing group accepted."); }
            catch (ArgumentException) { }
            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "CollisionRulesTest");
            UnitComponent units = scene.AddComponent<UnitComponent>();
            scene.AddComponent<BrawlLifecycleRoomComponent>();
            BrawlProjectileRoomComponent room = scene.AddComponent<BrawlProjectileRoomComponent>();
            room.CurrentTick = 10;
            BrawlLevelConfig level = new(1, 1, 10, 10, new int[100], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
            level.MapGrid[22] = (int)BrawlTileType.Grass;
            level.MapGrid[27] = (int)BrawlTileType.Wall;
            Unit barrel = Target(units, 3, BrawlUnitLayer.Tile | BrawlUnitLayer.Destroyable, BrawlFaction.NeutralHostile, 1);
            Unit enemy = Target(units, 5, BrawlUnitLayer.Hero | BrawlUnitLayer.Battle, BrawlFaction.Blue, 100);
            Unit friend = Target(units, 4, BrawlUnitLayer.Hero | BrawlUnitLayer.Battle, BrawlFaction.Red, 100);
            BrawlProjectile shot = Shot(room, super);
            BrawlProjectileCollisionHelper.Advance(shot, units, level, 1, 1);
            if (!shot.Finished || math.abs(shot.Position.x - 6.45f) > 0.001f || shot.HitCount != 3 || !room.ClearedGrass.Contains(22) ||
                level.MapGrid[22] != (int)BrawlTileType.Grass || !barrel.GetComponent<StateComponent>().Has(StateId.Dead) ||
                enemy.GetComponent<PropertyComponent>().GetFloat(PropertyId.Health) != 90 || friend.GetComponent<PropertyComponent>().GetFloat(PropertyId.Health) != 100)
                return Fail(101304, "Piercing sequence or private terrain mutation failed.");
            if (room.Notices.Count != 5 || room.Notices[0].TerrainIndex != 22 || room.Notices[1].TargetUnitId != barrel.Id ||
                room.Notices[2].TargetUnitId != enemy.Id || room.Notices[3].TerrainIndex != 27 ||
                room.Notices[4].EndReason != BrawlProjectileEndReason.Blocked || room.Notices[4].Tick != 10)
                return Fail(101305, "Impact order or single terminal notification incorrect.");
            BrawlProjectileCollisionHelper.Advance(shot, units, level, 1, 1);
            if (room.Notices.Count != 5) return Fail(101306, "Finished projectile emitted duplicate events.");
            room.Notices.Clear();
            shot = Shot(room, normal); shot.MaxHitCount = 1;
            BrawlProjectileCollisionHelper.Advance(shot, units, level, 1, 1);
            if (shot.HitCount != 1 || !shot.Finished || room.Notices.Count != 2 || room.Notices[0].TargetUnitId != enemy.Id)
                return Fail(101307, "Dead wall, cleared grass or friend still blocks normal attack.");
            room.Notices.Clear();
            shot = Shot(room, super); shot.Position = new float2(0, 8); shot.RemainingDistance = 1;
            BrawlProjectileCollisionHelper.Advance(shot, units, level, 1, 0.5f);
            if (room.Notices.Count != 1 || room.Notices[0].EndReason != BrawlProjectileEndReason.Range) return Fail(101308, "Range end reason missing.");
            room.Notices.Clear();
            shot = Shot(room, super); shot.Position = new float2(0, 8); shot.Lifetime = 0.025f;
            BrawlProjectileCollisionHelper.Advance(shot, units, level, 1, 0.05f);
            if (math.abs(shot.Position.x - 0.25f) > 0.0001f || room.Notices.Count != 1 || room.Notices[0].EndReason != BrawlProjectileEndReason.Lifetime)
                return Fail(101309, "Lifetime was not clipped inside a simulation step.");
            room.Notices.Clear();
            shot = Shot(room, new[] { super[2], super[2] }); shot.MaxHitCount = 1;
            BrawlProjectileCollisionHelper.Advance(shot, units, level, 1, 1);
            if (shot.HitCount != 1 || room.Notices.Count != 2 || room.Notices[1].EndReason != BrawlProjectileEndReason.HitLimit ||
                enemy.GetComponent<PropertyComponent>().GetFloat(PropertyId.Health) != 70)
                return Fail(101310, "Overlapping rules duplicated damage or missed hit-count termination.");
            using Scene other = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "OtherRoom");
            if (other.AddComponent<BrawlProjectileRoomComponent>().ClearedGrass.Count != 0) return Fail(101311, "Grass state leaked across rooms.");
            return ErrorCode.ERR_Success;
        }
        private static Unit Target(UnitComponent units, float x, BrawlUnitLayer layers, BrawlFaction faction, int hp)
        {
            Unit unit = units.AddChild<Unit, int>(1); unit.Position = new float3(x, 0, 2);
            BrawlPracticeUnitComponent data = unit.AddComponent<BrawlPracticeUnitComponent>();
            data.Kind = BrawlPracticeUnitKind.NPC; data.Faction = faction; data.UnitLayerMask = layers;
            data.PreviousPosition = unit.Position.xz; data.CollisionLength = 1;
            data.CollisionWidth = (layers & BrawlUnitLayer.Tile) != 0 ? 1 : 0;
            unit.AddComponent<StateComponent>(); unit.AddComponent<BrawlSpawnComponent>(); unit.AddComponent<BrawlDeathComponent>();
            unit.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new() { [PropertyId.Health] = hp, [PropertyId.BaseMaxHealth] = hp });
            return unit;
        }
        private static BrawlProjectile Shot(BrawlProjectileRoomComponent room, BrawlCollisionRuleConfig[] rules)
        {
            BrawlProjectile shot = room.AddChild<BrawlProjectile>();
            shot.Rules = rules; shot.Faction = BrawlFaction.Red; shot.Position = new float2(0, 2); shot.Direction = new float2(1, 0);
            shot.Speed = 10; shot.Radius = 0.05f; shot.RemainingDistance = 20; shot.EffectValue = 10; shot.HitEvent1 = BrawlHitEventType.Damage;
            return shot;
        }
        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
