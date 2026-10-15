namespace ET.Client
{
    [EnableClass]
    public sealed class BrawlProjectilePresentation : IBrawlProjectilePresentation
    {
        public void OnProjectileEvent(BrawlTrainingSession session, BrawlProjectileNotice notice) => BrawlProjectileVisualHelper.Present(session, notice);
        public void OnGrassCleared(BrawlTrainingSession session, int gridIndex) { } // 草表现不属于本轮弹丸资源接入范围。
    }
}
