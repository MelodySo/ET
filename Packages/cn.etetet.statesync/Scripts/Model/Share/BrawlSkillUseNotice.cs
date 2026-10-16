namespace ET
{
    public enum BrawlSkillUsePhase
    {
        Started = 1,
        Fired = 2,
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
        public bool Sustained;
    }
}
