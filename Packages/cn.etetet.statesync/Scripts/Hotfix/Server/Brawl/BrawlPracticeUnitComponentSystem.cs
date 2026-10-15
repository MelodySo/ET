namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlPracticeUnitComponent))]
    public static partial class BrawlPracticeUnitComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlPracticeUnitComponent self)
        {
            self.Kind = default;
            self.Faction = default;
            self.UnitLayerMask = default;
            self.BlocksMovement = false;
            self.NPCType = default;
            self.CollisionLength = 0;
            self.CollisionWidth = 0;
            self.CollisionPivot = default;
            self.PreviousPosition = default;
            self.OwnerPlayerId = 0;
        }
    }
}
