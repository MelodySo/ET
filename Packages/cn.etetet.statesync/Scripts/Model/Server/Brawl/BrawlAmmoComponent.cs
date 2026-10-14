namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlAmmoComponent : Entity, IAwake
    {
        public long LastUpdateAt;
    }
}
