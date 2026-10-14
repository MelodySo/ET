namespace ET.Server
{
    public static class BrawlProjectileHelper
    {
        public static void Tick(BrawlPracticeSessionComponent state)
        {
            Scene room = state.Room;
            BrawlProjectileRoomComponent projectiles = room.GetComponent<BrawlProjectileRoomComponent>();
            UnitComponent units = room.GetComponent<UnitComponent>();
            BrawlLevelConfig level = state.Fiber().GetSingleton<BrawlLevelConfigCategory>().Get(state.LevelId);
            foreach (Entity child in projectiles.Children.Values)
            {
                BrawlProjectile projectile = (BrawlProjectile)child;
                // 终点保留一帧快照，客户端先插值抵达终点，再看到删除。
                if (projectile.Finished) projectiles.PendingRemoval.Add(projectile.Id);
                else BrawlProjectileCollisionHelper.Advance(projectile, units, level, state.CellSize, BrawlMovementSettings.TickSeconds);
            }
            foreach (long id in projectiles.PendingRemoval) projectiles.GetChild<BrawlProjectile>(id).Dispose();
            projectiles.PendingRemoval.Clear();
            long now = room.GetComponent<BrawlLifecycleRoomComponent>().SimulationTime;
            foreach (Entity child in units.Children.Values)
            {
                Unit unit = (Unit)child;
                // NPC没有英雄施法组件；英雄死亡/受控时由调度器取消后续连发。
                if (unit.GetComponent<BrawlSkillCastComponent>() != null)
                {
                    BrawlSkillSequenceHelper.Tick(unit, level, state.CellSize, now);
                    BrawlAmmoHelper.Tick(unit, now);
                    BrawlSuperChargeHelper.Tick(unit, now);
                }
            }
            SendSnapshot(state);
        }

        public static void SendSnapshot(BrawlPracticeSessionComponent state)
        {
            Scene room = state.Room;
            BrawlProjectileRoomComponent projectiles = room.GetComponent<BrawlProjectileRoomComponent>();
            using G2C_BrawlProjectileSnapshot snapshot = G2C_BrawlProjectileSnapshot.Create();
            snapshot.EntryId = state.EntryId;
            snapshot.RoomId = room.Id;
            snapshot.Sequence = ++projectiles.SnapshotSequence;
            snapshot.Tick = state.Tick;
            foreach (Entity child in projectiles.Children.Values)
            {
                BrawlProjectile projectile = (BrawlProjectile)child;
                BrawlProjectileState info = BrawlProjectileState.Create();
                info.ProjectileId = projectile.Id;
                info.ConfigId = projectile.ConfigId;
                info.OwnerUnitId = projectile.OwnerUnitId;
                info.Faction = (int)projectile.Faction;
                info.X = projectile.Position.x;
                info.Y = projectile.Height;
                info.Z = projectile.Position.y;
                info.Radius = projectile.Radius;
                snapshot.Projectiles.Add(info);
            }
            state.GetParent<Session>().Send(snapshot);
        }
    }
}

