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
            BrawlUnitRosterViewHelper.Update(self.Session);
            BrawlSkillUseViewHelper.Update(self.Session);
            BrawlSkillJoystickHelper.Update(self.Session);
            BrawlSkillViewHelper.UpdateAmmo(self.Session);
            BrawlProjectileViewHelper.Update(self.Session);
        }

        [EntitySystem]
        private static void Destroy(this BrawlPracticeClientComponent self)
        {
            BrawlPracticeMovementViewHelper.Stop(self.Session);
            BrawlSkillJoystickHelper.CancelAll(self.Session);
            BrawlProjectileViewHelper.Clear(self.Session);
            BrawlSkillUseViewHelper.Clear(self.Session);
            BrawlAIPlayVerification.Abort(self.Session);
            self.Session = null;
        }
    }
}
