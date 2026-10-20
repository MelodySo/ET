using Unity.Mathematics;

namespace ET.Server
{
    public static class BrawlSkillEffectHelper
    {
        public static void Execute(Unit unit, BrawlSkillEffect effect, BrawlLevelConfig level, int cellSize)
        {
            BrawlSkillCastComponent cast = unit.GetComponent<BrawlSkillCastComponent>();
            BrawlProjectileConfig config = unit.Fiber().GetSingleton<BrawlProjectileConfigCategory>().Get(cast.ProjectileConfigId);
            Scene room = unit.Scene();
            BrawlProjectile projectile = room.GetComponent<BrawlProjectileRoomComponent>().AddChild<BrawlProjectile>();
            BrawlProjectileLaunch launch = effect.Launch;
            bool pulse = effect.Kind == BrawlSkillEffectKind.CirclePulse;
            float2 right = new(cast.Direction.y, -cast.Direction.x);
            try
            {
                projectile.ConfigId = config.Id; projectile.OwnerUnitId = unit.Id; projectile.Faction = cast.Faction;
                projectile.Rules = cast.Rules;
                projectile.Lifetime = config.Lifetime;
                projectile.MaxHitCount = pulse ? 0 : config.MaxHitCount;
                projectile.EffectValue = launch.EffectValue;
                projectile.SuperChargeOnHit = launch.SuperChargeOnHit;
                projectile.HitEvent1 = (BrawlHitEventType)config.HitEvent1;
                projectile.HitEvent2 = (BrawlHitEventType)config.HitEvent2;
                projectile.Position = unit.Position.xz + right * launch.Offset.x + cast.Direction * launch.Offset.y;
                projectile.Direction = right * launch.Direction.x + cast.Direction * launch.Direction.y;
                projectile.Height = unit.Position.y + cast.Height;
                projectile.Speed = pulse ? 0 : launch.Speed;
                projectile.Radius = launch.Radius;
                projectile.RemainingDistance = pulse ? 0 : launch.Range;
                // 零位移只采样本次重叠。圆形规则不因首个目标终止，所有目标沿用命中去重与伤害规则。
                BrawlProjectileCollisionHelper.Advance(projectile, room.GetComponent<UnitComponent>(), level, cellSize, 0);
            }
            finally
            {
                // 瞬时范围只留下命中事件，不进入客户端飞行物快照。
                if (pulse) projectile.Dispose();
            }
        }
    }
}
