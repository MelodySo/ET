namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlPracticeUnitComponent : Entity, IAwake
    {
        public BrawlPracticeUnitKind Kind;
        public int TeamId;
        public long OwnerPlayerId;
    }
}
