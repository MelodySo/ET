using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlProjectileCollision_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            if (BrawlFactionHelper.IsEnemy(BrawlFaction.NeutralHostile, BrawlFaction.NeutralHostile) ||
                !BrawlFactionHelper.IsEnemy(BrawlFaction.Red, BrawlFaction.NeutralHostile) ||
                !BrawlFactionHelper.IsEnemy(BrawlFaction.Red, BrawlFaction.Blue) ||
                BrawlFactionHelper.IsEnemy(BrawlFaction.Red, BrawlFaction.Red) ||
                BrawlFactionHelper.IsEnemy(BrawlFaction.Blue, BrawlFaction.NeutralFriendly))
                return Fail(100501, "Faction relations are incorrect.");
            if (!BrawlProjectileGeometryHelper.Circle(float2.zero, new float2(10, 0), new float2(5, 0), 0.35f, out float t) ||
                math.abs(t - 0.465f) > 0.00001f)
                return Fail(100502, "Swept circle tunneled through a thin target.");
            if (!BrawlProjectileGeometryHelper.Rectangle(float2.zero, new float2(10, 0), new float2(5, 0), new float2(0.5f), 0.05f, out t) ||
                math.abs(t - 0.445f) > 0.00001f)
                return Fail(100503, "Rectangle impact distance ignored the projectile radius.");
            if (BrawlProjectileGeometryHelper.Rectangle(new float2(0.59f, 0.59f), float2.zero, float2.zero,
                    new float2(0.5f), 0.1f, out _))
                return Fail(100504, "Expanded rectangle square corner produced a false collision.");

            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "ProjectileCollisionTest");
            UnitComponent units = scene.AddComponent<UnitComponent>();
            BrawlProjectileRoomComponent projectiles = scene.AddComponent<BrawlProjectileRoomComponent>();
            BrawlLevelConfig level = new(1, 1, 10, 10, new int[100], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
            Unit owner = Combatant(units, new float2(0, 2), BrawlFaction.Red);
            Unit friend = Combatant(units, new float2(1, 2), BrawlFaction.Red);
            Unit enemy = Combatant(units, new float2(5, 2), BrawlFaction.Blue);
            BrawlProjectile projectile = Shot(projectiles, owner);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 1);
            if (!Stopped(projectile, 4.65f, 1) || !projectile.HitUnits.Contains(enemy.Id) || projectile.HitUnits.Contains(friend.Id))
                return Fail(100505, "Enemy filtering, owner exclusion or nearest impact failed.");

            level.MapGrid[2 * 10 + 3] = (int)BrawlTileType.Wall;
            projectile = Shot(projectiles, owner);
            projectile.MaxHitCount = 0;
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 1);
            if (!Stopped(projectile, 2.45f, 0)) return Fail(100506, "Unlimited effective hits must not bypass wall blocking.");
            level.MapGrid[2 * 10 + 3] = (int)BrawlTileType.Ground;

            Unit obstacle = units.AddChild<Unit, int>(2);
            obstacle.Position = new float3(3, 0, 2);
            BrawlPracticeUnitComponent obstacleData = obstacle.AddComponent<BrawlPracticeUnitComponent>();
            obstacleData.Kind = BrawlPracticeUnitKind.NPC;
            obstacleData.NPCType = BrawlNPCType.Obstacle;
            obstacleData.Faction = BrawlFaction.NeutralHostile;
            BrawlNPCConfig config = new(2, 2, 0, 1, 0, 0, new[] { 1 }, new[] { 1 }, 1, 1, 0, 0, 0, 0, 0);
            BrawlNPCBlockHelper.Attach(obstacle, config, new BrawlUnitComponentBlockConfigCategory(
                new Dictionary<int, BrawlUnitComponentBlockConfig> { [1] = new(1, true) }));
            projectile = Shot(projectiles, owner);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 1);
            if (!Stopped(projectile, 2.45f, 0)) return Fail(100507, "Neutral barrel must block as an obstacle, not count as an enemy.");
            BrawlUnitComponentBlock block = obstacle.GetComponent<BrawlUnitComponentBlock>();
            block.Width = 1;
            block.Pivot = new float2(0.25f, 0);
            projectile = Shot(projectiles, owner);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 1);
            if (!Stopped(projectile, 2.7f, 0)) return Fail(100508, "Rectangular obstacle pivot was ignored.");
            obstacle.RemoveComponent<BrawlUnitComponentBlock>();
            obstacleData.NPCType = BrawlNPCType.Gem;
            projectile = Shot(projectiles, owner);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 1);
            if (!Stopped(projectile, 4.65f, 1)) return Fail(100509, "Non-combat NPC must not become an enemy target.");

            enemy.GetComponent<BrawlPracticeUnitComponent>().Faction = BrawlFaction.NeutralHostile;
            enemy.GetComponent<BrawlPracticeUnitComponent>().Kind = BrawlPracticeUnitKind.NPC;
            enemy.GetComponent<BrawlPracticeUnitComponent>().NPCType = BrawlNPCType.Combatant;
            projectile = Shot(projectiles, owner);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 1);
            if (!Stopped(projectile, 4.65f, 1)) return Fail(100510, "Hostile-neutral combat NPC was not hit.");

            projectile = Shot(projectiles, owner);
            projectile.BlockMask = BrawlProjectileBlockMask.None;
            projectile.MaxHitCount = 0;
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 0.5f);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 0.1f);
            if (projectile.HitCount != 1 || projectile.Finished || math.abs(projectile.Position.x - 6) > 0.0001f)
                return Fail(100511, "Penetration hit the same unit repeatedly or stopped without a termination condition.");
            projectile = Shot(projectiles, owner);
            projectile.BlockMask = BrawlProjectileBlockMask.None;
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 1);
            if (!Stopped(projectile, 4.65f, 1)) return Fail(100512, "Effective hit count must terminate independently of blocking.");

            // 起终点都不重叠弹道的敌人，本步横穿弹道，应由相对运动检测。
            enemy.GetComponent<BrawlPracticeUnitComponent>().PreviousPosition = new float2(2.5f, 0);
            enemy.Position = new float3(2.5f, 0, 4);
            projectile = Shot(projectiles, owner);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 0.5f);
            if (!projectile.Finished || projectile.HitCount != 1 || projectile.Position.x >= 2.5f)
                return Fail(100513, "Moving target crossing the projectile segment was missed.");
            enemy.GetComponent<BrawlPracticeUnitComponent>().PreviousPosition = new float2(0, 2);
            enemy.Position = new float3(0, 0, 2);
            projectile = Shot(projectiles, owner);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 0);
            if (!Stopped(projectile, 0, 1)) return Fail(100514, "Spawn overlap must be detected before the first flight step.");
            PropertyComponent health = enemy.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new()
            {
                [PropertyId.BaseMaxHealth] = 1000, [PropertyId.Health] = 1000, [PropertyId.DamageReductionRatio] = 0.2,
            });
            enemy.AddComponent<StateComponent>();
            projectile = Shot(projectiles, owner);
            projectile.HitEvent1 = BrawlHitEventType.Damage;
            projectile.EffectValue = 330;
            projectile.BlockMask = BrawlProjectileBlockMask.None;
            projectile.MaxHitCount = 0;
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 0);
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 0);
            if (math.abs(health.GetFloat(PropertyId.Health) - 736) > 0.001f || projectile.HitCount != 1)
                return Fail(100515, "One penetrating projectile applied damage repeatedly to an overlapping target.");
            projectile = Shot(projectiles, owner);
            projectile.HitEvent1 = BrawlHitEventType.Damage;
            projectile.EffectValue = 330;
            BrawlProjectileCollisionHelper.Advance(projectile, units, level, 1, 0);
            if (math.abs(health.GetFloat(PropertyId.Health) - 472) > 0.001f)
                return Fail(100516, "Different pellets must damage the same target independently.");
            return ErrorCode.ERR_Success;
        }

        private static Unit Combatant(UnitComponent units, float2 position, BrawlFaction faction)
        {
            Unit unit = units.AddChild<Unit, int>(1);
            unit.Position = new float3(position.x, 0, position.y);
            BrawlPracticeUnitComponent data = unit.AddComponent<BrawlPracticeUnitComponent>();
            data.Kind = BrawlPracticeUnitKind.Player;
            data.Faction = faction;
            data.CollisionLength = 0.6f;
            data.PreviousPosition = position;
            return unit;
        }

        private static BrawlProjectile Shot(BrawlProjectileRoomComponent room, Unit owner)
        {
            BrawlProjectile projectile = room.AddChild<BrawlProjectile>();
            projectile.OwnerUnitId = owner.Id;
            projectile.Faction = owner.GetComponent<BrawlPracticeUnitComponent>().Faction;
            projectile.Position = owner.Position.xz;
            projectile.Direction = new float2(1, 0);
            projectile.Speed = 10;
            projectile.Radius = 0.05f;
            projectile.RemainingDistance = 20;
            projectile.BlockMask = BrawlProjectileBlockMask.IndestructibleWall | BrawlProjectileBlockMask.DestructibleWall | BrawlProjectileBlockMask.Enemy;
            projectile.TargetMask = BrawlProjectileTargetMask.Enemy;
            projectile.MaxHitCount = 1;
            return projectile;
        }

        private static bool Stopped(BrawlProjectile projectile, float x, int hits) =>
            projectile.Finished && math.abs(projectile.Position.x - x) < 0.0001f && projectile.HitCount == hits;

        private static int Fail(int code, string message)
        {
            Log.Console(message);
            return code;
        }
    }
}
