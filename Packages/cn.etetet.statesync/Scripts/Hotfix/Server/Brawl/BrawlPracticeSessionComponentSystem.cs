namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlPracticeSessionComponent))]
    public static partial class BrawlPracticeSessionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlPracticeSessionComponent self)
        {
            self.Room = default;
            self.LastUpdateTime = self.GetSingleton<TimeInfo>().ClientNow();
            self.AccumulatorMilliseconds = 0;
            self.Tick = 0;
            self.LastInputTime = 0;
            self.LastInputSequence = 0;
            self.InputWindowTime = 0;
            self.InputWindowCount = 0;
            self.InputRateWindowTime = 0;
            self.InputRateCount = 0;
        }

        [EntitySystem]
        private static void Update(this BrawlPracticeSessionComponent self)
        {
            BrawlPracticeMovementHelper.Update(self);
        }
    }
}
