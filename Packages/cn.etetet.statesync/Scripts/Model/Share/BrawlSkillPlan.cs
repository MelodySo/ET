namespace ET
{
    /// <summary>一次施法的固定时间步骤；由发射器代码生成，每次施法独立持有。</summary>
    [EnableClass]
    public sealed class BrawlSkillPlan
    {
        public BrawlSkillStep[] Steps;
        public bool FaceDirection;
        public bool LockFacing;
        public bool ImmediatePresentation;
    }
}
