namespace ET
{
    /// <summary>无实体生命周期的发射算法；不处理网络、模型、伤害或弹药。</summary>
    public interface IBrawlSkillEmitter
    {
        BrawlProjectileLaunch[] Emit(BrawlSkillConfig skill, BrawlProjectileConfig projectile);
    }
}
