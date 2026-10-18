using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public sealed class Statesync_BrawlCombatAI_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            Fiber fiber = context.Fiber;
            BrawlAIConfigCategory oldAI = fiber.GetSingleton<BrawlAIConfigCategory>();
            BrawlLevelConfigCategory oldLevels = fiber.GetSingleton<BrawlLevelConfigCategory>();
            BrawlHeroConfigCategory oldHeroes = fiber.GetSingleton<BrawlHeroConfigCategory>();
            BrawlSkillConfigCategory oldSkills = fiber.GetSingleton<BrawlSkillConfigCategory>();
            BrawlProjectileConfigCategory oldProjectiles = fiber.GetSingleton<BrawlProjectileConfigCategory>();
            BrawlCollisionRuleConfigCategory oldRules = fiber.GetSingleton<BrawlCollisionRuleConfigCategory>();
            try
            {
                BrawlAIConfigCategory aiConfig = new()
                {
                    Trees = new() { [101] = Tree(), [401] = Tree(true) },
                    PracticeOpponents = new() { [101] = 401, [401] = 101 },
                };
                aiConfig.ResolveRef();
                if (aiConfig.GetPracticeOpponent(101) != 401 || aiConfig.GetPracticeOpponent(401) != 101)
                    return Fail(109101, "Practice opponents are not reciprocal.");
                fiber.AddSingleton(aiConfig);
                int[] cells = new int[1600];
                BrawlLevelConfig level = new(98702, 102, 40, 40, cells, Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
                fiber.AddSingleton(new BrawlLevelConfigCategory(new() { [level.Id] = level }));
                fiber.AddSingleton(new BrawlHeroConfigCategory(new()
                {
                    [101] = new(101, 1, 1, 10101, 10102, 0, 1000, 100, 3, 0, .3f, 1.2f, 1),
                    [401] = new(401, 1, 1, 40101, 40102, 0, 1000, 100, 3, 0, .3f, 1.2f, 1),
                }));
                fiber.AddSingleton(new BrawlSkillConfigCategory(new()
                {
                    [10101] = new(10101, 10101, 5, 1, 1, 0, 0, 3, 1.5f, .5f, 1, 2, 7.67f, 30, 0, 0, 0),
                    [10102] = new(10102, 10102, 5, 1, 1, 0, 0, 1, 1.5f, .5f, 1, 2, 7.67f, 45, 0, 0, 0),
                    [40101] = new(40101, 10101, 6, 2, 1, 0, .16f, 3, 1.6f, .8f, 1, 1, 9, .7f, 0, 0, 0),
                    [40102] = new(40102, 10102, 12, 2, 1, 0, .08333f, 1, 0, 1, 1, 1, 12, 1, 0, 0, 0),
                }));
                fiber.AddSingleton(new BrawlProjectileConfigCategory(new()
                {
                    [10101] = new(10101, .05f, 1, 0, 8, 300, 30, 1, 0, 0, 0, 0, 0, .1f, 1),
                    [10102] = new(10102, .05f, 1, 0, 8, 300, 30, 1, 0, 0, 0, 0, 0, .1f, 2),
                }));
                fiber.AddSingleton(BrawlCollisionTestHelper.Category());
                BrawlCollisionRuleConfig[] normalRules = BrawlCollisionTestHelper.Rules(1);
                BrawlCollisionRuleConfig[] superRules = BrawlCollisionTestHelper.Rules(2);
                using Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "CombatAITest");
                UnitComponent units = scene.AddComponent<UnitComponent>();
                BrawlProjectileRoomComponent shots = scene.AddComponent<BrawlProjectileRoomComponent>();
                Unit player = Create(units, 401, new float3(7.18f, 0, 0));
                Unit shelly = Create(units, 101, float3.zero);
                shelly.AddComponent<BrawlAIComponent>();
                BrawlMoveComponent move = shelly.GetComponent<BrawlMoveComponent>();
                BrawlSkillCastComponent cast = shelly.GetComponent<BrawlSkillCastComponent>();
                PropertyComponent properties = shelly.GetComponent<PropertyComponent>();
                BrawlAIHelper.Tick(shelly, level.Id, 1, player.Id, 1000);
                if (math.lengthsq(move.DesiredDirection) == 0 || cast.Launches != null)
                    return Fail(109102, "Shelly must chase beyond skill range minus 0.5m even when super is available.");
                player.Position = new float3(7.67f - .5f, 0, 0);
                BrawlAIHelper.Tick(shelly, level.Id, 1, player.Id, 1050);
                if (cast.Launches == null || cast.Slot != BrawlSkillSlot.Super || math.lengthsq(move.DesiredDirection) != 0)
                    return Fail(109103, "Shelly must stop at 7.17m and prefer super.");
                BrawlSkillSequenceHelper.Tick(shelly, level, 1, 1050);
                BrawlAIHelper.Tick(shelly, level.Id, 1, player.Id, 1100);
                if (cast.Slot != BrawlSkillSlot.NormalAttack || cast.Launches == null)
                    return Fail(109104, "Normal attack must follow when super is depleted.");
                BrawlSkillSequenceHelper.Tick(shelly, level, 1, 1100);
                properties.Set(PropertyId.Ammo, 0);
                BrawlAIHelper.Tick(shelly, level.Id, 1, player.Id, 2000);
                if (cast.Launches != null || math.lengthsq(move.DesiredDirection) != 0)
                    return Fail(109105, "Empty ammo must wait at attack position without moving closer.");

                Unit wall = Create(units, 1, new float3(3, 0, 0));
                BrawlPracticeUnitComponent data = wall.GetComponent<BrawlPracticeUnitComponent>();
                data.UnitLayerMask = BrawlUnitLayer.Tile | BrawlUnitLayer.Destroyable;
                data.CollisionLength = 1; data.CollisionWidth = 1; data.BlocksMovement = true;
                if (BrawlAISightHelper.IsClear(units, level, 1, float2.zero, player.Position.xz, .2f, normalRules, BrawlFaction.Red) ||
                    !BrawlAISightHelper.IsClear(units, level, 1, float2.zero, player.Position.xz, .2f, superRules, BrawlFaction.Red))
                    return Fail(109106, "Normal and super must filter destroyable walls independently.");
                BrawlAIHelper.Tick(shelly, level.Id, 1, player.Id, 2025);
                if (!shelly.GetComponent<BrawlAIComponent>().Chasing || math.lengthsq(move.DesiredDirection) == 0 || cast.Launches != null)
                    return Fail(109116, "Blocked normal attack with no super must chase around the wall, even within attack distance.");
                properties.Set(PropertyId.SuperAmmo, 1);
                BrawlAIHelper.Tick(shelly, level.Id, 1, player.Id, 2050);
                if (cast.Launches == null || cast.Slot != BrawlSkillSlot.Super)
                    return Fail(109107, "Destroyable wall must not gate the super branch through normal visibility.");
                data.UnitLayerMask = BrawlUnitLayer.Tile;
                if (BrawlAISightHelper.IsClear(units, level, 1, float2.zero, player.Position.xz, .2f, superRules, BrawlFaction.Red))
                    return Fail(109108, "Indestructible wall must block super sight.");
                wall.GetComponent<StateComponent>().AddSource(StateId.Dead, 1);
                if (!BrawlAISightHelper.IsClear(units, level, 1, float2.zero, player.Position.xz, .2f, normalRules, BrawlFaction.Red))
                    return Fail(109109, "Destroyed wall must not keep blocking sight.");
                int terrainIndex = 20 * 40 + 23;
                float2 terrainCenter = BrawlTerrainMovementHelper.CellPosition(level, terrainIndex, 1);
                cells[terrainIndex] = (int)BrawlTileType.Wall;
                if (BrawlAISightHelper.IsClear(units, level, 1, terrainCenter - new float2(2, 0), terrainCenter + new float2(2, 0), .2f, superRules, BrawlFaction.Red))
                    return Fail(109110, "Terrain wall must block both skills.");
                cells[terrainIndex] = (int)BrawlTileType.Grass;
                if (!BrawlAISightHelper.IsClear(units, level, 1, terrainCenter - new float2(2, 0), terrainCenter + new float2(2, 0), .2f, normalRules, BrawlFaction.Red))
                    return Fail(109111, "Grass must not block AI sight.");

                Unit colt = Create(units, 401, new float3(0, 0, -5));
                colt.AddComponent<BrawlAIComponent>();
                player.Position = new float3(11.6f, 0, -5);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 3000);
                BrawlSkillCastComponent coltCast = colt.GetComponent<BrawlSkillCastComponent>();
                if (!colt.GetComponent<BrawlAIComponent>().Chasing || coltCast.Launches != null)
                    return Fail(109112, "Colt must chase beyond its super range minus margin.");
                player.Position = new float3(11.5f, 0, -5);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 3050);
                if (coltCast.Launches == null || coltCast.Slot != BrawlSkillSlot.Super || math.lengthsq(colt.GetComponent<BrawlMoveComponent>().DesiredDirection) != 0)
                    return Fail(109113, "Colt must start super at 11.5m without requiring normal attack range.");
                long id = coltCast.PresentationCastId;
                int notices = shots.SkillNotices.Count;
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 3100);
                if (coltCast.PresentationCastId != id || shots.SkillNotices.Count != notices)
                    return Fail(109114, "Active burst must not start another skill each AI tick.");
                BrawlSkillSequenceHelper.Tick(colt, level, 1, 4050);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4100);
                if (coltCast.Launches != null || !colt.GetComponent<BrawlAIComponent>().Chasing)
                    return Fail(109117, "After super is spent, Colt must close to its independent normal attack range.");
                player.Position = new float3(8.5f, 0, -5);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4150);
                if (coltCast.Launches == null || coltCast.Slot != BrawlSkillSlot.NormalAttack || math.lengthsq(colt.GetComponent<BrawlMoveComponent>().DesiredDirection) != 0)
                    return Fail(109118, "Colt normal attack must stop at 8.5m.");

                BrawlMoveComponent coltMove = colt.GetComponent<BrawlMoveComponent>();
                float2 lockedDirection = coltCast.Direction;
                long movingCastId = coltCast.PresentationCastId;
                double ammoBeforeMoving = colt.GetComponent<PropertyComponent>().GetDouble(PropertyId.Ammo);
                notices = shots.SkillNotices.Count;
                player.Position = new float3(8.5f, 0, -4.8f);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4160);
                if (math.lengthsq(coltMove.DesiredDirection) > .000001f)
                    return Fail(109136, "A target within the full skill rectangle width must not trigger offset following.");
                player.Position = new float3(8.5f, 0, -4.6f);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4170);
                if (coltMove.DesiredDirection.y <= 0 || math.abs(coltMove.DesiredDirection.x) > .0001f)
                    return Fail(109137, "A target just outside the skill half-width must cause lateral correction, not radial chase.");
                BrawlSkillConfig originalNormal = fiber.GetSingleton<BrawlSkillConfigCategory>().Get(40101);
                fiber.GetSingleton<BrawlSkillConfigCategory>().GetAll()[40101] = new(40101, 10101, 6, 2, 1, 0, .16f, 3, 1.6f, .8f, 1, 1, 9, 2, 0, 0, 0);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4180);
                if (math.lengthsq(coltMove.DesiredDirection) > .000001f)
                    return Fail(109138, "Rectangle correction must read skill width, not individual projectile radius.");
                fiber.GetSingleton<BrawlSkillConfigCategory>().GetAll()[40101] = originalNormal;
                player.Position = new float3(7.5f, 0, -5);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4200);
                if (coltMove.DesiredDirection.x >= 0 || math.abs(coltMove.DesiredDirection.y) > .0001f)
                    return Fail(109128, "Colt must retreat when the target approaches during a burst.");
                player.Position = new float3(7.5f, 0, -4);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4250);
                if (coltMove.DesiredDirection.y <= 0)
                    return Fail(109129, "Colt must follow the target's positive lateral displacement.");
                player.Position = new float3(7.5f, 0, -6);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4300);
                if (coltMove.DesiredDirection.y >= 0)
                    return Fail(109130, "Colt must follow the target's negative lateral displacement.");
                player.Position = new float3(9.1f, 0, -5);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4350);
                if (!colt.GetComponent<BrawlAIComponent>().Chasing || coltMove.DesiredDirection.x <= 0)
                    return Fail(109131, "Colt must chase while firing when the target leaves range.");
                if (coltCast.PresentationCastId != movingCastId || math.any(coltCast.Direction != lockedDirection) ||
                    math.any(coltMove.FacingDirection != lockedDirection) || shots.SkillNotices.Count != notices ||
                    colt.GetComponent<PropertyComponent>().GetDouble(PropertyId.Ammo) != ammoBeforeMoving)
                    return Fail(109132, "Following must not restart the cast, consume ammo or rotate its locked firing direction.");
                player.Position = new float3(7.5f, 0, -5);
                wall.Position = new float3(3, 0, -5);
                data.UnitLayerMask = BrawlUnitLayer.Tile | BrawlUnitLayer.Destroyable;
                wall.GetComponent<StateComponent>().RemoveSource(StateId.Dead, 1);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4400);
                if (!colt.GetComponent<BrawlAIComponent>().Chasing)
                    return Fail(109133, "An obstructed firing line must switch offset following back to chase.");
                wall.Position = new float3(-1, 0, -5);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4450);
                if (!colt.GetComponent<BrawlAIComponent>().Chasing)
                    return Fail(109134, "An obstructed correction destination must fall back to chase instead of crossing the wall.");
                wall.Position = new float3(3, 0, 0);
                wall.GetComponent<StateComponent>().AddSource(StateId.Dead, 1);
                colt.GetComponent<StateComponent>().AddSource(StateId.Stunned, 2);
                BrawlAIHelper.Tick(colt, level.Id, 1, player.Id, 4500);
                if (math.lengthsq(coltMove.DesiredDirection) != 0)
                    return Fail(109135, "A controlled Colt must stop following.");
                colt.GetComponent<StateComponent>().RemoveSource(StateId.Stunned, 2);

                BTBrawlAttackPosition normalPosition = new() { Slot = BrawlSkillSlot.NormalAttack };
                BTBrawlAttackPosition superPosition = new() { Slot = BrawlSkillSlot.Super };
                fiber.GetSingleton<BrawlSkillConfigCategory>().GetAll()[40101] = new(40101, 10101, 6, 2, 1, 0, .16f, 3, 1.6f, .8f, 1, 1, 6, .7f, 0, 0, 0);
                player.Position = new float3(5.51f, 0, -5);
                if (BrawlAICombatHelper.AtAttackPosition(colt, normalPosition))
                    return Fail(109119, "Changing skill range must immediately change the AI threshold.");
                player.Position = new float3(5.5f, 0, -5);
                if (!BrawlAICombatHelper.AtAttackPosition(colt, normalPosition))
                    return Fail(109120, "The new skill range minus margin boundary must be accepted.");

                colt.Position = float3.zero; player.Position = new float3(5.5f, 0, 0);
                wall.GetComponent<StateComponent>().RemoveSource(StateId.Dead, 1);
                data.UnitLayerMask = BrawlUnitLayer.Tile | BrawlUnitLayer.Destroyable;
                int projectileCount = shots.Children.Count;
                notices = shots.SkillNotices.Count;
                if (BrawlAICombatHelper.AtAttackPosition(colt, normalPosition) || !BrawlAICombatHelper.AtAttackPosition(colt, superPosition))
                    return Fail(109121, "Each skill must resolve its own projectile rule group.");
                // 同一普攻仅换成穿墙规则组，就应允许射击；不能靠 Super 槽位硬判穿墙。
                fiber.GetSingleton<BrawlProjectileConfigCategory>().GetAll()[10101] = new(10101, .05f, 1, 0, 8, 300, 30, 1, 0, 0, 0, 0, 0, .1f, 2);
                if (!BrawlAICombatHelper.AtAttackPosition(colt, normalPosition))
                    return Fail(109122, "Normal attack must honor a changed projectile rule group.");
                // 同一大招的规则改为挡可摧毁墙，也必须跟随配置停止开火。
                fiber.GetSingleton<BrawlCollisionRuleConfigCategory>().GetAll()[202] = new(202, 2, 40, 0, 1, true, false);
                data.Faction = BrawlFaction.Blue;
                if (BrawlAICombatHelper.AtAttackPosition(colt, superPosition))
                    return Fail(109123, "Super must honor BlockProjectile instead of assuming destroyable walls are passable.");
                data.Faction = BrawlFaction.Red;
                if (!BrawlAICombatHelper.AtAttackPosition(colt, superPosition))
                    return Fail(109124, "AI sight must use the same faction relation matching as projectiles.");
                if (shots.Children.Count != projectileCount || shots.SkillNotices.Count != notices || wall.GetComponent<StateComponent>().Has(StateId.Dead))
                    return Fail(109125, "Sight queries must not emit projectiles, skill notices or hit events.");
                BrawlCollisionRuleConfig[] effectsOnly = { new(999, 9, 8, 0, 3, false, true) };
                cells[terrainIndex] = (int)BrawlTileType.Wall;
                if (!BrawlAISightHelper.IsClear(units, level, 1, terrainCenter - new float2(2, 0), terrainCenter + new float2(2, 0), .2f, effectsOnly, BrawlFaction.Red))
                    return Fail(109126, "Terrain visibility must use BlockProjectile, not ApplyHitEvents.");
                if (!BrawlAISightGeometryHelper.Rectangle(float2.zero, new float2(4, 4), .2f, new float2(2, 2), new float2(.5f)) ||
                    BrawlAISightGeometryHelper.Rectangle(float2.zero, new float2(4, 4), .2f, new float2(2, -2), new float2(.5f)))
                    return Fail(109115, "Rotated finite sight rectangle is incorrect.");
                return ErrorCode.ERR_Success;
            }
            finally
            {
                fiber.AddSingleton(oldAI); fiber.AddSingleton(oldLevels); fiber.AddSingleton(oldHeroes);
                fiber.AddSingleton(oldSkills); fiber.AddSingleton(oldProjectiles); fiber.AddSingleton(oldRules);
            }
        }

        private static BrawlAIRoot Tree(bool followDuringCast = false)
        {
            BrawlAIRoot tree = new() { Children = new()
            {
            new BTSelector { Children = new()
            {
                new BTSequence { Children = new() { new BTBrawlHasActiveCast(), new BTBrawlIdle() } },
                new BTSequence { Children = new() { new BTBrawlAttackPosition { Slot = BrawlSkillSlot.Super }, new BTBrawlUseSkill { Slot = BrawlSkillSlot.Super } } },
                new BTSequence { Children = new() { new BTBrawlAttackPosition { Slot = BrawlSkillSlot.NormalAttack }, new BTBrawlUseSkill { Slot = BrawlSkillSlot.NormalAttack } } },
                new BTSequence { Children = new() { new BTBrawlAttackPosition { Slot = BrawlSkillSlot.NormalAttack }, new BTBrawlIdle() } },
                new BTBrawlChase(),
            } },
            } };
            if (followDuringCast)
            {
                BTNode selector = tree.Children[0];
                selector.Children[0].Children[1] = new BTBrawlFollowTarget();
                selector.Children[1].Children.Add(new BTBrawlFollowTarget());
                selector.Children[2].Children.Add(new BTBrawlFollowTarget());
            }
            return tree;
        }

        private static Unit Create(UnitComponent units, int heroId, float3 position)
        {
            Unit unit = units.AddChild<Unit, int>(heroId); unit.Position = position;
            BrawlPracticeUnitComponent data = unit.AddComponent<BrawlPracticeUnitComponent>();
            data.Faction = BrawlFaction.Red; data.UnitLayerMask = BrawlUnitLayer.Hero | BrawlUnitLayer.Battle;
            unit.AddComponent<StateComponent>(); unit.AddComponent<BrawlSpawnComponent>();
            unit.AddComponent<BrawlSkillCastComponent>(); unit.AddComponent<BrawlAmmoComponent>(); unit.AddComponent<BrawlSuperChargeComponent>();
            BrawlMoveComponent move = unit.AddComponent<BrawlMoveComponent>(); move.Radius = .3f; move.Speed = 3; move.FacingDirection = new float2(0, 1);
            unit.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new()
            {
                [PropertyId.Level] = 1, [PropertyId.MaxAmmo] = 3, [PropertyId.Ammo] = 3,
                [PropertyId.MaxSuperAmmo] = 1, [PropertyId.SuperAmmo] = 1,
            });
            return unit;
        }
        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
