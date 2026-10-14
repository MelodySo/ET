namespace ET
{
    // 同Tick也保留每次阶段事件，避免死亡时间为0时只同步到删除而丢失死亡通知。
    public struct BrawlLifeNotice
    {
        public long UnitId;
        public BrawlLifePhase Phase;
        public long At;
        public long EndsAt;
    }
}
