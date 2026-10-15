namespace ET.Client
{
    [EntitySystemOf(typeof(BrawlAIPlayGuard))]
    public static partial class BrawlAIPlayGuardSystem
    {
        [EntitySystem] private static void Awake(this BrawlAIPlayGuard self) { }
    }
}