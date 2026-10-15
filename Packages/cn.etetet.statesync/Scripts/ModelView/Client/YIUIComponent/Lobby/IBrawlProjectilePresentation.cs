namespace ET.Client
{
    /// <summary>仅在显示时间到达事件Tick后通知。此版本不提供任何特效实现。</summary>
    public interface IBrawlProjectilePresentation
    {
        void OnProjectileEvent(BrawlTrainingSession session, BrawlProjectileNotice notice);
        void OnGrassCleared(BrawlTrainingSession session, int gridIndex);
    }
}
