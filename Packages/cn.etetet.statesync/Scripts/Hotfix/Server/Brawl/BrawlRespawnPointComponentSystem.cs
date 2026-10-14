namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlRespawnPointComponent))]
    public static partial class BrawlRespawnPointComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlRespawnPointComponent self, int configId)
        { self.ConfigId = configId; self.ParticipantSlot = -1; }
    }
}
