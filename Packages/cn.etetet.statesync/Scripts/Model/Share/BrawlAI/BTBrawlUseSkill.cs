namespace ET
{
    public sealed class BTBrawlUseSkill : BTAction
    {
        [BTInput(typeof(Unit))] public string Unit = "Unit";
        public BrawlSkillSlot Slot;
    }
}
