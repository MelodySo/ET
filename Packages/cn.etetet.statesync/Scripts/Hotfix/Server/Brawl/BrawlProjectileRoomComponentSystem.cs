namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlProjectileRoomComponent))]
    public static partial class BrawlProjectileRoomComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlProjectileRoomComponent self)
        {
            self.SnapshotSequence = 0;
            self.PendingRemoval.Clear();
        }
    }
}
