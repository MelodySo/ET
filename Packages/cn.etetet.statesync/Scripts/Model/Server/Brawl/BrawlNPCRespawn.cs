using Unity.Mathematics;
namespace ET.Server
{
    public struct BrawlNPCRespawn
    {
        public int ConfigId;
        public BrawlFaction Faction;
        public float3 Position;
        public long ReadyAt;
    }
}
