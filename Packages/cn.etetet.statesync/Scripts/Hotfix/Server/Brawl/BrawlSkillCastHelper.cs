using System;
using Unity.Mathematics;

namespace ET.Server
{
    public static class BrawlSkillCastHelper
    {
        public static void Cast(Session session, C2G_BrawlCastSkill message)
        {
            BrawlPracticeSessionComponent state = session.GetComponent<BrawlPracticeSessionComponent>();
            if (state == null || state.EntryId != message.EntryId || message.Sequence <= state.LastAttackSequence) return;
            BrawlSkillSlot slot = (BrawlSkillSlot)message.Slot;
            if (slot != BrawlSkillSlot.NormalAttack && slot != BrawlSkillSlot.Super) return;
            float2 direction = new float2(message.X, message.Z);
            if (!math.all(math.isfinite(direction)) || math.any(math.abs(direction) > 1) || math.lengthsq(direction) <= 0) return;
            state.LastAttackSequence = message.Sequence;
            direction = math.normalizesafe(direction);
            if (math.lengthsq(direction) <= 0) return;

            Fiber fiber = state.Fiber();
            BrawlHeroConfig hero = fiber.GetSingleton<BrawlHeroConfigCategory>().Get(state.HeroId);
            int skillId = slot == BrawlSkillSlot.Super ? hero.SuperSkillId : hero.NormalAttackSkillId;
            BrawlSkillConfig skill = fiber.GetSingleton<BrawlSkillConfigCategory>().GetOrDefault(skillId);
            if (skill == null) return;
            BrawlProjectileConfig config = fiber.GetSingleton<BrawlProjectileConfigCategory>().GetOrDefault(skill.ProjectileId);
            if (config == null) return;
            // 先结算已有时间，再取当前等级与位置，避免新弹丸参与释放前的补帧。
            BrawlPracticeMovementHelper.Update(state);
            Scene room = state.Room;
            Unit unit = room.GetComponent<UnitComponent>().Get(state.MyUnitId);
            long castTime = room.GetComponent<BrawlLifecycleRoomComponent>().SimulationTime;
            if (!TryStart(unit, skill, config, slot, direction, hero.CollisionHeight / 2, castTime)) return;
            room.GetComponent<BrawlProjectileRoomComponent>().CurrentTick = state.Tick;
            BrawlSkillSequenceHelper.Tick(unit, fiber.GetSingleton<BrawlLevelConfigCategory>().Get(state.LevelId), state.CellSize, castTime);
            BrawlProjectileHelper.SendSnapshot(state);
        }

        public static bool TryStart(Unit unit, BrawlSkillConfig skill, BrawlProjectileConfig config, BrawlSkillSlot slot,
            float2 direction, float height, long now)
        {
            if (!math.all(math.isfinite(direction)) || math.lengthsq(direction) <= 0 || BrawlUnitLifeHelper.CannotAct(unit) ||
                !BrawlSkillSequenceHelper.CanStart(unit.GetComponent<BrawlSkillCastComponent>(), now, slot)) return false;
            IBrawlSkillEmitter emitter = BrawlSkillEmitterHelper.Create((BrawlEmitterType)skill.EmitterType);
            if (emitter == null) return false;
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            BrawlProjectileLaunch[] launches;
            BrawlCollisionRuleConfig[] rules;
            try
            {
                if (!math.isfinite(config.Lifetime) || config.Lifetime < 0) throw new ArgumentException("Projectile lifetime is invalid.");
                rules = BrawlCollisionRuleHelper.Resolve(unit.Fiber().GetSingleton<BrawlCollisionRuleConfigCategory>(), config.CollisionRuleGroupId);
                launches = emitter.Emit(skill, config, properties.GetInt(PropertyId.Level));
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException)
            {
                Log.Warning(exception.Message);
                return false;
            }
            long cooldown;
            try
            {
                // 大招使用配置冷却；普攻继续保持此前雪莉无CD、左轮有CD的规则。
                cooldown = slot == BrawlSkillSlot.Super || skill.EmitterType == (int)BrawlEmitterType.Revolver
                    ? BrawlUnitLifeHelper.Milliseconds(skill.CooldownTime) : 0;
            }
            catch (ArgumentException exception) { Log.Warning(exception.Message); return false; }
            bool consumed = slot == BrawlSkillSlot.Super ? BrawlSuperChargeHelper.TryConsume(unit) : BrawlAmmoHelper.TryConsume(unit, now);
            if (!consumed) return false;
            BrawlSkillSequenceHelper.Start(unit, launches, config.Id, math.normalizesafe(direction), height, now, cooldown, slot, rules, skill.Id);
            return true;
        }

    }
}

