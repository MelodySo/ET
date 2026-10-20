using System;
using Unity.Mathematics;
namespace ET.Server
{
    public static class BrawlAICombatHelper
    {
        public static bool AtAttackPosition(Unit unit, BTBrawlAttackPosition node)
            => AtAttackPosition(unit, node.Slot, node.RangeMargin, node.SightWidth);

        private static bool AtAttackPosition(Unit unit, BrawlSkillSlot slot, float rangeMargin, float sightWidth)
        {
            Unit target = BrawlAIHelper.GetTarget(unit);
            if (target == null || !TryGetSkill(unit, slot, out BrawlSkillConfig skill, out BrawlProjectileConfig projectile)) return false;
            if (!math.isfinite(skill.SkillRangeParam1) || skill.SkillRangeParam1 <= 0 ||
                !math.isfinite(rangeMargin) || rangeMargin < 0) return false;
            // 当前扇形/矩形发射器均使用 SkillRangeParam1 作为弹丸射程。
            float distance = math.max(0, skill.SkillRangeParam1 - rangeMargin);
            if (math.distancesq(unit.Position.xz, target.Position.xz) > distance * distance) return false;
            return HasClearSight(unit, target, projectile, sightWidth);
        }

        private static bool HasClearSight(Unit unit, Unit target, BrawlProjectileConfig projectile, float sightWidth)
        {
            BrawlAIComponent ai = unit.GetComponent<BrawlAIComponent>();
            BrawlLevelConfig level = unit.Fiber().GetSingleton<BrawlLevelConfigCategory>().Get(ai.LevelId);
            BrawlCollisionRuleConfig[] rules;
            try
            {
                rules = BrawlCollisionRuleHelper.Resolve(unit.Fiber().GetSingleton<BrawlCollisionRuleConfigCategory>(), projectile.CollisionRuleGroupId);
            }
            catch (ArgumentException exception) { Log.Warning(exception.Message); return false; }
            return BrawlAISightHelper.IsClear(unit.Scene().GetComponent<UnitComponent>(), level, ai.CellSize,
                unit.Position.xz, target.Position.xz, sightWidth, rules, unit.GetComponent<BrawlPracticeUnitComponent>().Faction);
        }

        public static void FollowTarget(Unit unit, BTBrawlFollowTarget node)
        {
            Unit target = BrawlAIHelper.GetTarget(unit);
            BrawlSkillCastComponent cast = unit.GetComponent<BrawlSkillCastComponent>();
            if (target == null || cast.Plan == null || BrawlUnitLifeHelper.CannotAct(unit))
            { BrawlAIHelper.Idle(unit); return; }
            BrawlAIComponent ai = unit.GetComponent<BrawlAIComponent>();
            BrawlMoveComponent move = unit.GetComponent<BrawlMoveComponent>();
            if (!TryGetSkill(unit, cast.Slot, out BrawlSkillConfig skill, out BrawlProjectileConfig projectile) ||
                skill.SkillRangeType != (int)BrawlSkillRangeType.Rectangle ||
                !math.isfinite(skill.SkillRangeParam1) || skill.SkillRangeParam1 <= 0 ||
                !math.isfinite(skill.SkillRangeParam2) || skill.SkillRangeParam2 <= 0)
            { BrawlAIHelper.Idle(unit); return; }
            if (!HasClearSight(unit, target, projectile, node.SightWidth))
            { BrawlAIHelper.Chase(unit, node.RepathMilliseconds, node.MaxSearchNodes); return; }

            // 使用整段技能矩形，不使用单颗子弹半径，也不记录开火时双方的相对位置。
            float2 forward = math.normalizesafe(cast.Direction);
            float2 right = new float2(forward.y, -forward.x);
            float2 toTarget = target.Position.xz - unit.Position.xz;
            float longitudinal = math.dot(toTarget, forward);
            float lateral = math.dot(toTarget, right);
            if (longitudinal > skill.SkillRangeParam1)
            { BrawlAIHelper.Chase(unit, node.RepathMilliseconds, node.MaxSearchNodes); return; }
            float halfWidth = skill.SkillRangeParam2 * 0.5f;
            float lateralCorrection = lateral - math.clamp(lateral, -halfWidth, halfWidth);
            float preferredDistance = math.max(0, skill.SkillRangeParam1 - node.RangeMargin);
            float2 delta = right * lateralCorrection + forward * (longitudinal - preferredDistance);
            float2 destination = unit.Position.xz + delta;
            float length = math.length(delta);
            float step = move.Speed * BrawlMovementSettings.TickSeconds;
            if (length <= BrawlMovementSettings.ContactTolerance || step <= 0)
            { BrawlAIHelper.Idle(unit); return; }
            BrawlLevelConfig level = unit.Fiber().GetSingleton<BrawlLevelConfigCategory>().Get(ai.LevelId);
            if (!BrawlPathfindingHelper.CanTraverse(unit.Scene().GetComponent<UnitComponent>(), unit.Id,
                    level, ai.CellSize, unit.Position.xz, destination, move.Radius))
            { BrawlAIHelper.Chase(unit, node.RepathMilliseconds, node.MaxSearchNodes); return; }

            // 只写移动意图，不改施法方向；实际位置仍由公共移动碰撞流程推进。
            BrawlAIHelper.Idle(unit);
            move.DesiredDirection = delta / math.max(length, step);
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
