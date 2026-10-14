using System;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>NPC 组件配置的校验与挂载；类型和值按相同下标配对。</summary>
    public static class BrawlNPCBlockHelper
    {
        public static void Validate(BrawlNPCConfig npc, BrawlUnitComponentBlockConfigCategory blocks, BrawlRespawnPointConfigCategory points = null)
        {
            if (npc.ComponentTypes == null || npc.ComponentValues == null || npc.ComponentTypes.Length != npc.ComponentValues.Length)
                throw new InvalidOperationException($"NPC {npc.Id} 的组件类型和值数组必须存在且等长。");
            bool hasBlock = false;
            bool hasPoint = false;
            for (int i = 0; i < npc.ComponentTypes.Length; ++i)
            {
                if (npc.ComponentTypes[i] == (int)BrawlUnitComponentType.RespawnPoint)
                {
                    if (hasPoint || npc.NPCType != (int)BrawlNPCType.Spawn || points == null || points.GetOrDefault(npc.ComponentValues[i]) == null)
                        throw new InvalidOperationException($"NPC {npc.Id} 的复活点组件重复、类型不匹配或配置不存在。");
                    hasPoint = true;
                    continue;
                }
                if (npc.ComponentTypes[i] != (int)BrawlUnitComponentType.Block)
                    throw new InvalidOperationException($"NPC {npc.Id} 的组件类型 {npc.ComponentTypes[i]} 尚不支持。");
                if (hasBlock) throw new InvalidOperationException($"NPC {npc.Id} 重复配置阻挡组件。");
                hasBlock = true;
                int id = npc.ComponentValues[i];
                if (id <= 0 || blocks.GetOrDefault(id) == null)
                    throw new InvalidOperationException($"NPC {npc.Id} 引用的阻挡组件配置 {id} 不存在。");
            }
            if (!hasBlock) return;
            if (!math.isfinite(npc.CollisionLength) || npc.CollisionLength <= 0 ||
                !math.isfinite(npc.CollisionHeight) || npc.CollisionHeight <= 0 ||
                !math.isfinite(npc.CollisionWidth) || npc.CollisionWidth < 0 ||
                !math.isfinite(npc.PivotX) || !math.isfinite(npc.PivotZ))
                throw new InvalidOperationException($"NPC {npc.Id} 阻挡尺寸非法：长度/高度须为有限正数，宽度须为有限非负数，偏移须为有限数（米）。");
        }

        public static void Attach(Unit unit, BrawlNPCConfig npc, BrawlUnitComponentBlockConfigCategory blocks, BrawlRespawnPointConfigCategory points = null)
        {
            Validate(npc, blocks, points);
            for (int i = 0; i < npc.ComponentTypes.Length; ++i)
            {
                if (npc.ComponentTypes[i] == (int)BrawlUnitComponentType.RespawnPoint)
                {
                    unit.AddComponent<BrawlRespawnPointComponent, int>(npc.ComponentValues[i]);
                    continue;
                }
                BrawlUnitComponentBlockConfig config = blocks.Get(npc.ComponentValues[i]);
                unit.AddComponent<BrawlUnitComponentBlock, BrawlUnitComponentBlockConfig, BrawlNPCConfig>(config, npc);
            }
        }
    }
}
