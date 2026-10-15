using System;
using System.Collections.Generic;
using Unity.Mathematics;
namespace ET.Server
{
    public static class BrawlUnitFactory
    {
        public static Unit Create(UnitComponent units, int configId, BrawlPracticeUnitKind kind, BrawlFaction faction,
            long playerId, float3 position, int participantSlot = -1, bool isRespawn = false)
        {
            // ConfigId 在本模式中指向 BrawlHero/BrawlNPC，不调用原 MMO 的 Unit.Config()。
            Unit unit = units.AddChild<Unit, int>(configId);
            unit.UnitType = kind == BrawlPracticeUnitKind.NPC ? UnitType.NPC : UnitType.Player;
            unit.Position = position;
            unit.Rotation = quaternion.identity;
            BrawlPracticeUnitComponent data = unit.AddComponent<BrawlPracticeUnitComponent>();
            data.Kind = kind;
            data.Faction = faction;
            data.PreviousPosition = unit.Position.xz;
            data.OwnerPlayerId = playerId;
            BrawlSpawnComponent spawn = unit.AddComponent<BrawlSpawnComponent>();
            spawn.OriginalPosition = position;
            spawn.ParticipantSlot = participantSlot;
            BrawlDeathComponent death = unit.AddComponent<BrawlDeathComponent>();
            long now = units.Scene().GetComponent<BrawlLifecycleRoomComponent>().SimulationTime;
            if (kind != BrawlPracticeUnitKind.NPC)
            {
                data.UnitLayerMask = BrawlUnitLayer.Hero | BrawlUnitLayer.Battle;
                unit.AddComponent<BrawlSkillCastComponent>();
                unit.AddComponent<BrawlAmmoComponent>();
                unit.AddComponent<BrawlSuperChargeComponent>();
                BrawlHeroConfig hero = units.Fiber().GetSingleton<BrawlHeroConfigCategory>().Get(configId);
                if (hero.MoveSpeedTier <= 0) throw new InvalidOperationException($"英雄 {configId} 的移动挡位必须大于零。");
                float health = BrawlEffectValueHelper.Calculate(hero.BaseHp, hero.HpGrowthPerLevel, hero.Level);
                unit.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new()
                {
                    [PropertyId.Level] = hero.Level,
                    [PropertyId.BaseMaxHealth] = health,
                    [PropertyId.Health] = health,
                    [PropertyId.BaseMoveSpeed] = hero.MoveSpeedTier,
                });
                // 未配置普攻的单位没有弹药；已有草稿技能仍可生成单位，但施放验证独立处理。
                if (hero.NormalAttackSkillId > 0)
                {
                    BrawlSkillConfig normal = units.Fiber().GetSingleton<BrawlSkillConfigCategory>().GetOrDefault(hero.NormalAttackSkillId);
                    if (normal != null) BrawlAmmoHelper.Initialize(unit, normal, isRespawn, now);
                }
                BrawlSkillConfig super = hero.SuperSkillId > 0 ? units.Fiber().GetSingleton<BrawlSkillConfigCategory>().GetOrDefault(hero.SuperSkillId) : null;
                BrawlSuperChargeHelper.Initialize(unit, super?.AmmoCapacity ?? 0, now);
                BrawlMoveComponent move = unit.AddComponent<BrawlMoveComponent>();
                move.Speed = hero.MoveSpeedTier;
                move.Radius = hero.CollisionRadius;
                data.CollisionLength = hero.CollisionRadius * 2;
                BrawlGlobalConfigCategory global = units.Fiber().GetSingleton<BrawlGlobalConfigCategory>();
                spawn.Active = true;
                spawn.CompleteAt = checked(now + BrawlUnitLifeHelper.Milliseconds(global.HeroSpawnDuration));
                spawn.ProtectionDuration = BrawlUnitLifeHelper.Milliseconds(global.HeroSpawnProtectionDuration);
                death.Duration = BrawlUnitLifeHelper.Milliseconds(global.HeroDeathDuration);
            }
            else
            {
                BrawlNPCConfig npc = units.Fiber().GetSingleton<BrawlNPCConfigCategory>().Get(configId);
                unit.AddComponent<PropertyComponent, Dictionary<PropertyId, double>>(new()
                {
                    [PropertyId.Level] = 1,
                    [PropertyId.BaseMaxHealth] = npc.BaseHp,
                    [PropertyId.Health] = npc.BaseHp,
                    [PropertyId.BaseMoveSpeed] = npc.MoveSpeedTier,
                });
                data.Faction = (BrawlFaction)npc.Faction;
                data.NPCType = (BrawlNPCType)npc.NPCType;
                data.CollisionLength = npc.CollisionLength;
                data.CollisionWidth = npc.CollisionWidth;
                data.CollisionPivot = new float2(npc.PivotX, npc.PivotZ);
                death.Duration = BrawlUnitLifeHelper.Milliseconds(npc.DeathDuration);
                death.RespawnDelay = BrawlUnitLifeHelper.Milliseconds(npc.RespawnDelay);
                BrawlNPCBlockHelper.Attach(unit, npc,
                    units.Fiber().GetSingleton<BrawlRespawnPointConfigCategory>());
            }
            unit.AddComponent<StateComponent>();
            BrawlUnitLifeHelper.OnSpawn(unit, now);
            if (kind == BrawlPracticeUnitKind.Bot && configId == units.Fiber().GetSingleton<BrawlAIConfigCategory>().HeroId)
                unit.AddComponent<BrawlAIComponent>();
            units.Scene().GetComponent<BrawlLifecycleRoomComponent>().RosterDirty = true;
            return unit;
        }

    }
}
