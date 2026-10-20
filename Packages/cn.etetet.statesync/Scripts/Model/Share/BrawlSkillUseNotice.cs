namespace ET
{
    public enum BrawlSkillUsePhase
    {
        Started = 1,
        StepExecuted = 2,
        Completed = 3,
        Interrupted = 4,
    }

    /// <summary>成功施法的表现快照；不持有池化协议消息。</summary>
    [EnableClass]
    public sealed class BrawlSkillUseNotice
    {
        public long EventId;
        public long CastId;
        public long UnitId;
        public int SkillId;
        public BrawlSkillSlot Slot;
        public long Tick;
        public BrawlSkillUsePhase Phase;
        public float DirectionX;
        public float DirectionZ;
        public bool LockFacing;
        public bool FaceDirection;
        public bool ImmediatePresentation;
        public int StepId;
        public int PresentationId;
        public long DurationMilliseconds;
        public float Radius;
        public float PositionX;
        public float PositionY;
        public float PositionZ;
    }
}
