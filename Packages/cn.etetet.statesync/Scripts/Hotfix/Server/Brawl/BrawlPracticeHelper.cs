using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Server
{
    /// <summary>首版单人练习房间，与 Gate 同 Fiber；连接销毁时房间自动销毁。</summary>
    public static class BrawlPracticeHelper
    {
        // 首版入口与 Bot 按本轮约定固定，人数、出生点和 NPC 均读取配置。
        private const int PracticeModeId = 102;
        private const int PracticeLevelId = 10201;
        private const int BotHeroId = 201;

        public static void Enter(Session session, C2G_BrawlPracticeEnter request, G2C_BrawlPracticeEnter response)
        {
            Player player = session.GetComponent<SessionPlayerComponent>()?.Player;
            if (player == null) throw new InvalidOperationException("请先登录。");
            if (request.EntryId <= 0) throw new InvalidOperationException("练习赛请求标识无效。");
            BrawlPracticeSessionComponent state = session.GetComponent<BrawlPracticeSessionComponent>();
            if (state != null && state.EntryId == request.EntryId)
            {
                if (state.HeroId != request.HeroId) throw new InvalidOperationException("重复请求的英雄不一致。");
                FillSnapshot(state, response);
                return;
            }
            Fiber fiber = session.Fiber();
            BrawlModeConfig mode = fiber.GetSingleton<BrawlModeConfigCategory>().GetOrDefault(PracticeModeId);
            BrawlLevelConfig level = fiber.GetSingleton<BrawlLevelConfigCategory>().GetOrDefault(PracticeLevelId);
            if (mode == null || !mode.Enabled || mode.TeamSize != 1 || mode.TeamCount != 2 || mode.TimeLimitSeconds != 0)
                throw new InvalidOperationException("玩法 102 必须启用，TeamSize=1、TeamCount=2、TimeLimitSeconds=0。");
            if (level == null || level.ModeId != mode.Id) throw new InvalidOperationException("练习赛关卡 10201 不存在或 ModeId 不为 102。");
            if (fiber.GetSingleton<BrawlHeroConfigCategory>().GetOrDefault(request.HeroId) == null ||
                fiber.GetSingleton<BrawlHeroConfigCategory>().GetOrDefault(BotHeroId) == null)
                throw new InvalidOperationException("玩家或 Bot 英雄配置不存在。");
            int cellSize = fiber.GetSingleton<BrawlGlobalConfigCategory>().CellSize;
            if (cellSize <= 0) throw new InvalidOperationException("CellSize 必须大于零。");
            ValidateLevel(level, mode.TeamSize * mode.TeamCount, fiber.GetSingleton<BrawlNPCConfigCategory>());

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
                Unit hero = CreateUnit(units, request.HeroId, BrawlPracticeUnitKind.Player, 1, player.Id,
                    level.SpawnPoints[0], level.ColumnCount, cellSize);
                state.MyUnitId = hero.Id;
                CreateUnit(units, BotHeroId, BrawlPracticeUnitKind.Bot, 2, 0, level.SpawnPoints[1], level.ColumnCount, cellSize);
                for (int i = 0; i < level.NPCIds.Length; ++i)
                    CreateUnit(units, level.NPCIds[i], BrawlPracticeUnitKind.NPC, 0, 0, level.NPCPoints[i], level.ColumnCount, cellSize);
                FillSnapshot(state, response);
                Log.Info($"Brawl practice created: room={room.Id} player={player.Id} level={level.Id} hero={request.HeroId} bot={BotHeroId} units={response.Units.Count}");
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

        private static void ValidateLevel(BrawlLevelConfig level, int playerCount, BrawlNPCConfigCategory npcs)
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
                if (npcs.GetOrDefault(level.NPCIds[i]) == null) throw new InvalidOperationException($"NPC 配置不存在：{level.NPCIds[i]}");
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

        private static Unit CreateUnit(UnitComponent units, int configId, BrawlPracticeUnitKind kind, int teamId,
            long playerId, int point, int columns, int cellSize)
        {
            // ConfigId 在本模式中指向 BrawlHero/BrawlNPC，不调用原 MMO 的 Unit.Config()。
            Unit unit = units.AddChild<Unit, int>(configId);
            unit.UnitType = kind == BrawlPracticeUnitKind.NPC ? UnitType.NPC : UnitType.Player;
            unit.Position = new float3(point % columns * cellSize, 0, point / columns * cellSize);
            unit.Rotation = quaternion.identity;
            BrawlPracticeUnitComponent data = unit.AddComponent<BrawlPracticeUnitComponent>();
            data.Kind = kind;
            data.TeamId = teamId;
            data.OwnerPlayerId = playerId;
            return unit;
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
                Unit unit = (Unit)child;
                BrawlPracticeUnitComponent data = unit.GetComponent<BrawlPracticeUnitComponent>();
                BrawlPracticeUnitInfo info = BrawlPracticeUnitInfo.Create();
                info.UnitId = unit.Id;
                info.ConfigId = unit.ConfigId;
                info.Kind = (int)data.Kind;
                info.TeamId = data.TeamId;
                info.X = unit.Position.x;
                info.Y = unit.Position.y;
                info.Z = unit.Position.z;
                response.Units.Add(info);
            }
        }
    }
}
