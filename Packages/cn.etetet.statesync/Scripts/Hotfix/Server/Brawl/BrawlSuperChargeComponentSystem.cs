namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlSuperChargeComponent))]
    public static partial class BrawlSuperChargeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlSuperChargeComponent self) { self.LastUpdateAt = 0; }
    }
}
