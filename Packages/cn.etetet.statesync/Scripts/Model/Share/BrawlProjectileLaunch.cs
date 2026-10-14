using Unity.Mathematics;

namespace ET
{
    /// <summary>发射器模型空间的初始条件：X向右、Y向前；延迟相对本次施放开始计算。</summary>
    public struct BrawlProjectileLaunch
    {
        public float2 Offset;
        public float2 Direction;
        public float Speed;
        public float Radius;
        public float Range;
        public float EffectValue;
        public long DelayMilliseconds;
    }
}
