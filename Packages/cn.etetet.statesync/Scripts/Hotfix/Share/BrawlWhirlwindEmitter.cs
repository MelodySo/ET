using System;
using Unity.Mathematics;

namespace ET
{
    [EnableClass]
    public sealed class BrawlWhirlwindEmitter : IBrawlSkillEmitter
    {
        public BrawlSkillPlan Build(BrawlSkillConfig skill, BrawlProjectileConfig projectile, int level)
        {
            if (skill.SkillRangeType != (int)BrawlSkillRangeType.Circle || skill.SkillCastRangeType != 0 ||
                skill.EmitterParam1 != 1 || skill.EmitterParam3 != 0 || skill.ProjectilesPerVolley != 1)
                throw new NotSupportedException($"Skill {skill.Id}: whirlwind requires one self-centered circle pulse.");
            if (!math.isfinite(skill.SkillRangeParam1) || skill.SkillRangeParam1 <= 0 ||
                !math.isfinite(skill.EmitterParam2) || skill.EmitterParam2 <= 0 || skill.EmitterParam2 * 1000d > int.MaxValue ||
                projectile.MaxHitCount != 0 || !math.isfinite(projectile.SuperChargeOnHit) || projectile.SuperChargeOnHit < 0)
                throw new ArgumentException($"Skill {skill.Id}: invalid whirlwind radius, delay or hit settings.");
            long delay = (long)Math.Round(skill.EmitterParam2 * 1000d);
            if (delay <= 0) throw new ArgumentException("Whirlwind delay must be at least one millisecond.");
            BrawlProjectileLaunch pulse = new()
            {
                Direction = new float2(0, 1), Radius = skill.SkillRangeParam1,
                EffectValue = BrawlEffectValueHelper.Calculate(projectile.Value, projectile.ValueGrowthPerLevel, level),
                SuperChargeOnHit = projectile.SuperChargeOnHit,
            };
            return new BrawlSkillPlan
            {
                // 前摇不等于持续锁向；圆形技能允许独立移动和面向。
                FaceDirection = false, LockFacing = false, ImmediatePresentation = true,
                Steps = new[]
                {
                    new BrawlSkillStep
                    {
                        Id = 1, PresentationId = BrawlSkillPresentationId.WhirlwindCharge,
                        PresentationDurationMilliseconds = delay, PresentationRadius = skill.SkillRangeParam1,
                        Effects = Array.Empty<BrawlSkillEffect>(),
                    },
                    new BrawlSkillStep
                    {
                        Id = 2, DelayMilliseconds = delay, PresentationId = BrawlSkillPresentationId.WhirlwindRelease,
                        PresentationRadius = skill.SkillRangeParam1,
                        Effects = new[] { new BrawlSkillEffect { Kind = BrawlSkillEffectKind.CirclePulse, Launch = pulse } },
                    },
                },
            };
        }
    }
}
