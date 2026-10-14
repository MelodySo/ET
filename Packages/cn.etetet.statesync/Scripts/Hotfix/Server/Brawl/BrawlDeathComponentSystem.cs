namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlDeathComponent))]
    public static partial class BrawlDeathComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlDeathComponent self)
        {
            self.Active = false; self.StartedAt = 0; self.DestroyAt = 0; self.Duration = 0; self.RespawnDelay = 0;
        }
    }
}
