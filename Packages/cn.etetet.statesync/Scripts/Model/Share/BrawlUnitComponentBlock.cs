using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// NPC 的可选移动阻挡能力。挂载表示阻挡英雄，移除表示解除阻挡。
    /// 碰撞形状与偏移由 NPC 配置提供，可摧毁属性由阻挡组件配置提供。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlUnitComponentBlock : Entity, IAwake<BrawlUnitComponentBlockConfig, BrawlNPCConfig>
    {
        /// <summary>BrawlUnitComponentBlock 表的行 ID，对应 NPC 的 ComponentValues 项。</summary>
        public int ConfigId;
        public bool CanBeDestroyed;
        /// <summary>米；Width=0 为圆柱，Length 是直径，否则 Length/Width 是完整 X/Z 尺寸。</summary>
        public float Length;
        public float Width;
        /// <summary>脚底以上完整高度；当前 XZ 移动不使用高度分层。</summary>
        public float Height;
        /// <summary>相对单位原点的 X/Z 碰撞中心偏移，单位米。当前 NPC 轴对齐。</summary>
        public float2 Pivot;
    }
}
