using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlState_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "StateTest");
            Unit target = scene.AddComponent<UnitComponent>().AddChild<Unit, int>(1);
            BrawlLifecycleRoomComponent life = scene.AddComponent<BrawlLifecycleRoomComponent>();
            BrawlSpawnComponent spawn = target.AddComponent<BrawlSpawnComponent>();
            BrawlDeathComponent death = target.AddComponent<BrawlDeathComponent>();
            death.Duration = 3000;
            StateComponent states = target.AddComponent<StateComponent>();
            PropertyComponent properties = target.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new()
            {
                [PropertyId.BaseMaxHealth] = 1000, [PropertyId.Health] = 1000,
            });
            BrawlMoveComponent move = target.AddComponent<BrawlMoveComponent>();
            BrawlProjectile shot = scene.AddComponent<BrawlProjectileRoomComponent>().AddChild<BrawlProjectile>();
            shot.HitEvent1 = BrawlHitEventType.Damage;
            shot.EffectValue = 300;
            if (states.Has(StateId.Dead) || states.Has(StateId.Invincible) || states.PendingChanges.Count != 0)
                return Fail(100701, "States must default to false without pending changes.");
            states.AddSource(StateId.Stunned, 101);
            states.PendingChanges.Clear();
            states.AddSource(StateId.Stunned, 202);
            states.RemoveSource(StateId.Stunned, 101);
            if (!states.Has(StateId.Stunned) || states.PendingChanges.Count != 0 || !BrawlUnitLifeHelper.CannotAct(target))
                return Fail(100702, "Removing one stun source must not clear or resynchronize the final state.");
            states.RemoveSource(202);
            if (states.Has(StateId.Stunned) || states.PendingChanges[StateId.Stunned])
                return Fail(100703, "Last source must emit false.");
            states.AddSource(StateId.Airborne, 303, 1000);
            states.AddSource(StateId.Invincible, 303, 1000);
            states.Expire(999);
            if (!states.Has(StateId.Airborne) || !BrawlUnitLifeHelper.CannotAct(target))
                return Fail(100704, "Timed state expired before its boundary.");
            if (states.RemoveSource(303) != 2 || states.Has(StateId.Airborne) || states.Has(StateId.Invincible))
                return Fail(100705, "Removing one source must remove all its state contributions.");

            spawn.Active = true; spawn.CompleteAt = 3000; spawn.ProtectionDuration = 3000;
            BrawlUnitLifeHelper.OnSpawn(target, 1000);
            if (!states.Has(StateId.Spawning) || states.Has(StateId.Invincible) || !BrawlUnitLifeHelper.CannotAct(target))
                return Fail(100716, "Hero birth must precede invincibility.");
            BrawlProjectileHitHelper.Apply(shot, target);
            BrawlUnitLifeHelper.CompleteSpawn(target, 2999);
            if (!spawn.Active || properties.GetFloat(PropertyId.Health) != 1000)
                return Fail(100717, "Spawning hero took damage or completed early.");
            BrawlUnitLifeHelper.CompleteSpawn(target, 3000);
            BrawlProjectileHitHelper.Apply(shot, target);
            states.Expire(5999);
            if (properties.GetFloat(PropertyId.Health) != 1000 || !states.Has(StateId.Invincible))
                return Fail(100706, "Spawn protection did not last three seconds or failed to prevent damage.");
            states.Expire(6000);
            BrawlProjectileHitHelper.Apply(shot, target);
            if (states.Has(StateId.Invincible) || properties.GetFloat(PropertyId.Health) != 700)
                return Fail(100707, "Protection must expire at exactly three simulated seconds.");

            spawn.Active = true; spawn.CompleteAt = 7000;
            BrawlUnitLifeHelper.OnSpawn(target, 7000);
            states.AddSource(StateId.Invincible, 404, 12000);
            states.Expire(10000);
            if (!states.Has(StateId.Invincible)) return Fail(100708, "Spawn expiry removed another invincibility source.");
            states.RemoveSource(404);
            move.DesiredDirection = new float2(1, 0);
            move.Velocity = new float2(3, 0);
            shot.EffectValue = 2000;
            life.SimulationTime = 10000;
            BrawlProjectileHitHelper.Apply(shot, target);
            if (!states.Has(StateId.Dead) || properties.GetFloat(PropertyId.Health) != 0 || target.IsDisposed ||
                math.any(move.DesiredDirection != float2.zero) || math.any(move.Velocity != float2.zero) || !BrawlUnitLifeHelper.CannotAct(target))
                return Fail(100709, "Death must preserve the entity while stopping movement and attacks.");
            BrawlProjectileHitHelper.Apply(shot, target);
            if (properties.GetFloat(PropertyId.Health) != 0) return Fail(100710, "Repeated damage changed a dead target.");
            if (death.DestroyAt != 13000 || life.Notices.FindAll(n => n.Phase == BrawlLifePhase.Died).Count != 1)
                return Fail(100711, "Death deadline or single death event is incorrect.");
            Unit npc = scene.GetComponent<UnitComponent>().AddChild<Unit, int>(2);
            npc.AddComponent<StateComponent>(); npc.AddComponent<BrawlSpawnComponent>(); npc.AddComponent<BrawlDeathComponent>();
            BrawlUnitLifeHelper.OnSpawn(npc, 15000);
            if (npc.GetComponent<StateComponent>().Has(StateId.Invincible) || npc.GetComponent<StateComponent>().Has(StateId.Spawning))
                return Fail(100712, "NPC must not inherit hero spawning or protection.");

            using BrawlPracticeUnitInfo initial = BrawlPracticeUnitInfo.Create();
            states.AddSource(StateId.Stunned, 505);
            BrawlStateSyncHelper.FillInitial(states, initial.States);
            if (initial.States.Count != 5) return Fail(100714, "Initial snapshot must include each state, including false.");
            states.PendingChanges.Clear();
            states.RemoveSource(505);
            using G2C_BrawlMoveSnapshot snapshot = G2C_BrawlMoveSnapshot.Create();
            BrawlStateSyncHelper.FillChanges(target, snapshot.States);
            if (snapshot.States.Count != 1 || snapshot.States[0].UnitId != target.Id || snapshot.States[0].States.Count != 1 ||
                snapshot.States[0].States[0].StateId != (int)StateId.Stunned || snapshot.States[0].States[0].Value || states.PendingChanges.Count != 0)
                return Fail(100715, "State delta did not send the final false value or clear pending data.");
            return ErrorCode.ERR_Success;
        }

        private static int Fail(int code, string message)
        {
            Log.Console(message);
            return code;
        }
    }
}
