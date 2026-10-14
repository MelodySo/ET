using Unity.Mathematics;

namespace ET.Server
{
    [ChildOf(typeof(BrawlProjectileRoomComponent))]
    public sealed class BrawlProjectile : Entity, IAwake
    {
        public int ConfigId;
        public long OwnerUnitId;
        public BrawlFaction Faction;
        public BrawlProjectileBlockMask BlockMask;
        public BrawlProjectileTargetMask TargetMask;
        public int MaxHitCount;
        public int HitCount;
        public System.Collections.Generic.HashSet<long> HitUnits = new();
        public float2 Position;
        public float2 Direction;
        public float Height;
        public float Speed;
        public float Radius;
        public float RemainingDistance;
        public bool Finished;
    }
}
