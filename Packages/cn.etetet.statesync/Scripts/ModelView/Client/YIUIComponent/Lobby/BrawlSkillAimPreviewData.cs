using UnityEngine;

namespace ET.Client
{
    public struct BrawlSkillAimPreviewData
    {
        public BrawlSkillSlot Slot;
        public int SkillId;
        // 完整范围参数；施放范围类型为0时使用技能范围，尺寸与发射器规则一致。
        public BrawlSkillConfig Config;
        public Vector3 Origin;
        public Vector2 Direction;
    }
}
