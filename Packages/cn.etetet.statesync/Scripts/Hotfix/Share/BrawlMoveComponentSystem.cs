namespace ET
{
    [EntitySystemOf(typeof(BrawlMoveComponent))]
    public static partial class BrawlMoveComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlMoveComponent self)
        {
            self.DesiredDirection = default;
            self.Velocity = default;
            self.Speed = 0;
            // 创建单位时由英雄配置赋值，不保留固定半径兜底。
            self.Radius = 0;
        }
    }
}
