using Unity.Mathematics;
namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlSpawnComponent : Entity, IAwake
    {
        public bool Active;
        public long CompleteAt;
        public long ProtectionDuration;
        public float3 OriginalPosition;
        public int ParticipantSlot;
    }
}
