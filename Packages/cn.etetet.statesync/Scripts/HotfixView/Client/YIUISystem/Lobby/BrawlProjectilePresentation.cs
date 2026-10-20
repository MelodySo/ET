namespace ET.Client
{
    [EnableClass]
    public sealed class BrawlProjectilePresentation : IBrawlProjectilePresentation
    {
        public void OnProjectileEvent(BrawlTrainingSession session, BrawlProjectileNotice notice) => BrawlProjectileVisualHelper.Present(session, notice);
        public void OnGrassCleared(BrawlTrainingSession session, int gridIndex) => BrawlTerrainRenderHelper.ClearGrass(session, gridIndex);
    }
}
