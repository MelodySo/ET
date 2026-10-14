namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlPracticeUnitComponent : Entity, IAwake
    {
        public BrawlPracticeUnitKind Kind;
        public BrawlFaction Faction;
        public BrawlNPCType NPCType;
        public float CollisionLength;
        public float CollisionWidth;
        public Unity.Mathematics.float2 CollisionPivot;
        public Unity.Mathematics.float2 PreviousPosition;
        public long OwnerPlayerId;
    }
}
