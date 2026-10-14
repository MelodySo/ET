using System;
using Unity.Mathematics;

namespace ET.Server
{
    public static class BrawlPracticeMovementHelper
    {
        public static bool CheckInputRate(BrawlPracticeSessionComponent state)
        {
            long now = state.GetSingleton<TimeInfo>().ClientNow();
            if (now - state.InputRateWindowTime >= 1000)
            {
                state.InputRateWindowTime = now;
                state.InputRateCount = 0;
            }
            return ++state.InputRateCount <= BrawlMovementSettings.InputMessagesPerSecond;
        }

        public static void SetInput(Session session, C2G_BrawlMoveInput message)
        {
            BrawlPracticeSessionComponent state = session.GetComponent<BrawlPracticeSessionComponent>();
            if (state == null || state.EntryId != message.EntryId || message.Sequence <= state.LastInputSequence) return;
            float2 direction = new float2(message.X, message.Z);
            if (!math.all(math.isfinite(direction)) || math.any(math.abs(direction) > 1)) return;
            float2 facing = new float2(message.FacingX, message.FacingZ);
            if (!math.all(math.isfinite(facing)) || math.any(math.abs(facing) > 1)) return;
            facing = math.normalizesafe(facing);
            if (math.lengthsq(facing) == 0) return;
            long now = state.GetSingleton<TimeInfo>().ClientNow();
            if (now - state.InputWindowTime >= BrawlMovementSettings.TickMilliseconds)
            {
                state.InputWindowTime = now;
                state.InputWindowCount = 0;
            }
            // 允许起步/停止的即时输入，但非零方向不能无限刷包；停止总能覆盖旧方向。
            if (math.lengthsq(direction) > 0 && state.InputWindowCount >= BrawlMovementSettings.MaxInputsPerTick) return;
            ++state.InputWindowCount;
            Scene room = state.Room;
            Unit unit = room.GetComponent<UnitComponent>().Get(state.MyUnitId);
            BrawlMoveComponent move = unit.GetComponent<BrawlMoveComponent>();
            // 输入只表达方向和停止；死区由客户端处理，服务端统一按全速意图移动。
            move.DesiredDirection = BrawlUnitLifeHelper.CannotAct(unit) ? default : math.normalizesafe(direction);
            move.FacingDirection = facing;
            state.LastInputTime = now;
            state.LastInputSequence = message.Sequence;
        }

