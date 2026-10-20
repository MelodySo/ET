namespace ET
{
    /// <summary>相对施法起点的步骤。同一步可以只有表现，也可以执行多个效果。</summary>
    public struct BrawlSkillStep
    {
        public int Id;
        public long DelayMilliseconds;
        public int PresentationId;
        public long PresentationDurationMilliseconds;
        public float PresentationRadius;
        public BrawlSkillEffect[] Effects;
    }
}
