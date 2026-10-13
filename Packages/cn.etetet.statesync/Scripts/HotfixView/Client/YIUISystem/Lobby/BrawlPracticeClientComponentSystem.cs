namespace ET.Client
{
    [EntitySystemOf(typeof(BrawlPracticeClientComponent))]
    public static partial class BrawlPracticeClientComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlPracticeClientComponent self, BrawlTrainingSession session)
        {
            self.Session = session;
            session.LastRenderTime = UnityEngine.Time.unscaledTime;
        }

        [EntitySystem]
        private static void Update(this BrawlPracticeClientComponent self)
        {
            BrawlPracticeMovementViewHelper.Update(self.Session);
        }

        [EntitySystem]
        private static void Destroy(this BrawlPracticeClientComponent self)
        {
            BrawlPracticeMovementViewHelper.Stop(self.Session);
            self.Session = null;
        }
    }
}
