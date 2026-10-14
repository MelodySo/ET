namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlSpawnComponent))]
    public static partial class BrawlSpawnComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlSpawnComponent self)
        {
            self.Active = false; self.CompleteAt = 0; self.ProtectionDuration = 0;
            self.OriginalPosition = default; self.ParticipantSlot = -1;
        }
    }
}
