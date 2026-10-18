using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;
namespace ET.Test
{
    public sealed class Statesync_BrawlAI_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            Fiber fiber = context.Fiber;
            BrawlAIConfigCategory previousAI = fiber.GetSingleton<BrawlAIConfigCategory>();
            BrawlLevelConfigCategory previousLevels = fiber.GetSingleton<BrawlLevelConfigCategory>();
            try
            {
                BrawlAIRoot tree = new() { Children = new() { new BTBrawlChase() } };
                fiber.AddSingleton(new BrawlAIConfigCategory { Trees = new() { [201] = tree } });
                int[] cells = new int[121];
                BrawlLevelConfig level = new(98701, 102, 11, 11, cells, Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
                fiber.AddSingleton(new BrawlLevelConfigCategory(new Dictionary<int, BrawlLevelConfig> { [level.Id] = level }));
                using Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "BrawlAITest");
                UnitComponent units = scene.AddComponent<UnitComponent>();
                Unit bot = Create(units, 201, new float3(0, 0, 0));
                BrawlAIComponent ai = bot.AddComponent<BrawlAIComponent>();
                Unit player = Create(units, 101, new float3(4.99f, 0, 0));
                BrawlMoveComponent move = bot.GetComponent<BrawlMoveComponent>();
                BrawlAIHelper.Tick(bot, level.Id, 1, player.Id, 0);
                if (!ai.Chasing || move.DesiredDirection.x <= 0) return Fail(109001, "Inside 5m must Chase.");
                player.Position = new float3(5, 0, 0);
                BrawlAIHelper.Tick(bot, level.Id, 1, player.Id, 50);
                if (!ai.Chasing) return Fail(109002, "Exactly 5m must Chase.");
                player.Position = new float3(5.01f, 0, 0);
                BrawlAIHelper.Tick(bot, level.Id, 1, player.Id, 100);
                if (!ai.Chasing || math.lengthsq(move.DesiredDirection) == 0) return Fail(109003, "Chase must continue beyond the removed 5m boundary.");
                player.Position = new float3(2, 0, 0);
                player.GetComponent<StateComponent>().AddSource(StateId.Dead, 1);
                BrawlAIHelper.Tick(bot, level.Id, 1, player.Id, 150);
                if (ai.Chasing) return Fail(109004, "Dead target must Idle.");
                player.GetComponent<StateComponent>().RemoveSource(StateId.Dead, 1);
                bot.GetComponent<StateComponent>().AddSource(StateId.Spawning, 1);
                BrawlAIHelper.Tick(bot, level.Id, 1, player.Id, 200);
                if (ai.Chasing) return Fail(109005, "Spawning Bot must Idle.");
                bot.GetComponent<StateComponent>().RemoveSource(StateId.Spawning, 1);
                // Three-cell wall between two heroes: path must go around, not interpolate through it.
                for (int row = 4; row <= 6; ++row) cells[row * 11 + 5] = (int)BrawlTileType.Wall;
                bot.Position = new float3(-2, 0, 0);
                BrawlAIHelper.Idle(bot);
                for (int tick = 5; tick < 100; ++tick)
                {
                    BrawlAIHelper.Tick(bot, level.Id, 1, player.Id, tick * 50);
                    float2 next = BrawlUnitMovementHelper.Move(units, level, 1, bot.Id, bot.Position.xz, move.DesiredDirection * move.Speed * .05f, move.Radius);
                    bot.Position = new float3(next.x, 0, next.y);
                    if (!BrawlTerrainMovementHelper.CanOccupy(level, 1, next, move.Radius)) return Fail(109006, "Wall penetration.");
                }
                if (math.distance(bot.Position.xz, player.Position.xz) > .15f) return Fail(109007, "Bot failed to chase around wall.");
                // NPC blocking uses full dimensions and current life state, without cached stale occupancy.
                Unit obstacle = Create(units, 1, new float3(3, 0, 0));
                BrawlPracticeUnitComponent block = obstacle.GetComponent<BrawlPracticeUnitComponent>();
                block.BlocksMovement = true; block.CollisionLength = 1; block.CollisionWidth = 1;
                player.Position = new float3(4, 0, 0);
                BrawlAIHelper.Idle(bot);
                for (int tick = 100; tick < 155; ++tick)
                {
                    BrawlAIHelper.Tick(bot, level.Id, 1, player.Id, tick * 50);
                    float2 next = BrawlUnitMovementHelper.Move(units, level, 1, bot.Id, bot.Position.xz, move.DesiredDirection * move.Speed * .05f, move.Radius);
                    bot.Position = new float3(next.x, 0, next.y);
                    if (!BrawlUnitMovementHelper.CanOccupy(units, bot.Id, next, move.Radius)) return Fail(109008, "NPC penetration.");
                }
                if (math.distance(bot.Position.xz, player.Position.xz) > .15f) return Fail(109009, "NPC detour failed.");
                // A full water barrier is unreachable; do not reuse an old path or push through it.
                for (int row = 0; row < 11; ++row) cells[row * 11 + 5] = (int)BrawlTileType.Water;
                bot.Position = new float3(-2, 0, 0); player.Position = new float3(2, 0, 0);
                BrawlAIHelper.Idle(bot); BrawlAIHelper.Tick(bot, level.Id, 1, player.Id, 8000);
                if (ai.Path.Count != 0 || math.lengthsq(move.DesiredDirection) != 0) return Fail(109010, "Unreachable target must stay stopped.");
                if (BrawlPathfindingHelper.CanTraverse(units, bot.Id, level, 1, bot.Position.xz, new float2(-10, 0), move.Radius))
                    return Fail(109011, "Map boundary ignored.");
                BrawlAIHelper.Tick(bot, level.Id, 1, long.MaxValue, 8050);
                if (ai.Chasing) return Fail(109012, "Missing target must Idle.");
                // Radius-sensitive passage: small hero passes, oversized hero cannot cross the same gap.
                Array.Clear(cells, 0, cells.Length);
                for (int row = 0; row < 11; ++row) if (row != 5) cells[row * 11 + 5] = (int)BrawlTileType.Wall;
                move.Radius = .3f;
                if (!BrawlPathfindingHelper.Find(units, bot, level, 1, player.Position.xz, 10000, ai.Path)) return Fail(109014, "Small radius should pass one-cell gap.");
                move.Radius = .6f;
                if (BrawlPathfindingHelper.Find(units, bot, level, 1, player.Position.xz, 10000, ai.Path)) return Fail(109015, "Large radius must not pass one-cell gap.");
                move.Radius = .3f;
                int lifecycleResult = VerifyRespawn(fiber);
                if (lifecycleResult != 0) return lifecycleResult;
                scene.Dispose();
                if (!ai.IsDisposed || !bot.IsDisposed) return Fail(109013, "Room disposal leaked AI.");
                Log.Console("BRAWL_AI_TEST_PASS: unrestricted chase, wall/NPC detours, water unreachable, bounds, spawn/dead target, disposal.");
                return ErrorCode.ERR_Success;
            }
            finally { fiber.AddSingleton(previousAI); fiber.AddSingleton(previousLevels); }
        }
        private static int VerifyRespawn(Fiber fiber)
        {
            BrawlGlobalConfigCategory oldGlobal = fiber.GetSingleton<BrawlGlobalConfigCategory>();
            BrawlHeroConfigCategory oldHeroes = fiber.GetSingleton<BrawlHeroConfigCategory>();
            BrawlNPCConfigCategory oldNPCs = fiber.GetSingleton<BrawlNPCConfigCategory>();
            BrawlRespawnPointConfigCategory oldPoints = fiber.GetSingleton<BrawlRespawnPointConfigCategory>();
            try
            {
                fiber.AddSingleton(new BrawlGlobalConfigCategory(new BrawlGlobalConfig(1, 12, 3, 2, 3, 3, 3)));
                fiber.AddSingleton(new BrawlHeroConfigCategory(new Dictionary<int, BrawlHeroConfig>
                    { [201] = new(201, 1, 1, 0, 0, 0, 1000, 100, 3, 0, .3f, 1.2f, 1) }));
                fiber.AddSingleton(new BrawlNPCConfigCategory(new Dictionary<int, BrawlNPCConfig>
                    { [3] = new(3, 7, 3, 1, 0, 0, Array.Empty<int>(), Array.Empty<int>(), 1, 1, 1, 0, 0, 0, 0, 0, false) }));
                fiber.AddSingleton(new BrawlRespawnPointConfigCategory(new Dictionary<int, BrawlRespawnPointConfig>()));
                using Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "AIRespawnTest");
                UnitComponent units = scene.AddComponent<UnitComponent>();
                BrawlLifecycleRoomComponent life = scene.AddComponent<BrawlLifecycleRoomComponent>();
                Unit point = BrawlUnitFactory.Create(units, 3, BrawlPracticeUnitKind.NPC, BrawlFaction.Blue, 0, float3.zero);
                Unit bot = BrawlUnitFactory.Create(units, 201, BrawlPracticeUnitKind.Bot, BrawlFaction.Blue, 0, float3.zero, 0);
                BrawlAIComponent oldAI = bot.GetComponent<BrawlAIComponent>();
                if (oldAI == null) return Fail(109016, "Factory did not attach AI to Bull Bot.");
                life.Participants.Add(0, new BrawlParticipant { HeroId = 201, Kind = BrawlPracticeUnitKind.Bot,
                    Faction = BrawlFaction.Blue, PlayerId = 0, RespawnPointId = point.Id, UnitId = bot.Id });
                life.SimulationTime = 2000; BrawlLifecycleHelper.Tick(scene);
                oldAI.Path.Add(new float2(1, 1));
                bot.GetComponent<BrawlMoveComponent>().DesiredDirection = new float2(1, 0);
                bot.GetComponent<PropertyComponent>().Set(PropertyId.Health, 0);
                BrawlUnitLifeHelper.CheckDeath(bot);
                BrawlAIHelper.Tick(bot, 98701, 1, 0, 2000);
                if (oldAI.Path.Count != 0 || math.lengthsq(bot.GetComponent<BrawlMoveComponent>().DesiredDirection) != 0)
                    return Fail(109017, "Bot death must clear path and movement.");
                life.SimulationTime = 5000; BrawlLifecycleHelper.Tick(scene);
                Unit reborn = units.Get(life.Participants[0].UnitId);
                BrawlAIComponent newAI = reborn.GetComponent<BrawlAIComponent>();
                if (!oldAI.IsDisposed || newAI == null || newAI.Path.Count != 0 || !reborn.GetComponent<StateComponent>().Has(StateId.Spawning))
                    return Fail(109018, "Respawn must create fresh AI and respect spawning gate.");
                return 0;
            }
            finally { fiber.AddSingleton(oldGlobal); fiber.AddSingleton(oldHeroes); fiber.AddSingleton(oldNPCs); fiber.AddSingleton(oldPoints); }
        }
        private static Unit Create(UnitComponent units, int configId, float3 position)
        {
            Unit unit = units.AddChild<Unit, int>(configId); unit.Position = position;
            unit.AddComponent<BrawlPracticeUnitComponent>(); unit.AddComponent<StateComponent>();
            BrawlMoveComponent move = unit.AddComponent<BrawlMoveComponent>(); move.Radius = .3f; move.Speed = 3;
            return unit;
        }
        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
