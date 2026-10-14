using System;

namespace ET
{
    [Flags]
    public enum BrawlProjectileTargetMask
    {
        None = 0,
        Enemy = 1,
        Ally = 2,
        DestructibleWall = 4,
        DestructibleGrass = 8,
    }
}
