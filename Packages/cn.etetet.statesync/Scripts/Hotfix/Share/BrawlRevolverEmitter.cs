using System;
using Unity.Mathematics;

namespace ET
{
    [EnableClass]
    public sealed class BrawlRevolverEmitter : IBrawlSkillEmitter
    {
        public BrawlProjectileLaunch[] Emit(BrawlSkillConfig skill, BrawlProjectileConfig projectile, int level)
        {
            if (skill.SkillRangeType != (int)BrawlSkillRangeType.Rectangle || skill.SkillCastRangeType != 0 ||
                skill.EmitterParam1 != 1 || skill.EmitterParam2 != 0)
                throw new NotSupportedException($"技能 {skill.Id}：左轮发射器当前只支持单轮矩形连发。");
            if (!math.isfinite(projectile.SuperChargeOnHit) || projectile.SuperChargeOnHit < 0 ||
                skill.ProjectilesPerVolley <= 0 || projectile.MaxHitCount < 0 ||
                !math.isfinite(skill.SkillRangeParam1) || skill.SkillRangeParam1 <= 0 ||
                !math.isfinite(skill.SkillRangeParam2) || !math.isfinite(projectile.Radius) || projectile.Radius <= 0 ||
                skill.SkillRangeParam2 < projectile.Radius * 2 || !math.isfinite(projectile.Speed) || projectile.Speed <= 0 ||
                !math.isfinite(skill.EmitterParam3) || skill.EmitterParam3 <= 0 ||
                (double)skill.EmitterParam3 * skill.ProjectilesPerVolley * 1000 > int.MaxValue)
                throw new ArgumentException($"技能 {skill.Id} 或弹丸 {projectile.Id} 的连发数量、间隔、范围或弹丸参数非法。");

            float offset = skill.SkillRangeParam2 / 2 - projectile.Radius;
            float value = BrawlEffectValueHelper.Calculate(projectile.Value, projectile.ValueGrowthPerLevel, level);
            BrawlProjectileLaunch[] launches = new BrawlProjectileLaunch[skill.ProjectilesPerVolley];
            for (int i = 0; i < launches.Length; ++i)
            {
                launches[i] = new BrawlProjectileLaunch
                {
                    Offset = new float2(i % 2 == 0 ? -offset : offset, 0),
                    Direction = new float2(0, 1),
                    Speed = projectile.Speed, Radius = projectile.Radius, Range = skill.SkillRangeParam1,
                    EffectValue = value,
                    SuperChargeOnHit = projectile.SuperChargeOnHit,
                    // 从整轮起点计算，避免逐发按Tick取整而累计漂移。
                    DelayMilliseconds = (long)Math.Round(i * (double)skill.EmitterParam3 * 1000),
                };
            }
            return launches;
        }
    }
}
