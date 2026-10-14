namespace ET
{
    // 编号是稳定标识，不是数组下标；删除后不复用。
    public enum PropertyId
    {
        Level = 100,
        BaseMaxHealth = 200, MaxHealthBonus = 210, MaxHealthBonusRatio = 220, MaxHealth = 230, Health = 240,
        BaseHealthRegenRate = 300, HealthRegenRateBonus = 310, HealthRegenRateBonusRatio = 320, HealthRegenRate = 330,
        Shield = 400, DamageReductionRatio = 410,
        BaseMoveSpeed = 500, MoveSpeedBonus = 510, MoveSpeedBonusRatio = 520, MoveSpeed = 530,
        MaxAmmo = 600, Ammo = 610,
        BaseReloadRate = 700, ReloadRateBonus = 710, ReloadRateBonusRatio = 720, ReloadRate = 730,
        MaxSuperAmmo = 800, SuperAmmo = 810, BaseSuperChargeRegenRate = 820,
        SuperChargeRegenRateBonus = 830, SuperChargeRegenRateBonusRatio = 840, SuperChargeRegenRate = 850,
        // 900/910已废弃：眩晕/击飞迁移到StateId，不复用属性编号。
    }
}
