using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlSuperCharge_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "SuperChargeTest");
            UnitComponent units = scene.AddComponent<UnitComponent>();
            BrawlProjectileRoomComponent shots = scene.AddComponent<BrawlProjectileRoomComponent>();
            scene.AddComponent<BrawlLifecycleRoomComponent>();
            Unit owner = Combatant(units, BrawlPracticeUnitKind.Player, BrawlFaction.Red, 2);
            Unit npc = Combatant(units, BrawlPracticeUnitKind.NPC, BrawlFaction.NeutralHostile, 5);
            BrawlSuperChargeHelper.Initialize(owner, 2, 0);
            BrawlSuperChargeHelper.Tick(owner, 1000);
            if (Charge(owner) != 0) return Fail(101101, "Hero must start empty without automatic charge.");
            BrawlLevelConfig level = new(1, 1, 20, 20, new int[400], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
            BrawlProjectile shot = Shot(shots, owner);
            shot.HitEvent2 = BrawlHitEventType.Damage;
            BrawlProjectileCollisionHelper.Advance(shot, units, level, 1, 0.5f);
            if (Charge(owner) != 0.25 || npc.GetComponent<PropertyComponent>().GetFloat(PropertyId.Health) != 4980)
                return Fail(101102, "Two damage events against an NPC must award configured charge exactly once.");
            shot.Position = new float2(2, 4);
            BrawlProjectileCollisionHelper.Advance(shot, units, level, 1, 0.5f);
            if (Charge(owner) != 0.25) return Fail(101103, "Same projectile charged twice for the same target.");
            npc.GetComponent<StateComponent>().AddSource(StateId.Invincible, 1);
            BrawlProjectileHitHelper.Apply(Shot(shots, owner), npc);
            if (Charge(owner) != 0.25) return Fail(101104, "Invulnerable target awarded charge.");
            npc.GetComponent<StateComponent>().RemoveSource(StateId.Invincible, 1);
            npc.GetComponent<PropertyComponent>().Set(PropertyId.DamageReductionRatio, 1);
            BrawlProjectileHitHelper.Apply(Shot(shots, owner), npc);
            if (Charge(owner) != 0.25) return Fail(101105, "Zero actual damage awarded charge.");
            npc.GetComponent<PropertyComponent>().Set(PropertyId.DamageReductionRatio, 0);
            npc.GetComponent<PropertyComponent>().Set(PropertyId.Health, 1);
            BrawlProjectileHitHelper.Apply(Shot(shots, owner), npc);
            if (Charge(owner) != 0.5 || !npc.GetComponent<StateComponent>().Has(StateId.Dead))
                return Fail(101106, "Lethal overkill must award the full configured charge.");
            Unit enemy = Combatant(units, BrawlPracticeUnitKind.Bot, BrawlFaction.Blue, 7);
            BrawlProjectile arbitrary = Shot(shots, owner);
            arbitrary.ConfigId = 987654; arbitrary.SuperChargeOnHit = 0;
            BrawlProjectileHitHelper.Apply(arbitrary, enemy);
            if (Charge(owner) != 0.5) return Fail(101107, "Zero configured charge was ignored.");
            arbitrary.SuperChargeOnHit = 0.25f;
            BrawlProjectileHitHelper.Apply(arbitrary, enemy);
            if (Charge(owner) != 0.75) return Fail(101108, "Charge incorrectly depends on skill or projectile identity.");
            owner.GetComponent<StateComponent>().AddSource(StateId.Dead, -2);
            BrawlProjectile late = Shot(shots, owner);
            BrawlProjectileHitHelper.Apply(late, enemy);
            if (Charge(owner) != 0.75) return Fail(101109, "Dead owner received charge from an in-flight projectile.");
            Unit replacement = Combatant(units, BrawlPracticeUnitKind.Player, BrawlFaction.Red, 2);
            BrawlSuperChargeHelper.Initialize(replacement, 2, 1000);
            BrawlSuperChargeHelper.Restore(replacement, 0.75);
            units.Remove(owner.Id);
            BrawlProjectileHitHelper.Apply(late, enemy);
            if (Charge(replacement) != 0.75) return Fail(101110, "Old projectile charged replacement entity.");
            BrawlSuperChargeHelper.Add(replacement, 10);
            if (Charge(replacement) != 2) return Fail(101111, "Super charge overflowed capacity two.");
            BrawlSuperChargeHelper.Restore(replacement, 0.75);
            replacement.GetComponent<PropertyComponent>().Set(PropertyId.BaseSuperChargeRegenRate, 0.5);
            replacement.AddComponent<BrawlSkillCastComponent>().Plan = new BrawlSkillPlan();
            BrawlSuperChargeHelper.Tick(replacement, 2000);
            if (Charge(replacement) != 1.25) return Fail(101112, "Automatic charge did not run during attack sequence.");
            replacement.GetComponent<StateComponent>().AddSource(StateId.Spawning, -3);
            BrawlSuperChargeHelper.Tick(replacement, 3000);
            if (Charge(replacement) != 1.25) return Fail(101113, "Spawning unit automatically charged.");
            BrawlProjectileHitHelper.Apply(Shot(shots, enemy), replacement);
            if (Charge(enemy) != 0) return Fail(101114, "Spawning target awarded charge.");
            BrawlProjectileHitHelper.Apply(Shot(shots, npc), enemy);
            if (npc.GetComponent<BrawlSuperChargeComponent>() != null) return Fail(101115, "NPC unexpectedly owns super charge component.");
            return ErrorCode.ERR_Success;
        }

        private static Unit Combatant(UnitComponent units, BrawlPracticeUnitKind kind, BrawlFaction faction, float x)
        {
            Unit unit = units.AddChild<Unit, int>(1);
            unit.Position = new float3(x, 0, 4);
            BrawlPracticeUnitComponent data = unit.AddComponent<BrawlPracticeUnitComponent>();
            data.Kind = kind; data.Faction = faction; data.NPCType = BrawlNPCType.Combatant;
            data.UnitLayerMask = BrawlUnitLayer.Battle;
            data.CollisionLength = 0.6f; data.PreviousPosition = unit.Position.xz;
            unit.AddComponent<StateComponent>(); unit.AddComponent<BrawlSpawnComponent>(); unit.AddComponent<BrawlDeathComponent>();
            unit.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new() { [PropertyId.Health] = 5000, [PropertyId.BaseMaxHealth] = 5000 });
            if (kind != BrawlPracticeUnitKind.NPC)
            {
                unit.AddComponent<BrawlSuperChargeComponent>();
                BrawlSuperChargeHelper.Initialize(unit, 2, 0);
            }
            return unit;
        }

        private static BrawlProjectile Shot(BrawlProjectileRoomComponent shots, Unit owner)
        {
            BrawlProjectile shot = shots.AddChild<BrawlProjectile>();
            shot.OwnerUnitId = owner.Id; shot.Faction = owner.GetComponent<BrawlPracticeUnitComponent>().Faction;
            shot.Position = owner.Position.xz; shot.Direction = new float2(1, 0);
            shot.Speed = 10; shot.Radius = 0.04f; shot.RemainingDistance = 9;
            shot.Rules = BrawlCollisionTestHelper.Rules(2);
            shot.EffectValue = 10; shot.HitEvent1 = BrawlHitEventType.Damage; shot.SuperChargeOnHit = 0.25f;
            return shot;
        }
        private static double Charge(Unit unit) => unit.GetComponent<PropertyComponent>().GetDouble(PropertyId.SuperAmmo);
        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
