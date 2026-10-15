using System.Collections.Generic;

namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public sealed class BrawlProjectileRoomComponent : Entity, IAwake
    {
        public long SnapshotSequence;
        public long EventSequence;
        public long CurrentTick;
        public List<BrawlProjectileNotice> Notices = new();
        // 房间私有地形变更；共享关卡配置永远不修改。
        public HashSet<int> ClearedGrass = new();
        public List<long> PendingRemoval = new();
    }
}
