namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlPracticeUnitComponent))]
    public static partial class BrawlPracticeUnitComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlPracticeUnitComponent self) { }
    }
}
