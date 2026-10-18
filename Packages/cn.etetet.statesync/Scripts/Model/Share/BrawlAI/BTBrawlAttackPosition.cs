namespace ET
{
    /// <summary>到达站定距离且对应技能的墙体通路畅通，不判断弹药。</summary>
    public sealed class BTBrawlAttackPosition : BTCondition
    {
        [BTInput(typeof(Unit))] public string Unit = "Unit";
        public BrawlSkillSlot Slot;
        public float RangeMargin = 0.5f;
        public float SightWidth = 0.2f;
    }
}
