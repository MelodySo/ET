using System;

namespace ET
{
    /// <summary>逻辑身份标签，与 Unity GameObject.layer 无关。</summary>
    [Flags]
    public enum BrawlUnitLayer
    {
        None = 0,
        Hero = 1,
        Monster = 2,
        Battle = 4,
        Tile = 8,
        Grass = 16,
        Destroyable = 32,
        All = Hero | Monster | Battle | Tile | Grass | Destroyable,
    }
}
