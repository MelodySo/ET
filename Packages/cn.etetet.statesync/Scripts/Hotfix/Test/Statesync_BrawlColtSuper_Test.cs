using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlColtSuper_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            Fiber fiber = context.Fiber;
            BrawlCollisionRuleConfigCategory oldRules = fiber.GetSingleton<BrawlCollisionRuleConfigCategory>();
            BrawlProjectileConfigCategory old = fiber.GetSingleton<BrawlProjectileConfigCategory>();
            BrawlProjectileConfig config = new(40102, 0.06f, 0, 0, 10, 320, 32, 1, 0, 0, 0, 0, 0, 0.1f, 2);
            BrawlSkillConfig skill = new(40102, 40102, 12, 2, 1, 0, 0.08333f, 1, 0, 1, 1, 1, 12, 1, 0, 0, 0);
            try
            {
                fiber.AddSingleton(BrawlCollisionTestHelper.Category());
                fiber.AddSingleton(new BrawlProjectileConfigCategory(new Dictionary<int, BrawlProjectileConfig> { [40102] = config }));
                using Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "ColtSuperTest");
                UnitComponent units = scene.AddComponent<UnitComponent>();
                BrawlProjectileRoomComponent shots = scene.AddComponent<BrawlProjectileRoomComponent>();
                scene.AddComponent<BrawlLifecycleRoomComponent>();
                Unit owner = units.AddChild<Unit, int>(401);
                owner.Position = new float3(4, 0, 4);
                BrawlPracticeUnitComponent data = owner.AddComponent<BrawlPracticeUnitComponent>();
                data.Kind = BrawlPracticeUnitKind.Player; data.Faction = BrawlFaction.Red; data.PreviousPosition = owner.Position.xz;
                owner.AddComponent<StateComponent>(); owner.AddComponent<BrawlSpawnComponent>();
                owner.AddComponent<BrawlAmmoComponent>(); owner.AddComponent<BrawlSuperChargeComponent>();
                BrawlSkillCastComponent cast = owner.AddComponent<BrawlSkillCastComponent>();
                PropertyComponent properties = owner.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new()
                    { [PropertyId.Level] = 1, [PropertyId.MaxAmmo] = 3, [PropertyId.Ammo] = 3 });
                BrawlSuperChargeHelper.Initialize(owner, 2, 0);
                BrawlSuperChargeHelper.Add(owner, 0.4);
                if (BrawlSkillCastHelper.TryStart(owner, skill, config, BrawlSkillSlot.Super, new float2(0, 1), 0.6f, 0) || cast.Launches != null)
                    return Fail(101201, "Fractional super ammo allowed a cast.");
                if (shots.SkillNotices.Count != 0) return Fail(101211, "Rejected cast must not publish a success notice.");
                BrawlSuperChargeHelper.Add(owner, 1);
                cast.NextNormalCastAt = 5000;
                if (!BrawlSkillCastHelper.TryStart(owner, skill, config, BrawlSkillSlot.Super, new float2(0, 1), 0.6f, 0))
                    return Fail(101202, "Normal attack cooldown incorrectly blocked a charged super.");
                if (!Near(properties.GetDouble(PropertyId.SuperAmmo), 0.4) || properties.GetFloat(PropertyId.Ammo) != 3 ||
                    cast.NextSuperCastAt != 1000 || cast.NextNormalCastAt != 5000)
                    return Fail(101203, "Super consumed wrong resource or overwrote normal cooldown.");
                if (BrawlSkillCastHelper.TryStart(owner, skill, config, BrawlSkillSlot.Super, new float2(0, 1), 0.6f, 0) ||
                    !Near(properties.GetDouble(PropertyId.SuperAmmo), 0.4)) return Fail(101204, "Repeated active cast consumed additional charge.");
                if (shots.SkillNotices.Count != 1 || shots.SkillNotices[0].SkillId != skill.Id ||
                    shots.SkillNotices[0].Slot != BrawlSkillSlot.Super || shots.SkillNotices[0].UnitId != owner.Id)
                    return Fail(101212, "Success notice has wrong identity or rejected repeat generated a notice.");
                BrawlLevelConfig level = new(1, 1, 20, 20, new int[400], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
                for (long now = 0; now <= 950; now += 50) BrawlSkillSequenceHelper.Tick(owner, level, 1, now);
                if (shots.Children.Count != 12 || cast.Launches != null || BrawlSkillSequenceHelper.CanStart(cast, 950, BrawlSkillSlot.Super))
                    return Fail(101205, "Super did not produce twelve shots or ended its cooldown early.");
                cast.NextNormalCastAt = 0;
                if (!BrawlSkillSequenceHelper.CanStart(cast, 950, BrawlSkillSlot.NormalAttack) ||
                    !BrawlSkillSequenceHelper.CanStart(cast, 1000, BrawlSkillSlot.Super)) return Fail(101206, "Skill cooldowns are not independent.");
                BrawlProjectile first = null, second = null;
                int index = 0;
                foreach (Entity child in shots.Children.Values)
                {
                    BrawlProjectile shot = (BrawlProjectile)child;
                    if (index == 0) first = shot;
                    if (index == 1) second = shot;
                    float x = index % 2 == 0 ? 3.56f : 4.44f;
                    if (!Near(shot.Position.x, x) || shot.Position.y != 4 || shot.RemainingDistance != 12 || shot.EffectValue != 320 ||
                        shot.SuperChargeOnHit != 0.1f || shot.Rules.Length != 4)
                        return Fail(101207, "Super launch conditions do not match configured revolver geometry and projectile.");
                    ++index;
                }
                Unit wall = units.AddChild<Unit, int>(101);
                wall.Position = new float3(4, 0, 6);
                BrawlPracticeUnitComponent wallData = wall.AddComponent<BrawlPracticeUnitComponent>();
                wallData.Kind = BrawlPracticeUnitKind.NPC; wallData.NPCType = BrawlNPCType.Obstacle; wallData.Faction = BrawlFaction.NeutralHostile;
                BrawlNPCBlockHelper.Attach(wall,
                    new BrawlNPCConfig(101, 2, 0, 1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), 1, 1, 1, 0, 0, 0.2f, 0, 40, true));
                wall.AddComponent<StateComponent>(); wall.AddComponent<BrawlSpawnComponent>(); wall.AddComponent<BrawlDeathComponent>();
                wall.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new() { [PropertyId.Health] = 1 });
                BrawlProjectileCollisionHelper.Advance(first, units, level, 1, 0.3f);
                if (first.Finished || !wall.GetComponent<StateComponent>().Has(StateId.Dead) || !Near(properties.GetDouble(PropertyId.SuperAmmo), 0.5))
                    return Fail(101208, "Super pellet must destroy wall, penetrate, and recharge from actual damage.");
                BrawlProjectileCollisionHelper.Advance(second, units, level, 1, 0.3f);
                if (second.Finished || second.Position.y <= 6) return Fail(101209, "Later pellet remained blocked by dead wall.");
                if (BrawlSkillCastHelper.TryStart(owner, skill, config, (BrawlSkillSlot)99, new float2(0, 1), 0.6f, 1000) ||
                    !Near(properties.GetDouble(PropertyId.SuperAmmo), 0.5)) return Fail(101210, "Invalid skill slot was accepted or consumed resources.");
                return ErrorCode.ERR_Success;
            }
            finally { fiber.AddSingleton(old); fiber.AddSingleton(oldRules); }
        }
        private static bool Near(double a, double b) => Math.Abs(a - b) < 0.00001;
        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
