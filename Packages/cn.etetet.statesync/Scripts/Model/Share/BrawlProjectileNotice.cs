namespace ET
{
    public enum BrawlProjectileEventKind { Impact = 1, End = 2 }
    public enum BrawlProjectileEndReason { None = 0, Blocked = 1, HitLimit = 2, Range = 3, Lifetime = 4 }

    /// <summary>独立值快照，不持有单位/弹丸或池化协议引用。Impact位置是表面接触点，End位置是弹丸中心。</summary>
    public struct BrawlProjectileNotice
    {
        public long EventId;
        public long Tick;
        public long ProjectileId;
        public int ConfigId;
        public long OwnerUnitId;
        public long TargetUnitId;
        public int TerrainIndex;
        public BrawlProjectileEventKind Kind;
        public BrawlProjectileEndReason EndReason;
        public float X;
        public float Y;
        public float Z;
        public float NormalX;
        public float NormalZ;
        public bool Blocked;
        public bool Effective;
    }
}
