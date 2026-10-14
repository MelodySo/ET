using System;
namespace ET.Server
{
    public static class BrawlSuperChargeHelper
    {
        public static void Initialize(Unit unit, int capacity, long now)
        {
            if (capacity < 0) throw new ArgumentException("Super ammo capacity cannot be negative.");
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            properties.Set(PropertyId.MaxSuperAmmo, capacity);
            properties.Set(PropertyId.SuperAmmo, 0);
            properties.Set(PropertyId.BaseSuperChargeRegenRate, 0);
            unit.GetComponent<BrawlSuperChargeComponent>().LastUpdateAt = now;
        }

        public static void Add(Unit unit, double amount)
        {
            if (!double.IsFinite(amount) || amount < 0) throw new ArgumentException("Super charge must be finite and non-negative.");
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            double value = Math.Min(Math.Max(0, properties.GetInt(PropertyId.MaxSuperAmmo)), properties.GetDouble(PropertyId.SuperAmmo) + amount);
            if (value != properties.GetDouble(PropertyId.SuperAmmo)) properties.Set(PropertyId.SuperAmmo, value);
        }

        public static bool TryConsume(Unit unit)
        {
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            double ammo = Math.Min(properties.GetDouble(PropertyId.SuperAmmo), Math.Max(0, properties.GetInt(PropertyId.MaxSuperAmmo)));
            if (ammo < 1) return false;
            properties.Set(PropertyId.SuperAmmo, ammo - 1);
            return true;
        }

        public static void OnDamagingHit(BrawlProjectile projectile, Unit target)
        {
            if (projectile.SuperChargeOnHit <= 0 || !float.IsFinite(projectile.SuperChargeOnHit)) return;
            // 仅依赖弹丸配置，不检查它来自普通攻击还是超级技能。
            Unit owner = projectile.Scene().GetComponent<UnitComponent>().Get(projectile.OwnerUnitId);
            if (owner == null || owner.Id == target.Id || owner.GetComponent<BrawlPracticeUnitComponent>().Kind == BrawlPracticeUnitKind.NPC) return;
            StateComponent states = owner.GetComponent<StateComponent>();
            if (states.Has(StateId.Dead) || states.Has(StateId.Spawning)) return;
            if (!BrawlFactionHelper.IsEnemy(projectile.Faction, target.GetComponent<BrawlPracticeUnitComponent>().Faction)) return;
            // 本版英雄和NPC有效受伤目标均可贡献充能；未来NPC资格筛选在这里扩展。
            Add(owner, projectile.SuperChargeOnHit);
        }

        public static void SaveOnDeath(Unit unit)
        {
            int slot = unit.GetComponent<BrawlSpawnComponent>().ParticipantSlot;
            if (slot < 0) return;
            BrawlLifecycleRoomComponent life = unit.Scene().GetComponent<BrawlLifecycleRoomComponent>();
            BrawlParticipant participant = life.Participants[slot];
            participant.SuperAmmo = unit.GetComponent<PropertyComponent>().GetDouble(PropertyId.SuperAmmo);
            life.Participants[slot] = participant;
        }

        public static void Restore(Unit unit, double saved)
        {
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            properties.Set(PropertyId.SuperAmmo, Math.Clamp(saved, 0, Math.Max(0, properties.GetInt(PropertyId.MaxSuperAmmo))));
        }

        public static void Tick(Unit unit, long now)
        {
            BrawlSuperChargeComponent charge = unit.GetComponent<BrawlSuperChargeComponent>();
            long from = Math.Max(charge.LastUpdateAt, unit.GetComponent<BrawlSpawnComponent>().CompleteAt);
            charge.LastUpdateAt = now;
            StateComponent states = unit.GetComponent<StateComponent>();
            if (now <= from || states.Has(StateId.Dead) || states.Has(StateId.Spawning)) return;
            // 不因发射中的连发计划暂停自动充能，默认速率0。
            Add(unit, unit.GetComponent<PropertyComponent>().GetDouble(PropertyId.SuperChargeRegenRate) * ((now - from) / 1000d));
        }
    }
}
