using Unity.Mathematics;
namespace ET.Server
{
    [ComponentOf(typeof(Unit))]
    public sealed class BrawlSkillCastComponent : Entity, IAwake
    {
        public BrawlProjectileLaunch[] Launches;
        public int NextIndex;
        public long StartedAt;
        public long NextCastAt;
        public float2 Direction;
        public float Height;
        public int ProjectileConfigId;
        public BrawlFaction Faction;
    }
}
