using Unity.Mathematics;

namespace ET
{
    /// <summary>一次本地技能手势的数据，不进入网络同步。</summary>
    [EnableClass]
    public sealed class BrawlSkillAimState
    {
        public bool Active;
        public bool EverOutsideDeadZone;
        public float2 Direction;
    }
}
