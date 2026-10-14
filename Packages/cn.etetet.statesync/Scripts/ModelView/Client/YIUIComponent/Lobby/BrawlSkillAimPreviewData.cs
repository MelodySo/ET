using UnityEngine;

namespace ET.Client
{
    public struct BrawlSkillAimPreviewData
    {
        public BrawlSkillSlot Slot;
        public int SkillId;
        // 预留完整范围/施放范围参数，由未来表现或发射器对应规则解释。
        public BrawlSkillConfig Config;
        public Vector3 Origin;
        public Vector2 Direction;
    }
}
