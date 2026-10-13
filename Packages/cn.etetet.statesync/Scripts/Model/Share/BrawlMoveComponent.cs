using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlMoveComponent : Entity, IAwake
    {
        public float2 DesiredDirection;
        public float2 Velocity;
        public float Speed;
        public float Radius;
    }
}
