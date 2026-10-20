using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlSkillSequence_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            Fiber fiber = context.Fiber;
            BrawlProjectileConfigCategory previous = fiber.GetSingleton<BrawlProjectileConfigCategory>();
            BrawlCollisionRuleConfigCategory previousRules = fiber.GetSingleton<BrawlCollisionRuleConfigCategory>();
            BrawlSkillConfig skill = new(50101, 50101, 1, (int)BrawlEmitterType.Whirlwind,
                1, 0.3f, 0, 3, 1.5f, 1, 1, (int)BrawlSkillRangeType.Circle, 3, 0, 0, 0, 0);
            BrawlProjectileConfig config = new(50101, 3, 0, 0, 0, 1050, 105, 1, 0, 0, 0, 0, 0, 0, 3);
            try
            {
                fiber.AddSingleton(new BrawlProjectileConfigCategory(new() { [config.Id] = config }));
                fiber.AddSingleton(new BrawlCollisionRuleConfigCategory(new()
                {
                    [301] = new(301, 3, (int)BrawlUnitLayer.Battle, (int)BrawlUnitLayer.Tile, 1, false, true),
                }));
                using Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(),
                    SceneType.BrawlPractice, "SkillSequenceTest");
                UnitComponent units = scene.AddComponent<UnitComponent>();
                BrawlProjectileRoomComponent projectiles = scene.AddComponent<BrawlProjectileRoomComponent>();
                Unit owner = Target(units, float2.zero, BrawlFaction.Red);
                owner.AddComponent<BrawlSkillCastComponent>();
                owner.AddComponent<BrawlAmmoComponent>();
                owner.AddComponent<BrawlSpawnComponent>();
                owner.GetComponent<PropertyComponent>().Set(PropertyId.Level, 1);
                BrawlAmmoHelper.Initialize(owner, skill, 1000);
                owner.GetComponent<PropertyComponent>().Set(PropertyId.Ammo, 3);
                Unit enemy = Target(units, new float2(6, 0), BrawlFaction.Blue);
                Unit secondEnemy = Target(units, new float2(6, -1), BrawlFaction.Blue);
                Unit oldCenter = Target(units, new float2(-2, 0), BrawlFaction.Blue);
                Unit friend = Target(units, new float2(5, 0), BrawlFaction.Red);
                Unit immune = Target(units, new float2(6, 1), BrawlFaction.Blue);
                immune.GetComponent<StateComponent>().AddSource(StateId.Invincible, 1);
                Unit wall = Target(units, new float2(5, 0), BrawlFaction.NeutralHostile);
                wall.GetComponent<BrawlPracticeUnitComponent>().UnitLayerMask = BrawlUnitLayer.Tile | BrawlUnitLayer.Destroyable;
                BrawlLevelConfig level = new(1, 1, 20, 20, new int[400], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
                level.MapGrid[210] = (int)BrawlTileType.Wall;
                level.MapGrid[211] = (int)BrawlTileType.Grass;
                if (!BrawlSkillCastHelper.TryStart(owner, skill, config, BrawlSkillSlot.NormalAttack, new float2(0, 1), 0.6f, 1000))
                    return Fail(102001, "Whirlwind cast was rejected.");
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1000);
                if (Health(enemy) != 10000 || Health(secondEnemy) != 10000 ||
                    projectiles.Children.Count != 0 || projectiles.SkillNotices.Count != 2 ||
                    projectiles.SkillNotices[1].PresentationId != BrawlSkillPresentationId.WhirlwindCharge)
                    return Fail(102002, "Charge must notify presentation without creating damage or a visible projectile.");
                if (owner.GetComponent<PropertyComponent>().GetDouble(PropertyId.Ammo) != 2 ||
                    owner.GetComponent<BrawlSkillCastComponent>().Plan.LockFacing)
                    return Fail(102003, "Charge must consume one ammo without locking facing.");
                owner.Position = new float3(4, 0, 0);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1299);
                if (Health(enemy) != 10000 || BrawlSkillSequenceHelper.CanStart(owner.GetComponent<BrawlSkillCastComponent>(), 1299))
                    return Fail(102004, "Charge fired early or accepted a second cast.");
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1300);
                if (Health(enemy) != 8950 || Health(secondEnemy) != 8950 || Health(owner) != 10000 ||
                    Health(oldCenter) != 10000 || Health(friend) != 10000 ||
                    Health(immune) != 10000 || Health(wall) != 10000 || projectiles.ClearedGrass.Count != 0)
                    return Fail(102005, "Circle must follow its owner, damage enemies through walls and preserve non-targets.");
                if (projectiles.Children.Count != 0 || owner.GetComponent<BrawlSkillCastComponent>().Plan != null ||
                    projectiles.SkillNotices.Count != 4 || projectiles.SkillNotices[2].StepId != 2 ||
                    projectiles.SkillNotices[2].PositionX != 4 || projectiles.SkillNotices[3].Phase != BrawlSkillUsePhase.Completed)
                    return Fail(102006, "Pulse must complete once, publish its actual origin and leave no flying projectile.");
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1350);
                if (Health(enemy) != 8950 || Health(secondEnemy) != 8950 ||
                    BrawlSkillSequenceHelper.CanStart(owner.GetComponent<BrawlSkillCastComponent>(), 1999))
                    return Fail(102007, "Pulse repeated damage or lost its cooldown.");
                foreach (StateId interruptedBy in new[] { StateId.Stunned, StateId.Dead })
                {
                    long now = interruptedBy == StateId.Stunned ? 2000 : 3000;
                    if (!BrawlSkillCastHelper.TryStart(owner, skill, config, BrawlSkillSlot.NormalAttack, new float2(0, 1), 0.6f, now))
                        return Fail(102008, "Available cast could not restart.");
                    BrawlSkillSequenceHelper.Tick(owner, level, 1, now);
                    owner.GetComponent<StateComponent>().AddSource(interruptedBy, 2);
                    BrawlSkillSequenceHelper.Tick(owner, level, 1, now + 300);
                    owner.GetComponent<StateComponent>().RemoveSource(interruptedBy, 2);
                    BrawlSkillSequenceHelper.Tick(owner, level, 1, now + 350);
                    if (Health(enemy) != 8950 || Health(secondEnemy) != 8950 ||
                        owner.GetComponent<BrawlSkillCastComponent>().Plan != null ||
                        projectiles.SkillNotices[^1].Phase != BrawlSkillUsePhase.Interrupted)
                        return Fail(102009, "Interrupted charge must never resume or deal damage.");
                }
            }
            finally { fiber.AddSingleton(previous); fiber.AddSingleton(previousRules); }
            return ErrorCode.ERR_Success;
        }

        private static Unit Target(UnitComponent units, float2 position, BrawlFaction faction)
        {
            Unit unit = units.AddChild<Unit, int>(501);
            unit.Position = new float3(position.x, 0, position.y);
            BrawlPracticeUnitComponent data = unit.AddComponent<BrawlPracticeUnitComponent>();
            data.Faction = faction;
            data.UnitLayerMask = BrawlUnitLayer.Battle | BrawlUnitLayer.Hero;
            data.CollisionLength = 0.6f;
            unit.AddComponent<StateComponent>();
            unit.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new()
            {
                [PropertyId.BaseMaxHealth] = 10000, [PropertyId.Health] = 10000,
            });
            return unit;
        }

        private static float Health(Unit unit) => unit.GetComponent<PropertyComponent>().GetFloat(PropertyId.Health);
        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
