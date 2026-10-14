using System;
namespace ET.Server
{
    public static class BrawlUnitLifeHelper
    {
        private const long SpawnProtectionSource = -1;
        private const long DeathSource = -2;
        private const long SpawningSource = -3;

        public static long Milliseconds(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0 || seconds > int.MaxValue / 1000f)
                throw new ArgumentException("Lifecycle duration must be finite and non-negative.");
            return seconds == 0 ? 0 : Math.Max(1, (long)Math.Ceiling(seconds * 1000d - 0.0001d));
        }

        public static void OnSpawn(Unit unit, long now)
        {
            BrawlSpawnComponent spawn = unit.GetComponent<BrawlSpawnComponent>();
            if (spawn.Active)
            {
                unit.GetComponent<StateComponent>().AddSource(StateId.Spawning, SpawningSource);
                Notify(unit, BrawlLifePhase.SpawnStarted, now, spawn.CompleteAt);
                if (spawn.CompleteAt <= now) CompleteSpawn(unit, now);
            }
            else Notify(unit, BrawlLifePhase.SpawnCompleted, now, now);
        }

        public static void CompleteSpawn(Unit unit, long now)
        {
            BrawlSpawnComponent spawn = unit.GetComponent<BrawlSpawnComponent>();
            if (!spawn.Active || spawn.CompleteAt > now) return;
            spawn.Active = false;
            StateComponent states = unit.GetComponent<StateComponent>();
            states.RemoveSource(StateId.Spawning, SpawningSource);
            if (spawn.ProtectionDuration > 0)
                states.AddSource(StateId.Invincible, SpawnProtectionSource, checked(now + spawn.ProtectionDuration));
            Notify(unit, BrawlLifePhase.SpawnCompleted, now, now);
        }

        public static void CheckDeath(Unit unit)
        {
            if (unit.GetComponent<PropertyComponent>().GetFloat(PropertyId.Health) > 0) return;
            BrawlDeathComponent death = unit.GetComponent<BrawlDeathComponent>();
            if (death.Active) return;
            long now = unit.Scene().GetComponent<BrawlLifecycleRoomComponent>().SimulationTime;
            death.Active = true;
            death.StartedAt = now;
            death.DestroyAt = checked(now + death.Duration);
            unit.GetComponent<BrawlSpawnComponent>().Active = false;
            StateComponent states = unit.GetComponent<StateComponent>();
            states.AddSource(StateId.Dead, DeathSource);
            BrawlSuperChargeHelper.SaveOnDeath(unit);
            states.RemoveSource(StateId.Invincible, SpawnProtectionSource);
            states.RemoveSource(StateId.Spawning, SpawningSource);
            BrawlMoveComponent move = unit.GetComponent<BrawlMoveComponent>();
            if (move != null) { move.DesiredDirection = default; move.Velocity = default; }
            Notify(unit, BrawlLifePhase.Died, now, death.DestroyAt);
        }

        public static bool CannotAct(Unit unit)
        {
            StateComponent states = unit.GetComponent<StateComponent>();
            return states.Has(StateId.Dead) || states.Has(StateId.Spawning) || states.Has(StateId.Stunned) || states.Has(StateId.Airborne);
        }

        public static void Notify(Unit unit, BrawlLifePhase phase, long at, long endsAt)
        {
            unit.Scene().GetComponent<BrawlLifecycleRoomComponent>().Notices.Add(new BrawlLifeNotice
                { UnitId = unit.Id, Phase = phase, At = at, EndsAt = endsAt });
        }
    }
}
