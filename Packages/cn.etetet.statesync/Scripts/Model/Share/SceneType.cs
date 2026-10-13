namespace ET
{
    public static partial class SceneType
    {
        // 客户端
        public const int StateSync = PackageType.StateSync * 1000 + 1;
        // 服务端练习房间，与 Gate 共用 Fiber，但独立于原 MMO 地图事件。
        public const int BrawlPractice = PackageType.StateSync * 1000 + 2;
    }
}
