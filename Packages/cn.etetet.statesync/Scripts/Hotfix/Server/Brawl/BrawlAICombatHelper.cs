using System;
using Unity.Mathematics;
namespace ET.Server
{
    public static class BrawlAICombatHelper
    {
        public static bool AtAttackPosition(Unit unit, BTBrawlAttackPosition node)
        {
            Unit target = BrawlAIHelper.GetTarget(unit);
            if (target == null || !TryGetSkill(unit, node.Slot, out BrawlSkillConfig skill, out BrawlProjectileConfig projectile)) return false;
            if (!math.isfinite(skill.SkillRangeParam1) || skill.SkillRangeParam1 <= 0 ||
                !math.isfinite(node.RangeMargin) || node.RangeMargin < 0) return false;
            // 当前扇形/矩形发射器均使用 SkillRangeParam1 作为弹丸射程。
            float distance = math.max(0, skill.SkillRangeParam1 - node.RangeMargin);
            if (math.distancesq(unit.Position.xz, target.Position.xz) > distance * distance) return false;
            BrawlAIComponent ai = unit.GetComponent<BrawlAIComponent>();
            BrawlLevelConfig level = unit.Fiber().GetSingleton<BrawlLevelConfigCategory>().Get(ai.LevelId);
            BrawlCollisionRuleConfig[] rules;
            try
            {
                rules = BrawlCollisionRuleHelper.Resolve(unit.Fiber().GetSingleton<BrawlCollisionRuleConfigCategory>(), projectile.CollisionRuleGroupId);
            }
            catch (ArgumentException exception) { Log.Warning(exception.Message); return false; }
            return BrawlAISightHelper.IsClear(unit.Scene().GetComponent<UnitComponent>(), level, ai.CellSize,
                unit.Position.xz, target.Position.xz, node.SightWidth, rules, unit.GetComponent<BrawlPracticeUnitComponent>().Faction);
        }

        public static bool TryUseSkill(Unit unit, BrawlSkillSlot slot)
        {
            Unit target = BrawlAIHelper.GetTarget(unit);
            if (target == null || target.GetComponent<StateComponent>().Has(StateId.Spawning)) return false;
            BrawlAIComponent ai = unit.GetComponent<BrawlAIComponent>();
            if (slot == BrawlSkillSlot.NormalAttack && ai.Now < ai.NextNormalAttackAt) return false;
            if (!TryGetSkill(unit, slot, out BrawlSkillConfig skill, out BrawlProjectileConfig projectile)) return false;
            BrawlHeroConfig hero = unit.Fiber().GetSingleton<BrawlHeroConfigCategory>().Get(unit.ConfigId);
            float2 direction = math.normalizesafe(target.Position.xz - unit.Position.xz,
                unit.GetComponent<BrawlMoveComponent>().FacingDirection);
            // 仅限制 Bot 的决策节奏；不改变玩家普攻CD/资源和现有连发结算。
            if (!math.isfinite(skill.CooldownTime) || skill.CooldownTime < 0) return false;
            long interval = BrawlUnitLifeHelper.Milliseconds(skill.CooldownTime);
            if (!BrawlSkillCastHelper.TryStart(unit, skill, projectile, slot, direction, hero.CollisionHeight / 2, ai.Now)) return false;
            if (slot == BrawlSkillSlot.NormalAttack)
                ai.NextNormalAttackAt = checked(ai.Now + System.Math.Max(BrawlMovementSettings.TickMilliseconds, interval));
            unit.GetComponent<BrawlMoveComponent>().FacingDirection = direction;
            BrawlAIHelper.Idle(unit);
            return true;
        }

        private static bool TryGetSkill(Unit unit, BrawlSkillSlot slot, out BrawlSkillConfig skill, out BrawlProjectileConfig projectile)
        {
            skill = null; projectile = null;
            if (slot != BrawlSkillSlot.NormalAttack && slot != BrawlSkillSlot.Super) return false;
            BrawlHeroConfig hero = unit.Fiber().GetSingleton<BrawlHeroConfigCategory>().Get(unit.ConfigId);
            int skillId = slot == BrawlSkillSlot.Super ? hero.SuperSkillId : hero.NormalAttackSkillId;
            skill = unit.Fiber().GetSingleton<BrawlSkillConfigCategory>().GetOrDefault(skillId);
            if (skill == null) return false;
            projectile = unit.Fiber().GetSingleton<BrawlProjectileConfigCategory>().GetOrDefault(skill.ProjectileId);
            return projectile != null;
        }
    }
}
