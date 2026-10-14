namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlAmmoComponent))]
    public static partial class BrawlAmmoComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlAmmoComponent self) { self.LastUpdateAt = 0; }
    }
}
