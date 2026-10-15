using Unity.Mathematics;

namespace ET.Server
{
    public static class BrawlProjectileNoticeHelper
    {
        public static void Impact(BrawlProjectile projectile, BrawlProjectileContact contact, float2 center)
        {
            float2 point = center - contact.Normal * projectile.Radius;
            Add(projectile, new BrawlProjectileNotice
            {
                Kind = BrawlProjectileEventKind.Impact, TargetUnitId = contact.UnitId,
                TerrainIndex = contact.TerrainIndex, X = point.x, Z = point.y,
                NormalX = contact.Normal.x, NormalZ = contact.Normal.y,
                Blocked = contact.Blocked, Effective = contact.Effective,
            });
        }

        public static void End(BrawlProjectile projectile, BrawlProjectileEndReason reason)
        {
            if (projectile.Finished) return;
            projectile.Finished = true;
            Add(projectile, new BrawlProjectileNotice
            {
                Kind = BrawlProjectileEventKind.End, EndReason = reason, TerrainIndex = -1,
                X = projectile.Position.x, Z = projectile.Position.y,
            });
        }

        private static void Add(BrawlProjectile projectile, BrawlProjectileNotice notice)
        {
            BrawlProjectileRoomComponent room = projectile.GetParent<BrawlProjectileRoomComponent>();
            notice.EventId = ++room.EventSequence;
            notice.Tick = room.CurrentTick;
            notice.ProjectileId = projectile.Id;
            notice.ConfigId = projectile.ConfigId;
            notice.OwnerUnitId = projectile.OwnerUnitId;
            notice.Y = projectile.Height;
            room.Notices.Add(notice);
        }
    }
}
