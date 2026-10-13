namespace ET
{
    /// <summary>首版移动约定。世界格子尺寸仍读取 BrawlGlobal，英雄速度暂用 MoveSpeedTier 米/秒。</summary>
    public static class BrawlMovementSettings
    {
        public const int TickMilliseconds = 50;
        public const float TickSeconds = TickMilliseconds / 1000f;
        public const int InputTimeoutMilliseconds = 500;
        public const int MaxCatchUpTicks = 5;
        public const int MaxInputsPerTick = 4;
        public const int InputMessagesPerSecond = 60;
        public const int InterpolationDelayTicks = 2;
        public const int MaxSnapshotHistory = 32;
        public const float ContactTolerance = 0.00001f;
    }
}
