namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlSuperChargeComponent : Entity, IAwake
    {
        public long LastUpdateAt;
    }
}
