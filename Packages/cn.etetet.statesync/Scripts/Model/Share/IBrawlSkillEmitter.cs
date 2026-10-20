namespace ET
{
    /// <summary>代码定义技能步骤，不处理实体生命周期、网络或弹药消耗。</summary>
    public interface IBrawlSkillEmitter
    {
        BrawlSkillPlan Build(BrawlSkillConfig skill, BrawlProjectileConfig projectile, int level);
    }
}
