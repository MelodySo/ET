using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlMoveComponent : Entity, IAwake
    {
        public float2 DesiredDirection;
        // 面向与移动互相独立，之后由技能规则控制；本轮不驱动模型旋转。
        public float2 FacingDirection;
        public float2 Velocity;
        public float Speed;
        public float Radius;
    }
}
