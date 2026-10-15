using System;
using ET.Server;
using Unity.Mathematics;
namespace ET.Test
{
    public class Statesync_BrawlNPCBlock_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            BrawlLevelConfig level = new(1, 1, 10, 10, new int[100], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "BlockTest");
            UnitComponent units = scene.AddComponent<UnitComponent>();
            Unit wall = Obstacle(units, new float3(2, 0, 3), 0.3f, 0.5f, true, 40);
            Unit barrel = Obstacle(units, new float3(6, 0, 3), 0, 0, true, 8);
            Unit bystander = Obstacle(units, new float3(1, 0, 3), 0, 0, false, 40);
            if (!wall.GetComponent<BrawlPracticeUnitComponent>().BlocksMovement || bystander.GetComponent<BrawlPracticeUnitComponent>().BlocksMovement)
                return Fail(100201, "Movement flag must be independent of destroyable layer.");
            foreach (BrawlPracticeUnitKind kind in new[] { BrawlPracticeUnitKind.Player, BrawlPracticeUnitKind.Bot })
            {
                Unit hero = units.AddChild<Unit, int>(1);
                hero.AddComponent<BrawlPracticeUnitComponent>().Kind = kind;
                hero.Position = new float3(0, 0, 3);
                float2 stopped = BrawlUnitMovementHelper.Move(units, level, 1, hero.Id, hero.Position.xz, new float2(8, 0), 0.3f);
                if (math.abs(stopped.x - 1.9f) > 0.0001f) return Fail(100204, "Hero did not stop at the offset wall.");
                if (BrawlUnitMovementHelper.CanOccupy(units, hero.Id, new float2(2.5f, 3), 0.3f)) return Fail(100205, "Spawn overlap accepted.");
                float2 slid = BrawlUnitMovementHelper.Move(units, level, 1, hero.Id, stopped, new float2(0.2f, 0.1f), 0.3f);
                if (math.abs(slid.x - stopped.x) > 0.0001f || math.abs(slid.y - 3.1f) > 0.0001f) return Fail(100206, "Sliding failed.");
            }
            wall.AddComponent<StateComponent>().AddSource(StateId.Dead, -2);
            float2 passed = BrawlUnitMovementHelper.Move(units, level, 1, 0, new float2(0, 3), new float2(4, 0), 0.3f);
            if (math.abs(passed.x - 4) > 0.0001f) return Fail(100207, "Dead wall or non-blocking unit obstructed movement.");
            float2 stop = BrawlUnitMovementHelper.Move(units, level, 1, 0, new float2(4, 3), new float2(4, 0), 0.3f);
            if (math.abs(stop.x - 5.4f) > 0.0001f) return Fail(100208, "Circular obstacle diameter was not respected.");
            barrel.Dispose();
            if (!BrawlUnitMovementHelper.CanOccupy(units, 0, new float2(6, 3), 0.3f)) return Fail(100209, "Disposed obstacle remained.");
            if (!Rejected(NPC(-1, 0, true, 40)) || !Rejected(NPC(0, float.NaN, true, 40))) return Fail(100214, "Invalid geometry accepted.");
            if (!Rejected(NPC(0, 0, true, 64))) return Fail(100215, "Unknown layer accepted.");
            return ErrorCode.ERR_Success;
        }
        private static BrawlNPCConfig NPC(float width, float pivot, bool blocks, int layers) =>
            new(101, 2, 0, 100, 0, 0, Array.Empty<int>(), Array.Empty<int>(), 0.6f, 1, width, pivot, 0, 0, 0, layers, blocks);
        private static Unit Obstacle(UnitComponent units, float3 position, float width, float pivot, bool blocks, int layers)
        {
            Unit unit = units.AddChild<Unit, int>(101);
            unit.Position = position;
            unit.AddComponent<BrawlPracticeUnitComponent>();
            BrawlNPCBlockHelper.Attach(unit, NPC(width, pivot, blocks, layers));
            return unit;
        }
        private static bool Rejected(BrawlNPCConfig config)
        {
            try { BrawlNPCBlockHelper.Validate(config); }
            catch (InvalidOperationException) { return true; }
            return false;
        }
        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
