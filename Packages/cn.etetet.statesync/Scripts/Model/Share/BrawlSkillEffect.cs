namespace ET
{
    public enum BrawlSkillEffectKind
    {
        Projectile = 1,
        CirclePulse = 2,
    }

    /// <summary>步骤效果快照；瞬时范围效果复用弹丸命中数据，但不保留飞行实体。</summary>
    public struct BrawlSkillEffect
    {
        public BrawlSkillEffectKind Kind;
        public BrawlProjectileLaunch Launch;
    }
}
