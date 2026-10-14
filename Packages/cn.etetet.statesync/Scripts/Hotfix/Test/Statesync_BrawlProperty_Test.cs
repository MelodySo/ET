using System;
using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Statesync_BrawlProperty_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "PropertyTest");
            Unit target = scene.AddComponent<UnitComponent>().AddChild<Unit, int>(1);
            scene.AddComponent<BrawlLifecycleRoomComponent>();
            target.AddComponent<BrawlSpawnComponent>();
            target.AddComponent<BrawlDeathComponent>();
            target.AddComponent<StateComponent>();
            PropertyComponent properties = target.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new()
            {
                [PropertyId.Level] = 2, [PropertyId.BaseMaxHealth] = 1000, [PropertyId.Health] = 1000,
                [PropertyId.BaseMoveSpeed] = 300, [PropertyId.MoveSpeedBonus] = 50,
            });
            double[] contributions = { -0.5, 0.2, 0.15, -0.4 };
            for (int i = 0; i < contributions.Length; ++i)
                properties.AddModifier(new PropertyModifier { Id = 10 + i * 10, SourceInstanceId = 100 + i, Property = PropertyId.MoveSpeedBonusRatio, Value = contributions[i] });
            if (Math.Abs(properties.GetFloat(PropertyId.MoveSpeed) - 157.5f) > 0.001f)
                return Fail(100601, "Additive speed ratios produced an incorrect final value.");
            properties.RemoveModifier(20);
            if (Math.Abs(properties.GetFloat(PropertyId.MoveSpeed) - 87.5f) > 0.001f)
                return Fail(100602, "Removing a middle modifier corrupted remaining contributions.");
            properties.RemoveSource(100);
            if (Math.Abs(properties.GetFloat(PropertyId.MoveSpeed) - 262.5f) > 0.001f)
                return Fail(100603, "Source removal did not recalculate speed.");
            try { properties.Set((PropertyId)900, 1); return Fail(100604, "Retired stun property was accepted."); }
            catch (ArgumentException) { }
            try { properties.Set((PropertyId)910, 1); return Fail(100605, "Retired airborne property was accepted."); }
            catch (ArgumentException) { }
            try { properties.Set(PropertyId.Level, 1.5); return Fail(100606, "Fractional level was accepted."); }
            catch (ArgumentException) { }
            try { properties.Set(PropertyId.MoveSpeed, 10); return Fail(100607, "Derived property was writable."); }
            catch (ArgumentException) { }
            if (properties.GetInt(PropertyId.Level) != 2 || properties.GetFloat(PropertyId.Shield) != 0)
                return Fail(100608, "Validation rollback or default value is incorrect.");
            BrawlProjectile shot = scene.AddComponent<BrawlProjectileRoomComponent>().AddChild<BrawlProjectile>();
            shot.HitEvent1 = BrawlHitEventType.Damage;
            shot.EffectValue = BrawlEffectValueHelper.Calculate(300, 30, properties.GetInt(PropertyId.Level));
            properties.Set(PropertyId.DamageReductionRatio, 0.2);
            BrawlProjectileHitHelper.Apply(shot, target);
            if (shot.EffectValue != 330 || Math.Abs(properties.GetFloat(PropertyId.Health) - 736) > 0.001f)
                return Fail(100609, "Damage reduction was applied to remaining health instead of damage.");
            properties.Set(PropertyId.MaxHealthBonusRatio, 0.5);
            if (properties.GetFloat(PropertyId.MaxHealth) != 1500 || properties.GetFloat(PropertyId.Health) != 736)
                return Fail(100610, "Maximum-health recalculation reset current health.");
            properties.Set(PropertyId.Level, 5);
            if (shot.EffectValue != 330) return Fail(100611, "Existing projectile changed after owner level changed.");
            properties.Set(PropertyId.DamageReductionRatio, 1);
            BrawlProjectileHitHelper.Apply(shot, target);
            if (properties.GetFloat(PropertyId.Health) != 736) return Fail(100612, "Full reduction must prevent health loss.");
            properties.Set(PropertyId.DamageReductionRatio, 0);
            shot.EffectValue = 2000;
            BrawlProjectileHitHelper.Apply(shot, target);
            if (properties.GetFloat(PropertyId.Health) != 0 || target.IsDisposed || !target.GetComponent<StateComponent>().Has(StateId.Dead))
                return Fail(100613, "Lethal damage must mark death and preserve the entity.");
            return ErrorCode.ERR_Success;
        }

        private static int Fail(int code, string message)
        {
            Log.Console(message);
            return code;
        }
    }
}
