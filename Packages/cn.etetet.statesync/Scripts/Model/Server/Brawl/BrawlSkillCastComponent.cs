using Unity.Mathematics;
namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlSkillCastComponent : Entity, IAwake
    {
        public BrawlSkillPlan Plan;
        public BrawlCollisionRuleConfig[] Rules;
        public int NextIndex;
        public long StartedAt;
        public long NextNormalCastAt;
        public long NextSuperCastAt;
        public float2 Direction;
        public float Height;
        public int ProjectileConfigId;
        public BrawlFaction Faction;
        public long PresentationCastId;
        public int SkillId;
        public BrawlSkillSlot Slot;
    }
}
