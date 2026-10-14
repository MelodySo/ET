using Unity.Mathematics;
namespace ET.Server
{
    /// <summary>英雄拥有的连发计划；执行时取当前位置，方向和弹丸数值使用施放快照。</summary>
    public static class BrawlSkillSequenceHelper
    {
        public static bool CanStart(BrawlSkillCastComponent cast, long now)
        {
            return cast.Launches == null && now >= cast.NextCastAt;
        }

        public static void Start(Unit unit, BrawlProjectileLaunch[] launches, int configId, float2 direction,
            float height, long now, long cooldown)
        {
            BrawlSkillCastComponent cast = unit.GetComponent<BrawlSkillCastComponent>();
            cast.Launches = launches; cast.NextIndex = 0; cast.StartedAt = now;
            cast.NextCastAt = checked(now + cooldown);
            cast.ProjectileConfigId = configId; cast.Direction = direction; cast.Height = height;
            cast.Faction = unit.GetComponent<BrawlPracticeUnitComponent>().Faction;
        }

        public static void Tick(Unit unit, BrawlLevelConfig level, int cellSize, long now)
        {
            BrawlSkillCastComponent cast = unit.GetComponent<BrawlSkillCastComponent>();
            if (cast.Launches == null) return;
            if (BrawlUnitLifeHelper.CannotAct(unit))
            {
                cast.Launches = null;
                return;
            }
            BrawlProjectileConfig config = unit.Fiber().GetSingleton<BrawlProjectileConfigCategory>().Get(cast.ProjectileConfigId);
            Scene room = unit.Scene();
            BrawlProjectileRoomComponent projectiles = room.GetComponent<BrawlProjectileRoomComponent>();
            float2 right = new float2(cast.Direction.y, -cast.Direction.x);
            while (cast.NextIndex < cast.Launches.Length && cast.Launches[cast.NextIndex].DelayMilliseconds <= now - cast.StartedAt)
            {
                BrawlProjectileLaunch launch = cast.Launches[cast.NextIndex++];
                BrawlProjectile projectile = projectiles.AddChild<BrawlProjectile>();
                projectile.ConfigId = config.Id; projectile.OwnerUnitId = unit.Id; projectile.Faction = cast.Faction;
                projectile.BlockMask = (BrawlProjectileBlockMask)config.BlockMask;
                projectile.TargetMask = (BrawlProjectileTargetMask)config.TargetMask;
                projectile.MaxHitCount = config.MaxHitCount; projectile.EffectValue = launch.EffectValue;
                projectile.HitEvent1 = (BrawlHitEventType)config.HitEvent1;
                projectile.HitEvent2 = (BrawlHitEventType)config.HitEvent2;
                projectile.Position = unit.Position.xz + right * launch.Offset.x + cast.Direction * launch.Offset.y;
                projectile.Direction = right * launch.Direction.x + cast.Direction * launch.Direction.y;
                projectile.Height = unit.Position.y + cast.Height;
                projectile.Speed = launch.Speed; projectile.Radius = launch.Radius; projectile.RemainingDistance = launch.Range;
                // 新弹丸只检查出生重叠；下一Tick才开始移动，不能多走出生前的一整步。
                BrawlProjectileCollisionHelper.Advance(projectile, room.GetComponent<UnitComponent>(), level, cellSize, 0);
            }
            if (cast.NextIndex == cast.Launches.Length) cast.Launches = null;
        }
    }
}