        public static void Update(BrawlPracticeSessionComponent state)
        {
            Scene room = state.Room;
            if (room == null) return;
            long now = state.GetSingleton<TimeInfo>().ClientNow();
            long elapsed = Math.Max(0, now - state.LastUpdateTime);
            state.LastUpdateTime = now;
            // 卡顿时最多追赶五步，不拿整个卡顿时长做一次大位移。
            state.AccumulatorMilliseconds += Math.Min(elapsed, BrawlMovementSettings.TickMilliseconds * BrawlMovementSettings.MaxCatchUpTicks);
            if (state.AccumulatorMilliseconds < BrawlMovementSettings.TickMilliseconds) return;
            BrawlLevelConfig level = state.Fiber().GetSingleton<BrawlLevelConfigCategory>().Get(state.LevelId);
            UnitComponent units = room.GetComponent<UnitComponent>();
            while (state.AccumulatorMilliseconds >= BrawlMovementSettings.TickMilliseconds)
            {
                state.AccumulatorMilliseconds -= BrawlMovementSettings.TickMilliseconds;
                ++state.Tick;
                room.GetComponent<BrawlLifecycleRoomComponent>().SimulationTime = state.Tick * BrawlMovementSettings.TickMilliseconds;
                state.MyUnitId = BrawlLifecycleHelper.Tick(room);
                foreach (Entity child in units.Children.Values)
                {
                    Unit unit = (Unit)child;
                    unit.GetComponent<StateComponent>().Expire(state.Tick * BrawlMovementSettings.TickMilliseconds);
                    BrawlUnitLifeHelper.CheckDeath(unit);
                    unit.GetComponent<BrawlPracticeUnitComponent>().PreviousPosition = unit.Position.xz;
                    // NPC 本轮没有移动组件，但挂载阻挡组件的 NPC 参与英雄移动限位。
                    BrawlMoveComponent move = unit.GetComponent<BrawlMoveComponent>();
                    if (move == null) continue;
                    PropertyComponent properties = unit.GetComponent<PropertyComponent>();
                    move.Speed = properties.GetFloat(PropertyId.MoveSpeed);
                    bool controlled = BrawlUnitLifeHelper.CannotAct(unit);
                    if (unit.Id == state.MyUnitId && now - state.LastInputTime > BrawlMovementSettings.InputTimeoutMilliseconds)
                        move.DesiredDirection = default;
                    float2 previous = unit.Position.xz;
                    float2 next = BrawlUnitMovementHelper.Move(units, level, state.CellSize, unit.Id, previous,
                        (controlled ? float2.zero : move.DesiredDirection) * move.Speed * BrawlMovementSettings.TickSeconds, move.Radius);
                    move.Velocity = (next - previous) / BrawlMovementSettings.TickSeconds;
                    if (math.any(next != previous)) unit.Position = new float3(next.x, unit.Position.y, next.y);
                }
                BrawlProjectileHelper.Tick(state);
                state.MyUnitId = BrawlLifecycleHelper.Tick(room);
            }
            using G2C_BrawlMoveSnapshot snapshot = G2C_BrawlMoveSnapshot.Create();
            snapshot.EntryId = state.EntryId;
            snapshot.RoomId = room.Id;
            snapshot.Tick = state.Tick;
            snapshot.LastInputSequence = state.LastInputSequence;
            snapshot.MyUnitId = state.MyUnitId;
            BrawlUnitSnapshotHelper.FillRoster(room, snapshot);
            foreach (Entity child in units.Children.Values)
            {
                Unit unit = (Unit)child;
                PropertyComponent properties = unit.GetComponent<PropertyComponent>();
                BrawlPropertyState propertyInfo = BrawlPropertyState.Create();
                propertyInfo.UnitId = unit.Id;
                propertyInfo.Level = properties.GetInt(PropertyId.Level);
                propertyInfo.Health = properties.GetFloat(PropertyId.Health);
                propertyInfo.MaxHealth = properties.GetFloat(PropertyId.MaxHealth);
                propertyInfo.MaxAmmo = properties.GetInt(PropertyId.MaxAmmo);
                propertyInfo.Ammo = properties.GetFloat(PropertyId.Ammo);
                propertyInfo.ReloadRate = properties.GetFloat(PropertyId.ReloadRate);
                propertyInfo.MaxSuperAmmo = properties.GetInt(PropertyId.MaxSuperAmmo);
                propertyInfo.SuperAmmo = properties.GetFloat(PropertyId.SuperAmmo);
                propertyInfo.SuperChargeRegenRate = properties.GetFloat(PropertyId.SuperChargeRegenRate);
                snapshot.Properties.Add(propertyInfo);
                BrawlStateSyncHelper.FillChanges(unit, snapshot.States);
                BrawlMoveComponent move = unit.GetComponent<BrawlMoveComponent>();
                if (move == null) continue;
                BrawlMoveState info = BrawlMoveState.Create();
                info.UnitId = unit.Id;
                info.X = unit.Position.x;
                info.Z = unit.Position.z;
                info.MoveX = move.DesiredDirection.x;
                info.MoveZ = move.DesiredDirection.y;
                info.FacingX = move.FacingDirection.x;
                info.FacingZ = move.FacingDirection.y;
                snapshot.Units.Add(info);
            }
            // 练习房间当前只有本连接一名真实玩家；以后多人房间改为广播给成员。
            state.GetParent<Session>().Send(snapshot);
        }
    }
}
