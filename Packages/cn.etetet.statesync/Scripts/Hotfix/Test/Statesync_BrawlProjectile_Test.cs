using System;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlProjectile_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            BrawlProjectileConfig config = new(1, 0.05f, 1, 0, 8, 300, 30, 1, 0, 0, 0, 0, 0, 0.1f, 1);
            BrawlShotgunEmitter emitter = (BrawlShotgunEmitter)BrawlSkillEmitterHelper.Create(BrawlEmitterType.Shotgun);
            BrawlProjectileLaunch[] launches = emitter.Emit(Skill(5), config, 1);
            if (launches.Length != 5) return Fail(100301, "Shotgun count does not match configuration.");
            float[] expectedAngles = { -15, -7.5f, 0, 7.5f, 15 };
            for (int i = 0; i < launches.Length; ++i)
            {
                BrawlProjectileLaunch launch = launches[i];
                float degrees = math.degrees(math.atan2(launch.Direction.x, launch.Direction.y));
                if (math.abs(degrees - expectedAngles[i]) > 0.0001f || math.abs(math.length(launch.Direction) - 1) > 0.0001f)
                    return Fail(100302, "Shotgun directions do not span the full sector symmetrically.");
                if (launch.SuperChargeOnHit != 0.1f || launch.EffectValue != 300 || launch.Range != 7.67f || launch.Speed != 8 || launch.Radius != 0.05f || math.any(launch.Offset != float2.zero))
                    return Fail(100303, "Shotgun launch conditions do not match configuration.");
            }
            BrawlProjectileLaunch single = emitter.Emit(Skill(1), config, 1)[0];
            if (emitter.Emit(Skill(5), config, 2)[0].EffectValue != 330 || emitter.Emit(Skill(1), config, 5)[0].EffectValue != 420)
                return Fail(100312, "Emitter did not apply hero level growth to pellet values.");
            try
            {
                emitter.Emit(Skill(5), config, 0);
                return Fail(100313, "Invalid hero level was accepted.");
            }
            catch (ArgumentException) { }
            if (math.any(single.Direction != new float2(0, 1))) return Fail(100304, "A single pellet must go forward.");
            if (emitter.Emit(Skill(3), config, 1).Length != 3) return Fail(100305, "Pellet count was hardcoded.");
            if (BrawlSkillEmitterHelper.Create(BrawlEmitterType.Boomerang) != null)
                return Fail(100306, "Unimplemented emitter should not fall back to shotgun.");
            try
            {
                emitter.Emit(Skill(5, 2), config, 1);
                return Fail(100307, "Unsupported multiple volleys were accepted.");
            }
            catch (NotSupportedException) { }
            try
            {
                emitter.Emit(Skill(0), config, 1);
                return Fail(100308, "Zero pellet count was accepted.");
            }
            catch (ArgumentException) { }

            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "ProjectileTest");
            BrawlProjectileRoomComponent room = scene.AddComponent<BrawlProjectileRoomComponent>();
            BrawlProjectile projectile = room.AddChild<BrawlProjectile>();
            projectile.Direction = new float2(0, 1);
            projectile.Speed = 8;
            projectile.RemainingDistance = 7.67f;
            for (int tick = 0; tick < 19; ++tick) projectile.Advance(0.05f);
            if (math.abs(projectile.Position.y - 7.6f) > 0.0001f || projectile.Finished)
                return Fail(100309, "Fixed-step projectile speed is incorrect.");
            projectile.Advance(0.05f);
            if (math.abs(projectile.Position.y - 7.67f) > 0.0001f || !projectile.Finished || projectile.RemainingDistance != 0)
                return Fail(100310, "Projectile did not clamp to its range endpoint.");
            projectile.Advance(0.05f);
            if (math.abs(projectile.Position.y - 7.67f) > 0.0001f)
                return Fail(100311, "Finished projectile continued moving.");
            return ErrorCode.ERR_Success;
        }

        private static BrawlSkillConfig Skill(int count, int rounds = 1)
        {
            return new BrawlSkillConfig(1, 1, count, 1, rounds, 0, 0, 3, 1.5f, 0.5f, 1, 2, 7.67f, 30, 0, 0, 0);
        }

        private static int Fail(int code, string message)
        {
            Log.Console(message);
            return code;
        }
    }
}
