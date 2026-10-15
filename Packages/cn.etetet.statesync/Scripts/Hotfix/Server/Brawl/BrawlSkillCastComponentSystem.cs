namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlSkillCastComponent))]
    public static partial class BrawlSkillCastComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlSkillCastComponent self)
        {
            self.Launches = null; self.NextIndex = 0; self.StartedAt = 0;
            self.Rules = System.Array.Empty<BrawlCollisionRuleConfig>();
            self.NextNormalCastAt = 0; self.NextSuperCastAt = 0;
            self.Direction = default; self.Height = 0; self.ProjectileConfigId = 0; self.Faction = default;
        }
    }
}
