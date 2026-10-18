using System;
using System.Collections.Generic;
using Unity.Mathematics;
namespace ET.Server
{
    public static class BrawlLifecycleHelper
    {
        public static void Initialize(BrawlPracticeSessionComponent state, BrawlModeConfig mode, BrawlLevelConfig level, long playerId, int botHeroId)
        {
            Scene scene = state.Room;
            UnitComponent units = scene.GetComponent<UnitComponent>();
            BrawlLifecycleRoomComponent life = scene.AddComponent<BrawlLifecycleRoomComponent>();
            BrawlGlobalConfigCategory global = state.Fiber().GetSingleton<BrawlGlobalConfigCategory>();
            life.RespawnPointIds = new long[level.SpawnPoints.Length];
            for (int i = 0; i < level.SpawnPoints.Length; ++i)
            {
                bool red = i < mode.TeamSize;
                BrawlFaction faction = red ? BrawlFaction.Red : BrawlFaction.Blue;
                Unit point = BrawlUnitFactory.Create(units, red ? global.RedRespawnPointNpcId : global.BlueRespawnPointNpcId,
                    BrawlPracticeUnitKind.NPC, faction, 0, Position(level.SpawnPoints[i], level, state.CellSize));
                BrawlRespawnPointComponent component = point.GetComponent<BrawlRespawnPointComponent>();
                if (component == null) throw new InvalidOperationException("Respawn point NPC requires a respawn point component.");
                point.GetComponent<BrawlPracticeUnitComponent>().Faction = faction;
                component.ParticipantSlot = i;
                life.RespawnPointIds[i] = point.Id;
            }
            // 当前练习赛仍为1v1；映射数组按TeamSize支持红队在前、蓝队在后。
            AddParticipant(state, 0, state.HeroId, BrawlPracticeUnitKind.Player, BrawlFaction.Red, playerId);
            AddParticipant(state, mode.TeamSize, botHeroId, BrawlPracticeUnitKind.Bot, BrawlFaction.Blue, 0);
        }

        private static void AddParticipant(BrawlPracticeSessionComponent state, int slot, int heroId, BrawlPracticeUnitKind kind, BrawlFaction faction, long playerId)
        {
            Scene scene = state.Room;
            BrawlLifecycleRoomComponent life = scene.GetComponent<BrawlLifecycleRoomComponent>();
            life.Participants.Add(slot, new BrawlParticipant { HeroId = heroId, Kind = kind, Faction = faction, PlayerId = playerId, RespawnPointId = life.RespawnPointIds[slot] });
            long unitId = SpawnHero(scene, slot);
            if (kind == BrawlPracticeUnitKind.Player) state.MyUnitId = unitId;
        }

        private static long SpawnHero(Scene scene, int slot)
        {
            BrawlLifecycleRoomComponent life = scene.GetComponent<BrawlLifecycleRoomComponent>();
            UnitComponent units = scene.GetComponent<UnitComponent>();
            BrawlParticipant participant = life.Participants[slot];
            Unit point = units.Get(participant.RespawnPointId);
            Unit unit = BrawlUnitFactory.Create(units, participant.HeroId, participant.Kind, participant.Faction, participant.PlayerId,
                point.Position, slot);
            BrawlSuperChargeHelper.Restore(unit, participant.SuperAmmo);
            participant.UnitId = unit.Id;
            life.Participants[slot] = participant;
            return unit.Id;
        }

        public static long Tick(Scene scene)
        {
            UnitComponent units = scene.GetComponent<UnitComponent>();
            BrawlLifecycleRoomComponent life = scene.GetComponent<BrawlLifecycleRoomComponent>();
            long now = life.SimulationTime;
            List<long> removed = new();
            foreach (Entity child in units.Children.Values)
            {
                Unit unit = (Unit)child;
                BrawlUnitLifeHelper.CompleteSpawn(unit, now);
                BrawlUnitLifeHelper.CheckDeath(unit);
                BrawlDeathComponent death = unit.GetComponent<BrawlDeathComponent>();
                if (death.Active && death.DestroyAt <= now) removed.Add(unit.Id);
            }
            foreach (long id in removed)
            {
                Unit unit = units.Get(id);
                BrawlSpawnComponent spawn = unit.GetComponent<BrawlSpawnComponent>();
                BrawlDeathComponent death = unit.GetComponent<BrawlDeathComponent>();
                BrawlPracticeUnitComponent data = unit.GetComponent<BrawlPracticeUnitComponent>();
                int slot = spawn.ParticipantSlot;
                if (data.Kind == BrawlPracticeUnitKind.NPC && data.NPCType != BrawlNPCType.Gem && data.NPCType != BrawlNPCType.Spawn && death.RespawnDelay > 0)
                    life.NPCRespawns.Add(new BrawlNPCRespawn { ConfigId = unit.ConfigId, Faction = data.Faction,
                        Position = spawn.OriginalPosition, ReadyAt = checked(now + death.RespawnDelay) });
                BrawlUnitLifeHelper.Notify(unit, BrawlLifePhase.Removed, now, now);
                units.Remove(id);
                life.RosterDirty = true;
                if (slot >= 0) SpawnHero(scene, slot);
            }
            for (int i = life.NPCRespawns.Count - 1; i >= 0; --i)
            {
                BrawlNPCRespawn pending = life.NPCRespawns[i];
                if (pending.ReadyAt > now) continue;
                Unit unit = BrawlUnitFactory.Create(units, pending.ConfigId, BrawlPracticeUnitKind.NPC, pending.Faction, 0, pending.Position);
                unit.GetComponent<BrawlPracticeUnitComponent>().Faction = pending.Faction;
                life.NPCRespawns.RemoveAt(i);
            }
            foreach (BrawlParticipant participant in life.Participants.Values)
                if (participant.Kind == BrawlPracticeUnitKind.Player) return participant.UnitId;
            return 0;
        }

        public static float3 Position(int point, BrawlLevelConfig level, int cellSize)
        {
            float2 position = BrawlTerrainMovementHelper.CellPosition(level, point, cellSize);
            return new float3(position.x, 0, position.y);
        }
    }
}
