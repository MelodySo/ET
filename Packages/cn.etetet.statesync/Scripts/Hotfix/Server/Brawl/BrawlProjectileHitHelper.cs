using Unity.Mathematics;

namespace ET.Server
{
    public static class BrawlProjectileHitHelper
    {
        public static void Apply(BrawlProjectile projectile, Unit target)
        {
            ApplyEvent(projectile.HitEvent1, projectile.EffectValue, target);
            ApplyEvent(projectile.HitEvent2, projectile.EffectValue, target);
        }

        private static void ApplyEvent(BrawlHitEventType hitEvent, float value, Unit target)
        {
            if (hitEvent != BrawlHitEventType.Damage) return;
            StateComponent states = target.GetComponent<StateComponent>();
            if (states.Has(StateId.Invincible) || states.Has(StateId.Dead) || states.Has(StateId.Spawning)) return;
            // 有效作战/墙体目标在创建时保证具有属性组件；不在命中时兜底创建。
            PropertyComponent properties = target.GetComponent<PropertyComponent>();
            float health = properties.GetFloat(PropertyId.Health);
            float reduction = math.clamp(properties.GetFloat(PropertyId.DamageReductionRatio), 0, 1);
            float damage = math.max(0, value) * (1 - reduction);
            properties.Set(PropertyId.Health, math.max(0, health - damage));
            BrawlUnitLifeHelper.CheckDeath(target);
            // 只标记死亡并停止行动；不移除实体/阻挡，不自动安排复活。
        }
    }
}
