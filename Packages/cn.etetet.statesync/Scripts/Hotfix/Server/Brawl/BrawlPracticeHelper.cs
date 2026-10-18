using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>首版单人练习房间，与 Gate 同 Fiber；连接销毁时房间自动销毁。</summary>
    public static class BrawlPracticeHelper
    {
        public static void Enter(Session session, C2G_BrawlPracticeEnter request, G2C_BrawlPracticeEnter response)
        {
            Player player = session.GetComponent<SessionPlayerComponent>()?.Player;
            if (player == null) throw new InvalidOperationException("请先登录。");
            if (request.EntryId <= 0) throw new InvalidOperationException("练习赛请求标识无效。");
            BrawlPracticeSessionComponent state = session.GetComponent<BrawlPracticeSessionComponent>();
            if (state != null && state.EntryId == request.EntryId)
            {
                if (state.HeroId != request.HeroId || state.LevelId != request.LevelId)
                    throw new InvalidOperationException("重复请求的英雄或关卡不一致。");
                FillSnapshot(state, response);
                return;
            }
            Fiber fiber = session.Fiber();
            BrawlLevelConfig level = fiber.GetSingleton<BrawlLevelConfigCategory>().GetOrDefault(request.LevelId);
            if (level == null) throw new InvalidOperationException($"关卡 {request.LevelId} 不存在，请导出最新配置。");
            BrawlModeConfig mode = fiber.GetSingleton<BrawlModeConfigCategory>().GetOrDefault(level.ModeId);
            if (mode == null || !mode.Enabled || mode.TeamSize != 1 || mode.TeamCount < 1 || mode.TeamCount > 2 || mode.TimeLimitSeconds != 0)
                throw new InvalidOperationException($"关卡 {level.Id} 的玩法必须启用，TeamSize=1、TeamCount=1或2、TimeLimitSeconds=0。");
            BrawlHeroConfig playerHero = fiber.GetSingleton<BrawlHeroConfigCategory>().GetOrDefault(request.HeroId);
            if (playerHero == null) throw new InvalidOperationException("玩家英雄配置不存在。");
            int botHeroId = 0;
            BrawlHeroConfig botHero = null;
            if (mode.TeamCount > 1)
            {
                botHeroId = fiber.GetSingleton<BrawlAIConfigCategory>().GetPracticeOpponent(request.HeroId);
                botHero = fiber.GetSingleton<BrawlHeroConfigCategory>().GetOrDefault(botHeroId);
                if (botHero == null) throw new InvalidOperationException("Bot 英雄配置不存在。");
            }
            int cellSize = fiber.GetSingleton<BrawlGlobalConfigCategory>().CellSize;
            if (cellSize <= 0) throw new InvalidOperationException("CellSize 必须大于零。");
            ValidateLevel(level, mode.TeamSize * mode.TeamCount, fiber.GetSingleton<BrawlNPCConfigCategory>(),
                fiber.GetSingleton<BrawlRespawnPointConfigCategory>());
            ValidateHeroSpawn(level, cellSize, level.SpawnPoints[0], playerHero);
            if (botHero != null) ValidateHeroSpawn(level, cellSize, level.SpawnPoints[mode.TeamSize], botHero);

            // 新的进入请求替换该连接尚未退出的旧房间，延迟退出按 EntryId 匹配，不会销毁新房间。
            session.RemoveComponent<BrawlPracticeSessionComponent>();
            state = session.AddComponent<BrawlPracticeSessionComponent>();
            try
            {
                state.EntryId = request.EntryId;
                state.LevelId = level.Id;
                state.HeroId = request.HeroId;
                state.CellSize = cellSize;
                Scene room = EntitySceneFactory.CreateScene(state, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "BrawlPractice");
                state.Room = room;
                UnitComponent units = room.AddComponent<UnitComponent>();
                room.AddComponent<BrawlProjectileRoomComponent>();
                BrawlLifecycleHelper.Initialize(state, mode, level, player.Id, botHeroId);
                for (int i = 0; i < level.NPCIds.Length; ++i)
                    BrawlUnitFactory.Create(units, level.NPCIds[i], BrawlPracticeUnitKind.NPC, BrawlFaction.NeutralHostile, 0,
                        BrawlLifecycleHelper.Position(level.NPCPoints[i], level, cellSize));
                // NPC 的尺寸与偏移可能伸入相邻出生格；不能只按格子索引判断重叠。
                foreach (Entity child in units.Children.Values)
                {
                    Unit unit = (Unit)child;
                    BrawlMoveComponent move = unit.GetComponent<BrawlMoveComponent>();
                    if (move != null && !BrawlUnitMovementHelper.CanOccupy(units, unit.Id, unit.Position.xz, move.Radius))
                        throw new InvalidOperationException($"英雄 {unit.ConfigId} 的出生位置与阻挡 NPC 重叠。");
                }
                FillSnapshot(state, response);
                room.GetComponent<BrawlLifecycleRoomComponent>().RosterDirty = false;
                room.GetComponent<BrawlLifecycleRoomComponent>().Notices.Clear();
                Log.Info($"Brawl practice created: room={room.Id} player={player.Id} level={level.Id} hero={request.HeroId} bot={botHeroId} units={response.Units.Count}");
            }
            catch
            {
                session.RemoveComponent<BrawlPracticeSessionComponent>();
                throw;
            }
        }

        public static void Exit(Session session, long entryId)
        {
            BrawlPracticeSessionComponent state = session.GetComponent<BrawlPracticeSessionComponent>();
            if (state == null || state.EntryId != entryId) return;
            Log.Info($"Brawl practice exit: entry={entryId}");
            session.RemoveComponent<BrawlPracticeSessionComponent>();
        }

        private static void ValidateHeroSpawn(BrawlLevelConfig level, int cellSize, int point, BrawlHeroConfig hero)
        {
            if (!math.isfinite(hero.CollisionRadius) || hero.CollisionRadius <= 0)
                throw new InvalidOperationException($"英雄 {hero.Id} 的 CollisionRadius 必须为有限正数（米）。");
            float2 position = BrawlTerrainMovementHelper.CellPosition(level, point, cellSize);
            if (!BrawlTerrainMovementHelper.CanOccupy(level, cellSize, position, hero.CollisionRadius))
                throw new InvalidOperationException($"出生格 {point} 容纳不下英雄 {hero.Id}，碰撞半径为 {hero.CollisionRadius} 米。");
        }

        private static void ValidateLevel(BrawlLevelConfig level, int playerCount, BrawlNPCConfigCategory npcs,
            BrawlRespawnPointConfigCategory points)
        {
            if (level.RowCount < 1 || level.RowCount > 100 || level.ColumnCount < 1 || level.ColumnCount > 100 ||
                level.MapGrid == null || level.MapGrid.Length != level.RowCount * level.ColumnCount ||
                level.SpawnPoints == null || level.SpawnPoints.Length != playerCount ||
                level.NPCIds == null || level.NPCPoints == null || level.NPCIds.Length != level.NPCPoints.Length)
                throw new InvalidOperationException("练习赛关卡网格、出生点数量或 NPC 数组非法。");
            HashSet<int> occupied = new();
            foreach (int point in level.SpawnPoints) ValidatePoint(level, occupied, point);
            for (int i = 0; i < level.NPCIds.Length; ++i)
            {
                BrawlNPCConfig npc = npcs.GetOrDefault(level.NPCIds[i]);
                if (npc == null) throw new InvalidOperationException($"NPC 配置不存在：{level.NPCIds[i]}");
                if (npc.NPCType == (int)BrawlNPCType.Spawn)
                    throw new InvalidOperationException("复活点只通过地图SpawnPoints创建，不能重复配置在NPCIds中。");
                BrawlUnitLifeHelper.Milliseconds(npc.DeathDuration);
                BrawlUnitLifeHelper.Milliseconds(npc.RespawnDelay);
                if (!BrawlFactionHelper.IsValid(npc.Faction)) throw new InvalidOperationException($"NPC {npc.Id} 的 Faction 非法。");
                if (npc.NPCType == (int)BrawlNPCType.Combatant &&
                    (!math.isfinite(npc.CollisionLength) || npc.CollisionLength <= 0 || !math.isfinite(npc.CollisionWidth) ||
                     npc.CollisionWidth < 0 || !math.isfinite(npc.PivotX) || !math.isfinite(npc.PivotZ)))
                    throw new InvalidOperationException($"NPC {npc.Id} 的作战碰撞尺寸非法。");
                BrawlNPCBlockHelper.Validate(npc, points);
                ValidatePoint(level, occupied, level.NPCPoints[i]);
            }
        }

        private static void ValidatePoint(BrawlLevelConfig level, HashSet<int> occupied, int point)
        {
            if (point < 0 || point >= level.MapGrid.Length || !occupied.Add(point))
                throw new InvalidOperationException($"出生格越界或重叠：{point}");
            if (level.MapGrid[point] != (int)BrawlTileType.Ground && level.MapGrid[point] != (int)BrawlTileType.Grass)
                throw new InvalidOperationException($"出生格必须在平地或草丛：{point}");
        }

        private static void FillSnapshot(BrawlPracticeSessionComponent state, G2C_BrawlPracticeEnter response)
        {
            Scene room = state.Room;
            response.RoomId = room.Id;
            response.LevelId = state.LevelId;
            response.CellSize = state.CellSize;
            response.MyUnitId = state.MyUnitId;
            foreach (Entity child in room.GetComponent<UnitComponent>().Children.Values)
            {
                response.Units.Add(BrawlUnitSnapshotHelper.Create((Unit)child));
            }
        }
    }
}
