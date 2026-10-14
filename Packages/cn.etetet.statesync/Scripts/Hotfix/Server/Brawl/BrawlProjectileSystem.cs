using Unity.Mathematics;

namespace ET.Server
{
    [EntitySystemOf(typeof(BrawlProjectile))]
    public static partial class BrawlProjectileSystem
    {
        [EntitySystem]
        private static void Awake(this BrawlProjectile self)
        {
            self.ConfigId = 0;
            self.OwnerUnitId = 0;
            self.Faction = default;
            self.BlockMask = default;
            self.TargetMask = default;
            self.MaxHitCount = 0;
            self.HitCount = 0;
            self.EffectValue = 0;
            self.HitEvent1 = default;
            self.HitEvent2 = default;
            self.HitUnits.Clear();
            self.Position = default;
            self.Direction = default;
            self.Height = 0;
            self.Speed = 0;
            self.Radius = 0;
            self.RemainingDistance = 0;
            self.Finished = false;
        }

        public static void Advance(this BrawlProjectile self, float seconds)
        {
            float distance = math.min(self.Speed * seconds, self.RemainingDistance);
            self.Position += self.Direction * distance;
            self.RemainingDistance -= distance;
            self.Finished = self.RemainingDistance <= 0;
        }
    }
}
