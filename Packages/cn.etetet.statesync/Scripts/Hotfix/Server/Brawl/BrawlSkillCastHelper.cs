using System;
using Unity.Mathematics;

namespace ET.Server
{
    public static class BrawlSkillCastHelper
    {
        public static void Cast(Session session, C2G_BrawlNormalAttack message)
        {
            BrawlPracticeSessionComponent state = session.GetComponent<BrawlPracticeSessionComponent>();
            if (state == null || state.EntryId != message.EntryId || message.Sequence <= state.LastAttackSequence) return;
            float2 direction = new float2(message.X, message.Z);
            if (!math.all(math.isfinite(direction)) || math.any(math.abs(direction) > 1) || math.lengthsq(direction) <= 0) return;
            state.LastAttackSequence = message.Sequence;
            direction = math.normalizesafe(direction);
            if (math.lengthsq(direction) <= 0) return;

            Fiber fiber = state.Fiber();
            BrawlHeroConfig hero = fiber.GetSingleton<BrawlHeroConfigCategory>().Get(state.HeroId);
            BrawlSkillConfig skill = fiber.GetSingleton<BrawlSkillConfigCategory>().GetOrDefault(hero.NormalAttackSkillId);
            if (skill == null) return;
            IBrawlSkillEmitter emitter = BrawlSkillEmitterHelper.Create((BrawlEmitterType)skill.EmitterType);
            if (emitter == null) return;
            BrawlProjectileConfig config = fiber.GetSingleton<BrawlProjectileConfigCategory>().GetOrDefault(skill.ProjectileId);
            if (config == null) return;
            // 先结算已有时间，再取当前等级与位置，避免新弹丸参与释放前的补帧。
            BrawlPracticeMovementHelper.Update(state);
            Scene room = state.Room;
            Unit unit = room.GetComponent<UnitComponent>().Get(state.MyUnitId);
            PropertyComponent properties = unit.GetComponent<PropertyComponent>();
            if (BrawlUnitLifeHelper.CannotAct(unit)) return;
            long castTime = room.GetComponent<BrawlLifecycleRoomComponent>().SimulationTime;
            if (!BrawlSkillSequenceHelper.CanStart(unit.GetComponent<BrawlSkillCastComponent>(), castTime)) return;
            BrawlProjectileLaunch[] launches;
            try { launches = emitter.Emit(skill, config, properties.GetInt(PropertyId.Level)); }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException)
            {
                Log.Warning(exception.Message);
                return;
            }
            long cooldown;
            try
            {
                // 本轮只启用左轮的冷却；保持此前雪莉无CD的行为。
                cooldown = skill.EmitterType == (int)BrawlEmitterType.Revolver
                    ? BrawlUnitLifeHelper.Milliseconds(skill.CooldownTime) : 0;
            }
            catch (ArgumentException exception) { Log.Warning(exception.Message); return; }
            BrawlSkillSequenceHelper.Start(unit, launches, config.Id, direction, hero.CollisionHeight / 2, castTime, cooldown);
            BrawlSkillSequenceHelper.Tick(unit, fiber.GetSingleton<BrawlLevelConfigCategory>().Get(state.LevelId), state.CellSize, castTime);
            BrawlProjectileHelper.SendSnapshot(state);
        }

    }
}

