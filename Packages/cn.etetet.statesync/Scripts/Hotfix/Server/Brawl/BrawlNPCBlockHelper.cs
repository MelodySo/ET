using System;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>NPC逻辑标签、移动阻挡与可选组件的校验及初始化；阻挡不再由组件类型1驱动。</summary>
    public static class BrawlNPCBlockHelper
    {
        public static void Validate(BrawlNPCConfig npc, BrawlRespawnPointConfigCategory points = null)
        {
            if (!BrawlFactionHelper.IsValid(npc.Faction) || (npc.UnitLayerMask & ~(int)BrawlUnitLayer.All) != 0)
                throw new InvalidOperationException($"NPC {npc.Id} 的阵营或标签非法。");
            if (npc.ComponentTypes == null || npc.ComponentValues == null || npc.ComponentTypes.Length != npc.ComponentValues.Length)
                throw new InvalidOperationException($"NPC {npc.Id} 的组件类型和值数组必须存在且等长。");
            bool hasPoint = false;
            for (int i = 0; i < npc.ComponentTypes.Length; ++i)
            {
                if (npc.ComponentTypes[i] != (int)BrawlUnitComponentType.RespawnPoint)
                    throw new InvalidOperationException($"NPC {npc.Id} 的组件类型 {npc.ComponentTypes[i]} 尚不支持；旧阻挡组件请迁移到UnitLayerMask/BlocksMovement。");
                if (hasPoint || npc.NPCType != (int)BrawlNPCType.Spawn || points == null || points.GetOrDefault(npc.ComponentValues[i]) == null)
                    throw new InvalidOperationException($"NPC {npc.Id} 的复活点组件重复、类型不匹配或配置不存在。");
                hasPoint = true;
            }
            if (!npc.BlocksMovement && npc.UnitLayerMask == 0) return;
            if (!math.isfinite(npc.CollisionLength) || npc.CollisionLength <= 0 ||
                !math.isfinite(npc.CollisionHeight) || npc.CollisionHeight <= 0 ||
                !math.isfinite(npc.CollisionWidth) || npc.CollisionWidth < 0 ||
                !math.isfinite(npc.PivotX) || !math.isfinite(npc.PivotZ))
                throw new InvalidOperationException($"NPC {npc.Id} 的碰撞尺寸非法。");
        }

        public static void Attach(Unit unit, BrawlNPCConfig npc, BrawlRespawnPointConfigCategory points = null)
        {
            Validate(npc, points);
            BrawlPracticeUnitComponent data = unit.GetComponent<BrawlPracticeUnitComponent>();
            data.UnitLayerMask = (BrawlUnitLayer)npc.UnitLayerMask;
            data.BlocksMovement = npc.BlocksMovement;
            data.CollisionLength = npc.CollisionLength;
            data.CollisionWidth = npc.CollisionWidth;
            data.CollisionPivot = new float2(npc.PivotX, npc.PivotZ);
            for (int i = 0; i < npc.ComponentTypes.Length; ++i)
                unit.AddComponent<BrawlRespawnPointComponent, int>(npc.ComponentValues[i]);
        }
    }
}
