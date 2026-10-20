using System;
using System.Collections.Generic;

namespace ET
{
    public static class BrawlSkillPlanHelper
    {
        /// <summary>保持原枪械算法：相同时间的一轮弹丸共享一个表现步骤。</summary>
        public static BrawlSkillPlan FromProjectiles(BrawlProjectileLaunch[] launches)
        {
            if (launches == null || launches.Length == 0) throw new ArgumentException("A skill requires at least one launch.");
            List<BrawlSkillStep> steps = new();
            for (int index = 0; index < launches.Length;)
            {
                long delay = launches[index].DelayMilliseconds;
                List<BrawlSkillEffect> effects = new();
                do
                {
                    effects.Add(new BrawlSkillEffect { Kind = BrawlSkillEffectKind.Projectile, Launch = launches[index++] });
                } while (index < launches.Length && launches[index].DelayMilliseconds == delay);
                steps.Add(new BrawlSkillStep
                {
                    Id = steps.Count + 1, DelayMilliseconds = delay,
                    PresentationId = BrawlSkillPresentationId.GunShot, Effects = effects.ToArray(),
                });
            }
            BrawlSkillPlan plan = new()
            {
                Steps = steps.ToArray(), FaceDirection = true,
                LockFacing = launches[^1].DelayMilliseconds > 0,
            };
            Validate(plan);
            return plan;
        }

        public static void Validate(BrawlSkillPlan plan)
        {
            if (plan == null || plan.Steps == null || plan.Steps.Length == 0)
                throw new ArgumentException("A skill requires at least one step.");
            long previousTime = 0;
            HashSet<int> ids = new();
            foreach (BrawlSkillStep step in plan.Steps)
            {
                if (step.Id <= 0 || !ids.Add(step.Id) || step.DelayMilliseconds < previousTime ||
                    step.PresentationDurationMilliseconds < 0 || !float.IsFinite(step.PresentationRadius) ||
                    step.PresentationRadius < 0 || step.Effects == null)
                    throw new ArgumentException("Skill step identity, timing or presentation data is invalid.");
                previousTime = step.DelayMilliseconds;
                foreach (BrawlSkillEffect effect in step.Effects)
                    if (effect.Kind != BrawlSkillEffectKind.Projectile && effect.Kind != BrawlSkillEffectKind.CirclePulse)
                        throw new NotSupportedException("Unsupported skill effect.");
            }
        }
    }
}
