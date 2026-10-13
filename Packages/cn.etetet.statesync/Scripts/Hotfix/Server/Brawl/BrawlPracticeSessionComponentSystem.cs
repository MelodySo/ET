namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlPracticeSessionComponent))]
    public static partial class BrawlPracticeSessionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlPracticeSessionComponent self) { }
    }
}
