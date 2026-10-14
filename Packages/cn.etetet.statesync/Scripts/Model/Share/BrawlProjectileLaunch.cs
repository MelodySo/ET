using Unity.Mathematics;

namespace ET
{
    /// <summary>发射器模型空间的初始条件：X 向右、Y 对应世界 Z 向前。当前只实现立即发射、匀速直线。</summary>
    public struct BrawlProjectileLaunch
    {
        public float2 Offset;
        public float2 Direction;
        public float Speed;
        public float Radius;
        public float Range;
    }
}
