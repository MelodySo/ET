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
            BrawlProjectileLaunch[] launches;
            try { launches = emitter.Emit(skill, config, properties.GetInt(PropertyId.Level)); }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException)
            {
                Log.Warning(exception.Message);
                return;
            }
            BrawlProjectileRoomComponent projectiles = room.GetComponent<BrawlProjectileRoomComponent>();
            float2 right = new float2(direction.y, -direction.x);
            foreach (BrawlProjectileLaunch launch in launches)
            {
                BrawlProjectile projectile = projectiles.AddChild<BrawlProjectile>();
                projectile.ConfigId = config.Id;
                projectile.OwnerUnitId = unit.Id;
                projectile.Faction = unit.GetComponent<BrawlPracticeUnitComponent>().Faction;
                projectile.BlockMask = (BrawlProjectileBlockMask)config.BlockMask;
                projectile.TargetMask = (BrawlProjectileTargetMask)config.TargetMask;
                projectile.MaxHitCount = config.MaxHitCount;
                projectile.EffectValue = launch.EffectValue;
                projectile.HitEvent1 = (BrawlHitEventType)config.HitEvent1;
                projectile.HitEvent2 = (BrawlHitEventType)config.HitEvent2;
                projectile.Position = unit.Position.xz + right * launch.Offset.x + direction * launch.Offset.y;
                projectile.Direction = right * launch.Direction.x + direction * launch.Direction.y;
                // 高度只用于 Sphere 展示，不参与 XZ 平面轨迹计算。
                projectile.Height = unit.Position.y + hero.CollisionHeight / 2;
                projectile.Speed = launch.Speed;
                projectile.Radius = launch.Radius;
                projectile.RemainingDistance = launch.Range;
                // 出生点重叠立即判定，避免贴墙开火先穿出墙体再开始检测。
                BrawlProjectileCollisionHelper.Advance(projectile, room.GetComponent<UnitComponent>(),
                    fiber.GetSingleton<BrawlLevelConfigCategory>().Get(state.LevelId), state.CellSize, 0);
            }
            BrawlProjectileHelper.SendSnapshot(state);
        }

    }
}

