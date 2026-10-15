using Unity.Mathematics;

namespace ET.Server
{
    [ChildOf(typeof(BrawlProjectileRoomComponent))]
    public sealed class BrawlProjectile : Entity, IAwake
    {
        public int ConfigId;
        public long OwnerUnitId;
        public BrawlFaction Faction;
        public BrawlCollisionRuleConfig[] Rules = System.Array.Empty<BrawlCollisionRuleConfig>();
        public float Lifetime;
        public float Age;
        public int MaxHitCount;
        public int HitCount;
        public float EffectValue;
        public float SuperChargeOnHit;
        public BrawlHitEventType HitEvent1;
        public BrawlHitEventType HitEvent2;
        public System.Collections.Generic.HashSet<long> HitUnits = new();
        public System.Collections.Generic.HashSet<int> HitTerrain = new();
        public float2 Position;
        public float2 Direction;
        public float Height;
        public float Speed;
        public float Radius;
        public float RemainingDistance;
        public bool Finished;
    }
}
