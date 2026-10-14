using System;
using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;

namespace ET.Test
{
    public class Statesync_BrawlNPCBlock_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            BrawlUnitComponentBlockConfigCategory blocks = new(new Dictionary<int, BrawlUnitComponentBlockConfig>
            {
                [1] = new BrawlUnitComponentBlockConfig(1, true),
                [2] = new BrawlUnitComponentBlockConfig(2, false),
            });
            BrawlLevelConfig level = new(1, 1, 10, 10, new int[100], Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());
            using Scene scene = EntitySceneFactory.CreateScene(context.Fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "BlockTest");
            UnitComponent units = scene.AddComponent<UnitComponent>();
            Unit wall = units.AddChild<Unit, int>(101);
            wall.Position = new float3(2, 0, 3);
            BrawlNPCBlockHelper.Attach(wall, NPC(new[] { 1 }, new[] { 1 }, 0.3f, 0.5f), blocks);
            BrawlUnitComponentBlock block = wall.GetComponent<BrawlUnitComponentBlock>();
            if (block == null || !block.CanBeDestroyed || block.ConfigId != 1)
                return Fail(100201, "Destructible block was not initialized.");
            Unit barrel = units.AddChild<Unit, int>(201);
            barrel.Position = new float3(6, 0, 3);
            BrawlNPCBlockHelper.Attach(barrel, NPC(new[] { 1 }, new[] { 2 }, 0, 0), blocks);
            if (barrel.GetComponent<BrawlUnitComponentBlock>().CanBeDestroyed)
                return Fail(100202, "Indestructible block flag was lost.");
            Unit bystander = units.AddChild<Unit, int>(301);
            bystander.Position = new float3(1, 0, 3);
            BrawlNPCBlockHelper.Attach(bystander, NPC(Array.Empty<int>(), Array.Empty<int>(), 0, 0), blocks);
            if (bystander.GetComponent<BrawlUnitComponentBlock>() != null)
                return Fail(100203, "NPC without block configuration received a block.");
            foreach (BrawlPracticeUnitKind kind in new[] { BrawlPracticeUnitKind.Player, BrawlPracticeUnitKind.Bot })
            {
                Unit hero = units.AddChild<Unit, int>(1);
                hero.AddComponent<BrawlPracticeUnitComponent>().Kind = kind;
                BrawlMoveComponent move = hero.AddComponent<BrawlMoveComponent>();
                move.Radius = 0.3f;
                hero.Position = new float3(0, 0, 3);
                float2 stopped = BrawlUnitMovementHelper.Move(units, level, 1, hero.Id, hero.Position.xz, new float2(8, 0), move.Radius);
                if (math.abs(stopped.x - 1.9f) > 0.0001f)
                    return Fail(100204, "Hero did not stop at the offset wall.");
                if (BrawlUnitMovementHelper.CanOccupy(units, hero.Id, new float2(2.5f, 3), move.Radius))
                    return Fail(100205, "Spawn overlap was accepted.");
                float2 slid = BrawlUnitMovementHelper.Move(units, level, 1, hero.Id, stopped, new float2(0.2f, 0.1f), move.Radius);
                if (math.abs(slid.x - stopped.x) > 0.0001f || math.abs(slid.y - 3.1f) > 0.0001f)
                    return Fail(100206, "Diagonal wall sliding failed.");
            }
            wall.RemoveComponent<BrawlUnitComponentBlock>();
            float2 passed = BrawlUnitMovementHelper.Move(units, level, 1, 0, new float2(0, 3), new float2(4, 0), 0.3f);
            if (math.abs(passed.x - 4) > 0.0001f)
                return Fail(100207, "Removed block or overlapping heroes still obstruct movement.");
            float2 barrelStop = BrawlUnitMovementHelper.Move(units, level, 1, 0, new float2(4, 3), new float2(4, 0), 0.3f);
            if (math.abs(barrelStop.x - 5.4f) > 0.0001f)
                return Fail(100208, "Indestructible barrel should still block movement.");
            barrel.Dispose();
            if (!BrawlUnitMovementHelper.CanOccupy(units, 0, new float2(6, 3), 0.3f))
                return Fail(100209, "Disposed NPC still blocks movement.");
            if (!Rejected(NPC(new[] { 1 }, Array.Empty<int>(), 0, 0), blocks))
                return Fail(100210, "Mismatched arrays were accepted.");
            if (!Rejected(NPC(new[] { 99 }, new[] { 1 }, 0, 0), blocks))
                return Fail(100211, "Unknown component type was accepted.");
            if (!Rejected(NPC(new[] { 1, 1 }, new[] { 1, 2 }, 0, 0), blocks))
                return Fail(100212, "Duplicate block type was accepted.");
            if (!Rejected(NPC(new[] { 1 }, new[] { 999 }, 0, 0), blocks))
                return Fail(100213, "Missing block row was accepted.");
            if (!Rejected(NPC(new[] { 1 }, new[] { 1 }, -1, 0), blocks) ||
                !Rejected(NPC(new[] { 1 }, new[] { 1 }, 0, float.NaN), blocks))
                return Fail(100214, "Invalid geometry was accepted.");
            return ErrorCode.ERR_Success;
        }

        private static BrawlNPCConfig NPC(int[] types, int[] values, float width, float pivotX)
        {
            return new BrawlNPCConfig(101, 1, 0, 100, 0, 0, types, values, 0.6f, 1, width, pivotX, 0, 0, 0);
        }

        private static bool Rejected(BrawlNPCConfig npc, BrawlUnitComponentBlockConfigCategory blocks)
        {
            try { BrawlNPCBlockHelper.Validate(npc, blocks); }
            catch (InvalidOperationException) { return true; }
            return false;
        }

        private static int Fail(int code, string message)
        {
            Log.Console(message);
            return code;
        }
    }
}
