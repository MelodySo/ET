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

