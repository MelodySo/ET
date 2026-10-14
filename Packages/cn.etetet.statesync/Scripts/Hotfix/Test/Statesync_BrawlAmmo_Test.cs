using System;
using System.Collections.Generic;
using ET.Server;

namespace ET.Test
{
    public class Statesync_BrawlAmmo_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "AmmoTest");
            Unit unit = scene.AddComponent<UnitComponent>().AddChild<Unit, int>(1);
            PropertyComponent properties = unit.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new());
            unit.AddComponent<BrawlAmmoComponent>();
            BrawlSkillCastComponent cast = unit.AddComponent<BrawlSkillCastComponent>();
            BrawlSpawnComponent spawn = unit.AddComponent<BrawlSpawnComponent>();
            StateComponent states = unit.AddComponent<StateComponent>();
            BrawlAmmoHelper.Initialize(unit, Skill(3, 1.5f), false, 0);
            if (properties.GetInt(PropertyId.MaxAmmo) != 3 || Ammo(unit) != 3) return Fail(101001, "Initial ammo must be full.");
            for (int i = 0; i < 3; ++i)
                if (!BrawlAmmoHelper.TryConsume(unit, 0)) return Fail(101002, "Each attack must consume one full ammo.");
            if (Ammo(unit) != 0 || BrawlAmmoHelper.TryConsume(unit, 0)) return Fail(101003, "Empty ammo allowed an extra attack.");
            BrawlAmmoHelper.Tick(unit, 750);
            if (!Near(Ammo(unit), 0.5) || BrawlAmmoHelper.TryConsume(unit, 750)) return Fail(101004, "Partial ammo cannot fire.");
            BrawlAmmoHelper.Tick(unit, 1500);
            if (Ammo(unit) != 1) return Fail(101005, "Exactly one slot must reload in 1.5 seconds.");
            BrawlAmmoHelper.Tick(unit, 2250);
            if (!BrawlAmmoHelper.TryConsume(unit, 2250) || !Near(Ammo(unit), 0.5)) return Fail(101006, "Firing must preserve fractional progress and consume exactly one ammo.");
            BrawlAmmoHelper.Tick(unit, 3750);
            if (!Near(Ammo(unit), 1.5)) return Fail(101007, "Reload did not continue from retained fractional progress.");
            BrawlAmmoHelper.Tick(unit, 9750);
            if (Ammo(unit) != 3) return Fail(101008, "Reload exceeded capacity.");

            BrawlAmmoHelper.Initialize(unit, Skill(3, 1.6f), false, 10000);
            BrawlAmmoHelper.TryConsume(unit, 10000);
            cast.Launches = new BrawlProjectileLaunch[6];
            BrawlAmmoHelper.Tick(unit, 10800);
            if (Ammo(unit) != 2) return Fail(101009, "Ammo must not reload during a burst.");
            cast.Launches = null;
            BrawlAmmoHelper.ResetClock(unit, 10800);
            for (long now = 10850; now <= 12400; now += 50) BrawlAmmoHelper.Tick(unit, now);
            if (Ammo(unit) != 3) return Fail(101010, "Colt must finish a reload exactly 1.6 seconds after its last shot.");

            BrawlAmmoHelper.Initialize(unit, Skill(3, 1.5f), true, 13000);
            spawn.CompleteAt = 15000;
            states.AddSource(StateId.Spawning, -3);
            BrawlAmmoHelper.Tick(unit, 14950);
            if (Ammo(unit) != 1) return Fail(101011, "Respawn ammo must be one third and cannot reload while spawning.");
            states.RemoveSource(StateId.Spawning, -3);
            BrawlAmmoHelper.Tick(unit, 15000);
            if (Ammo(unit) != 1) return Fail(101012, "Birth completion tick included time spent spawning.");
            BrawlAmmoHelper.Tick(unit, 16500);
            if (Ammo(unit) != 2) return Fail(101013, "Reload did not resume after birth.");
            states.AddSource(StateId.Dead, -2);
            BrawlAmmoHelper.Tick(unit, 18000);
            if (Ammo(unit) != 2) return Fail(101014, "Dead unit reloaded ammo.");
            states.RemoveSource(StateId.Dead, -2);
            spawn.CompleteAt = 0;
            BrawlAmmoHelper.Initialize(unit, Skill(1, 1.5f), true, 18000);
            if (!Near(Ammo(unit), 1d / 3) || BrawlAmmoHelper.TryConsume(unit, 18000)) return Fail(101015, "Capacity one must retain fractional respawn ammo.");
            properties.AddModifier(new PropertyModifier { Id = 1, SourceInstanceId = 1, Property = PropertyId.ReloadRateBonusRatio, Value = 1 });
            BrawlAmmoHelper.Tick(unit, 18500);
            if (Ammo(unit) != 1) return Fail(101016, "Reload rate modifier did not apply to continuous progress.");
            properties.RemoveModifier(1);
            BrawlAmmoHelper.Initialize(unit, Skill(3, 1.6f), false, 19000);
            properties.Set(PropertyId.Ammo, 2.4);
            if (!BrawlAmmoHelper.TryConsume(unit, 19000) || !Near(Ammo(unit), 1.4))
                return Fail(101017, "Colt attack incorrectly rounded 2.4 ammo down instead of keeping 1.4.");
            cast.Launches = new BrawlProjectileLaunch[6];
            BrawlAmmoHelper.Tick(unit, 19800);
            if (!Near(Ammo(unit), 1.4)) return Fail(101018, "Burst changed retained reload progress.");
            cast.Launches = null;
            BrawlAmmoHelper.ResetClock(unit, 19800);
            BrawlAmmoHelper.Tick(unit, 20600);
            if (!Near(Ammo(unit), 1.9)) return Fail(101019, "Reload after burst did not continue from retained 40 percent progress.");
            return ErrorCode.ERR_Success;
        }

        private static BrawlSkillConfig Skill(int capacity, float reload) => new(1, 1, 5, 1, 1, 0, 0, capacity, reload, 0, 1, 2, 7.67f, 30, 0, 0, 0);
        private static double Ammo(Unit unit) => unit.GetComponent<PropertyComponent>().GetDouble(PropertyId.Ammo);
        private static bool Near(double a, double b) => Math.Abs(a - b) < 1e-8;
        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
