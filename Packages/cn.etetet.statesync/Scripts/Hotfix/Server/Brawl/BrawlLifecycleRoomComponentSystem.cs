namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlLifecycleRoomComponent))]
    public static partial class BrawlLifecycleRoomComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlLifecycleRoomComponent self)
        {
            self.SimulationTime = 0; self.RosterDirty = false; self.RespawnPointIds = System.Array.Empty<long>();
            self.Participants.Clear(); self.NPCRespawns.Clear(); self.Notices.Clear();
        }
    }
}
