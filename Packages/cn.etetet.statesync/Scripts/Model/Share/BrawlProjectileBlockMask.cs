using System;

namespace ET
{
    [Flags]
    public enum BrawlProjectileBlockMask
    {
        None = 0,
        IndestructibleWall = 1,
        DestructibleWall = 2,
        Enemy = 4,
        Ally = 8,
        DestructibleGrass = 16,
    }
}
