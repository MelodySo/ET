namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlRespawnPointComponent : Entity, IAwake<int>
    {
        public int ConfigId;
        public int ParticipantSlot;
    }
}
