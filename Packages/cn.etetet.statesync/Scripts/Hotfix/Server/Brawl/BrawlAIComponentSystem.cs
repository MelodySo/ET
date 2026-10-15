namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlAIComponent))]
    public static partial class BrawlAIComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlAIComponent self)
        {
            self.Path.Clear(); self.Waypoint = 0; self.TargetId = 0; self.PlannedTargetId = 0;
            self.Now = 0; self.NextPlanAt = 0; self.LevelId = 0; self.CellSize = 0; self.Chasing = false; self.Plans = 0;
        }
        [EntitySystem]
        private static void Destroy(this BrawlAIComponent self) { self.Path.Clear(); }
    }
}