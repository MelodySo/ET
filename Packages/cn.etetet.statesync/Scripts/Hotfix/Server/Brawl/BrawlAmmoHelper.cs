using System;
namespace ET.Server
{
    public static class BrawlAmmoHelper
    {
        // 本轮约定的重生资源规则；首次入场仍为满弹。
        private const double RespawnAmmoRatio = 1d / 3;

        public static void Initialize(Unit unit, BrawlSkillConfig skill, bool isRespawn, long now)
        {
            if (skill.AmmoCapacity < 0 || !float.IsFinite(skill.ReloadTime) || skill.ReloadTime < 0)
                throw new ArgumentException($"技能 {skill.Id} 的弹药容量或装填时间非法。");
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            properties.Set(PropertyId.MaxAmmo, skill.AmmoCapacity);
            properties.Set(PropertyId.BaseReloadRate, skill.ReloadTime > 0 ? 1000d / BrawlUnitLifeHelper.Milliseconds(skill.ReloadTime) : 0);
            properties.Set(PropertyId.Ammo, skill.AmmoCapacity * (isRespawn ? RespawnAmmoRatio : 1));
            ResetClock(unit, now);
        }

        public static bool TryConsume(Unit unit, long now)
        {
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            double ammo = Math.Min(properties.GetDouble(PropertyId.Ammo), Math.Max(0, properties.GetInt(PropertyId.MaxAmmo)));
            if (ammo < 1) return false;
            // 一次技能仅消耗一格；发射期间暂停装填，但保留未完成的小数进度。
            properties.Set(PropertyId.Ammo, ammo - 1);
            ResetClock(unit, now);
            return true;
        }

        public static void ResetClock(Unit unit, long now)
        {
            unit.GetComponent<BrawlAmmoComponent>().LastUpdateAt = now;
        }

        public static void Tick(Unit unit, long now)
        {
            BrawlAmmoComponent ammoState = unit.GetComponent<BrawlAmmoComponent>();
            long from = ammoState.LastUpdateAt;
            ammoState.LastUpdateAt = now;
            StateComponent states = unit.GetComponent<StateComponent>();
            if (states.Has(StateId.Dead) || states.Has(StateId.Spawning) || unit.GetComponent<BrawlSkillCastComponent>().Launches != null) return;
            // 出生完成所在Tick不能计入此前仍在出生阶段的时间。
            from = Math.Max(from, unit.GetComponent<BrawlSpawnComponent>().CompleteAt);
            if (now <= from) return;
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            int maxAmmo = Math.Max(0, properties.GetInt(PropertyId.MaxAmmo));
            double old = properties.GetDouble(PropertyId.Ammo);
            double value = Math.Min(maxAmmo, old + properties.GetDouble(PropertyId.ReloadRate) * ((now - from) / 1000d));
            // 消除浮点积分在完整格边界附近的舍入残差。
            if (Math.Abs(value - Math.Round(value)) < 1e-9) value = Math.Round(value);
            if (value != old) properties.Set(PropertyId.Ammo, value);
        }
    }
}
