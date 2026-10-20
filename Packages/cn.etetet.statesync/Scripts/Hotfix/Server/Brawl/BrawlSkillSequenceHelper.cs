using Unity.Mathematics;
namespace ET.Server
{
    /// <summary>英雄拥有的技能步骤计划；执行时取当前位置，数值使用施放快照。</summary>
    public static class BrawlSkillSequenceHelper
    {
        public static bool CanStart(BrawlSkillCastComponent cast, long now, BrawlSkillSlot slot = BrawlSkillSlot.NormalAttack)
        {
            if (slot != BrawlSkillSlot.NormalAttack && slot != BrawlSkillSlot.Super) return false;
            return cast.Plan == null && now >= (slot == BrawlSkillSlot.Super ? cast.NextSuperCastAt : cast.NextNormalCastAt);
        }

        public static void Start(Unit unit, BrawlProjectileLaunch[] launches, int configId, float2 direction,
            float height, long now, long cooldown, BrawlSkillSlot slot = BrawlSkillSlot.NormalAttack, BrawlCollisionRuleConfig[] rules = null, int skillId = 0)
            => Start(unit, BrawlSkillPlanHelper.FromProjectiles(launches), configId, direction, height, now, cooldown, slot, rules, skillId);

        public static void Start(Unit unit, BrawlSkillPlan plan, int configId, float2 direction,
            float height, long now, long cooldown, BrawlSkillSlot slot = BrawlSkillSlot.NormalAttack, BrawlCollisionRuleConfig[] rules = null, int skillId = 0)
        {
            BrawlSkillPlanHelper.Validate(plan);
            BrawlSkillCastComponent cast = unit.GetComponent<BrawlSkillCastComponent>();
            cast.Rules = rules ?? BrawlCollisionRuleHelper.Resolve(unit.Fiber().GetSingleton<BrawlCollisionRuleConfigCategory>(),
                unit.Fiber().GetSingleton<BrawlProjectileConfigCategory>().Get(configId).CollisionRuleGroupId);
            cast.Plan = plan; cast.NextIndex = 0; cast.StartedAt = now;
            if (slot == BrawlSkillSlot.Super) cast.NextSuperCastAt = checked(now + cooldown);
            else cast.NextNormalCastAt = checked(now + cooldown);
            cast.ProjectileConfigId = configId; cast.Direction = direction; cast.Height = height;
            cast.Faction = unit.GetComponent<BrawlPracticeUnitComponent>().Faction;
            cast.SkillId = skillId; cast.Slot = slot;
            BrawlSkillUseNoticeHelper.Start(unit, now);
        }

        public static void Tick(Unit unit, BrawlLevelConfig level, int cellSize, long now)
        {
            BrawlSkillCastComponent cast = unit.GetComponent<BrawlSkillCastComponent>();
            if (cast.Plan == null) return;
            if (BrawlUnitLifeHelper.CannotAct(unit))
            {
                BrawlSkillUseNoticeHelper.Record(unit, BrawlSkillUsePhase.Interrupted, now);
                cast.Plan = null;
                BrawlAmmoHelper.ResetClock(unit, now);
                return;
            }
            while (cast.NextIndex < cast.Plan.Steps.Length && cast.Plan.Steps[cast.NextIndex].DelayMilliseconds <= now - cast.StartedAt)
            {
                BrawlSkillStep step = cast.Plan.Steps[cast.NextIndex++];
                foreach (BrawlSkillEffect effect in step.Effects)
                    BrawlSkillEffectHelper.Execute(unit, effect, level, cellSize);
                BrawlSkillUseNoticeHelper.Record(unit, BrawlSkillUsePhase.StepExecuted, now, step);
            }
            if (cast.NextIndex == cast.Plan.Steps.Length)
            {
                BrawlSkillUseNoticeHelper.Record(unit, BrawlSkillUsePhase.Completed, now);
                cast.Plan = null;
                BrawlAmmoHelper.ResetClock(unit, now);
            }
        }
    }
}
