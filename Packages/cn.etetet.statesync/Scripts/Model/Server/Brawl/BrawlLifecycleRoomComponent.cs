using System.Collections.Generic;
namespace ET.Server
{
    [ComponentOf(typeof(Scene))]
    public sealed class BrawlLifecycleRoomComponent : Entity, IAwake
    {
        public long SimulationTime;
        public bool RosterDirty;
        public long[] RespawnPointIds;
        public readonly Dictionary<int, BrawlParticipant> Participants = new();
        public readonly List<BrawlNPCRespawn> NPCRespawns = new();
        public readonly List<BrawlLifeNotice> Notices = new();
    }
}
