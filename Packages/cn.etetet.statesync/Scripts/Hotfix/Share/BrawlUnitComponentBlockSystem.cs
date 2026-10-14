using Unity.Mathematics;

namespace ET
{
    [EntitySystemOf(typeof(BrawlUnitComponentBlock))]
    public static partial class BrawlUnitComponentBlockSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlUnitComponentBlock self, BrawlUnitComponentBlockConfig config, BrawlNPCConfig npc)
        {
            // 配置由 NPC 挂载入口校验；每次 Awake 完整覆盖，避免对象池复用遗留状态。
            self.ConfigId = config.Id;
            self.CanBeDestroyed = config.CanBeDestroyed;
            self.Length = npc.CollisionLength;
            self.Width = npc.CollisionWidth;
            self.Height = npc.CollisionHeight;
            self.Pivot = new float2(npc.PivotX, npc.PivotZ);
        }
    }
}
