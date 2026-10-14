namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlDeathComponent : Entity, IAwake
    {
        public bool Active;
        public long StartedAt;
        public long DestroyAt;
        public long Duration;
        public long RespawnDelay;
    }
}
