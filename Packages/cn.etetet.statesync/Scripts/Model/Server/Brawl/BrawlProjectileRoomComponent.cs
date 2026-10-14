using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public sealed class BrawlProjectileRoomComponent : Entity, IAwake
    {
        public long SnapshotSequence;
        public List<long> PendingRemoval = new();
    }
}
