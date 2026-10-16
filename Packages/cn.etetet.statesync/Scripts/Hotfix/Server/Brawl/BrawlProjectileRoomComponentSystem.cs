namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlProjectileRoomComponent))]
    public static partial class BrawlProjectileRoomComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlProjectileRoomComponent self)
        {
            self.SnapshotSequence = 0;
            self.EventSequence = 0;
            self.CurrentTick = 0;
            self.Notices.Clear();
            self.SkillCastSequence = 0;
            self.SkillEventSequence = 0;
            self.SkillNotices.Clear();
            self.ClearedGrass.Clear();
            self.PendingRemoval.Clear();
        }
    }
}
