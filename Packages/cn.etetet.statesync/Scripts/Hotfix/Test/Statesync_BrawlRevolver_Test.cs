using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlRevolver_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            BrawlSkillConfig skill = new(40101, 40101, 6, 2, 1, 0, 0.16f, 3, 1.6f, 0.8f, 1, 1, 9, 0.7f, 0, 0, 0);
            BrawlProjectileConfig config = new(40101, 0.04f, 1, 0, 10, 360, 36, 1, 0, 0, 0, 0, 0, 0.1f, 1);
            IBrawlSkillEmitter emitter = BrawlSkillEmitterHelper.Create(BrawlEmitterType.Revolver);
            BrawlProjectileLaunch[] launches = emitter.Emit(skill, config, 2);
            if (launches.Length != 6) return Fail(100901, "Revolver count must come from config.");
            for (int i = 0; i < launches.Length; ++i)
            {
                if (math.abs(launches[i].Offset.x - (i % 2 == 0 ? -0.31f : 0.31f)) > 0.00001f ||
                    launches[i].Offset.y != 0 || math.any(launches[i].Direction != new float2(0, 1)) ||
                    launches[i].DelayMilliseconds != i * 160 || launches[i].Range != 9 || launches[i].EffectValue != 396 || launches[i].SuperChargeOnHit != 0.1f)
                    return Fail(100902, "Revolver offsets, direction, timing, range or level snapshot are incorrect.");
            }
            try
            {
                emitter.Emit(new BrawlSkillConfig(1, 40101, 6, 2, 1, 0, 0.16f, 3, 1, 0.8f, 1, 1, 9, 0.05f, 0, 0, 0), config, 1);
                return Fail(100903, "Emitter accepted a width smaller than the projectile diameter.");
            }
            catch (ArgumentException) { }

            Fiber fiber = context.Fiber;
            BrawlCollisionRuleConfigCategory oldRules = fiber.GetSingleton<BrawlCollisionRuleConfigCategory>();
            BrawlProjectileConfigCategory previousConfig = fiber.GetSingleton<BrawlProjectileConfigCategory>();
            try
            {
                fiber.AddSingleton(BrawlCollisionTestHelper.Category());
                fiber.AddSingleton(new BrawlProjectileConfigCategory(new Dictionary<int, BrawlProjectileConfig> { [config.Id] = config }));
                using Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "RevolverTest");
                UnitComponent units = scene.AddComponent<UnitComponent>();
                BrawlProjectileRoomComponent projectiles = scene.AddComponent<BrawlProjectileRoomComponent>();
                Unit owner = units.AddChild<Unit, int>(401);
                owner.Position = new float3(4, 0, 4);
                owner.AddComponent<BrawlPracticeUnitComponent>().Faction = BrawlFaction.Red;
                StateComponent states = owner.AddComponent<StateComponent>();
                BrawlSkillCastComponent cast = owner.AddComponent<BrawlSkillCastComponent>();
                owner.AddComponent<BrawlAmmoComponent>();
                owner.AddComponent<BrawlSpawnComponent>();
                PropertyComponent properties = owner.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new());
                BrawlAmmoHelper.Initialize(owner, skill, false, 1000);
                BrawlLevelConfig level = new(1, 1, 20, 20, new int[400], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
                if (!BrawlAmmoHelper.TryConsume(owner, 1000)) return Fail(100911, "Full ammo failed to start burst.");
                BrawlSkillSequenceHelper.Start(owner, launches, config.Id, new float2(1, 0), 0.6f, 1000, 800, skillId: skill.Id);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1000);
                if (projectiles.SkillNotices.Count != 2 || projectiles.SkillNotices[0].Phase != BrawlSkillUsePhase.Started ||
                    projectiles.SkillNotices[1].Phase != BrawlSkillUsePhase.Fired || !projectiles.SkillNotices[0].Sustained ||
                    projectiles.SkillNotices[0].CastId != projectiles.SkillNotices[1].CastId)
                    return Fail(100913, "Burst start and first fire notices are missing or mismatched.");
                if (projectiles.Children.Count != 1 || BrawlSkillSequenceHelper.CanStart(cast, 1000))
                    return Fail(100904, "First shot must be immediate and active burst must reject a second cast.");
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1150);
                if (projectiles.Children.Count != 1) return Fail(100905, "Second shot fired early.");
                owner.Position = new float3(5, 0, 6);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1200);
                BrawlProjectile latest = null;
                foreach (Entity child in projectiles.Children.Values) latest = (BrawlProjectile)child;
                if (projectiles.Children.Count != 2 || latest == null || math.distance(latest.Position, new float2(5, 5.69f)) > 0.00001f ||
                    math.any(latest.Direction != new float2(1, 0)) || latest.EffectValue != 396)
                    return Fail(100906, "Later shot must follow owner position while preserving locked aim and value.");
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1350);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1500);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1650);
                if (projectiles.Children.Count != 5 || BrawlSkillSequenceHelper.CanStart(cast, 1799))
                    return Fail(100907, "Absolute shot schedule drifted or cooldown ended early.");
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1800);
                BrawlAmmoHelper.Tick(owner, 1800);
                if (properties.GetFloat(PropertyId.Ammo) != 2) return Fail(100912, "Six shots must consume one ammo without reloading during the burst.");
                if (projectiles.Children.Count != 6 || !BrawlSkillSequenceHelper.CanStart(cast, 1800))
                    return Fail(100908, "Sixth shot and cooldown must finish at 800ms.");
                if (projectiles.SkillNotices.Count != 8 || projectiles.SkillNotices[7].Phase != BrawlSkillUsePhase.Completed)
                    return Fail(100914, "A six-shot burst must publish start, six fires and completion.");
                BrawlSkillSequenceHelper.Start(owner, launches, config.Id, new float2(0, 1), 0.6f, 1800, 800, skillId: skill.Id);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 1800);
                states.AddSource(StateId.Dead, -2);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 2000);
                if (projectiles.Children.Count != 7 || cast.Launches != null)
                    return Fail(100909, "Death must cancel unfired shots without deleting existing projectiles.");
                states.RemoveSource(StateId.Dead, -2);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 3000);
                if (projectiles.Children.Count != 7) return Fail(100910, "Cancelled burst resumed after state removal.");
                if (projectiles.SkillNotices.Count != 11 || projectiles.SkillNotices[10].Phase != BrawlSkillUsePhase.Interrupted ||
                    projectiles.SkillNotices[10].CastId == projectiles.SkillNotices[0].CastId)
                    return Fail(100915, "Death must interrupt only the new cast and never resume it.");
                projectiles.SkillNotices.Clear();
                BrawlProjectileLaunch pellet = launches[0];
                BrawlSkillSequenceHelper.Start(owner, new[] { pellet, pellet, pellet }, config.Id, new float2(0, 1),
                    0.6f, 3000, 0, skillId: skill.Id);
                BrawlSkillSequenceHelper.Tick(owner, level, 1, 3000);
                if (projectiles.SkillNotices.Count != 3 || projectiles.SkillNotices[0].Sustained ||
                    projectiles.SkillNotices[1].Phase != BrawlSkillUsePhase.Fired || projectiles.Children.Count != 10)
                    return Fail(100916, "Simultaneous pellets must share one fire notice without changing projectile count.");
            }
            finally { fiber.AddSingleton(previousConfig); fiber.AddSingleton(oldRules); }
            return ErrorCode.ERR_Success;
        }

        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
