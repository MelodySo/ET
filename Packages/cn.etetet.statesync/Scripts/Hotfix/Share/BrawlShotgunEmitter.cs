using System;
using Unity.Mathematics;

namespace ET
{
    [EnableClass]
    public sealed class BrawlShotgunEmitter : IBrawlSkillEmitter
    {
        public BrawlSkillPlan Build(BrawlSkillConfig skill, BrawlProjectileConfig projectile, int level)
            => BrawlSkillPlanHelper.FromProjectiles(Emit(skill, projectile, level));

        public BrawlProjectileLaunch[] Emit(BrawlSkillConfig skill, BrawlProjectileConfig projectile, int level)
        {
            if (skill.SkillRangeType != (int)BrawlSkillRangeType.Sector || skill.SkillCastRangeType != 0 ||
                skill.EmitterParam1 != 1 || skill.EmitterParam2 != 0 || skill.EmitterParam3 != 0)
                throw new NotSupportedException($"技能 {skill.Id}：当前霰弹枪只支持单轮同时发射的扇形。");
            if (!math.isfinite(projectile.SuperChargeOnHit) || projectile.SuperChargeOnHit < 0 ||
                projectile.MaxHitCount < 0 || skill.ProjectilesPerVolley <= 0 || !math.isfinite(skill.SkillRangeParam1) || skill.SkillRangeParam1 <= 0 ||
                !math.isfinite(skill.SkillRangeParam2) || skill.SkillRangeParam2 < 0 || skill.SkillRangeParam2 > 360 ||
                !math.isfinite(projectile.Speed) || projectile.Speed <= 0 || !math.isfinite(projectile.Radius) || projectile.Radius <= 0)
                throw new ArgumentException($"技能 {skill.Id} 或弹丸 {projectile.Id} 的数量、射程、角度、速度、半径非法。");

            float effectValue = BrawlEffectValueHelper.Calculate(projectile.Value, projectile.ValueGrowthPerLevel, level);
            BrawlProjectileLaunch[] launches = new BrawlProjectileLaunch[skill.ProjectilesPerVolley];
            for (int i = 0; i < launches.Length; ++i)
            {
                float angle = launches.Length == 1 ? 0 : -skill.SkillRangeParam2 / 2 + skill.SkillRangeParam2 * i / (launches.Length - 1);
                float radians = math.radians(angle);
                launches[i] = new BrawlProjectileLaunch
                {
                    Offset = float2.zero,
                    Direction = new float2(math.sin(radians), math.cos(radians)),
                    Speed = projectile.Speed,
                    Radius = projectile.Radius,
                    Range = skill.SkillRangeParam1,
                    EffectValue = effectValue,
                    SuperChargeOnHit = projectile.SuperChargeOnHit,
                };
            }
            return launches;
        }
    }
}
