using System.Collections.Generic;
using ET.Server;
using Unity.Mathematics;
namespace ET.Test
{
    public class Statesync_BrawlLifecycle_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await ETTask.CompletedTask;
            Fiber fiber = context.Fiber;
            BrawlGlobalConfigCategory oldGlobal = fiber.GetSingleton<BrawlGlobalConfigCategory>();
            BrawlHeroConfigCategory oldHeroes = fiber.GetSingleton<BrawlHeroConfigCategory>();
            BrawlSkillConfigCategory oldSkills = fiber.GetSingleton<BrawlSkillConfigCategory>();
            BrawlNPCConfigCategory oldNPCs = fiber.GetSingleton<BrawlNPCConfigCategory>();
            BrawlRespawnPointConfigCategory oldPoints = fiber.GetSingleton<BrawlRespawnPointConfigCategory>();
            BrawlUnitComponentBlockConfigCategory oldBlocks = fiber.GetSingleton<BrawlUnitComponentBlockConfigCategory>();
            try
            {
                fiber.AddSingleton(new BrawlGlobalConfigCategory(new BrawlGlobalConfig(1, 12, 3, 2, 3, 3, 3)));
                fiber.AddSingleton(new BrawlHeroConfigCategory(new Dictionary<int, BrawlHeroConfig>
                    { [1] = new(1, 1, 1, 0, 99, 0, 1000, 100, 3, 0, 0.3f, 1.2f, 1) }));
                fiber.AddSingleton(new BrawlSkillConfigCategory(new Dictionary<int, BrawlSkillConfig>
                    { [99] = new(99, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0) }));
                fiber.AddSingleton(new BrawlNPCConfigCategory(new Dictionary<int, BrawlNPCConfig>
                {
                    [2] = new(2, 1, 0, 5000, 500, 2, new int[0], new int[0], 0.6f, 1, 0, 0, 0, 3, 5, 6, false),
                    [3] = new(3, 7, 3, 1, 0, 0, new[] { 2 }, new[] { 1 }, 1, 1, 1, 0, 0, 0, 0, 0, false),
                    [4] = new(4, 2, 0, 1, 0, 0, new int[0], new int[0], 1, 1, 1, 0, 0, 0.2f, 0, 40, true),
                }));
                fiber.AddSingleton(new BrawlRespawnPointConfigCategory(new Dictionary<int, BrawlRespawnPointConfig> { [1] = new(1) }));
                fiber.AddSingleton(new BrawlUnitComponentBlockConfigCategory(new Dictionary<int, BrawlUnitComponentBlockConfig> { [1] = new(1, true) }));
                using Scene scene = EntitySceneFactory.CreateScene(fiber.Root, IdGenerater.Instance.GenerateId(), SceneType.BrawlPractice, "LifecycleTest");
                UnitComponent units = scene.AddComponent<UnitComponent>();
                BrawlLifecycleRoomComponent life = scene.AddComponent<BrawlLifecycleRoomComponent>();
                Unit point = BrawlUnitFactory.Create(units, 3, BrawlPracticeUnitKind.NPC, BrawlFaction.Red, 0, new float3(1, 0, 1));
                Unit hero = BrawlUnitFactory.Create(units, 1, BrawlPracticeUnitKind.Player, BrawlFaction.Red, 10, point.Position, 0);
                long originalHeroId = hero.Id;
                life.RespawnPointIds = new[] { point.Id };
                life.Participants.Add(0, new BrawlParticipant { HeroId = 1, Kind = BrawlPracticeUnitKind.Player, Faction = BrawlFaction.Red, PlayerId = 10, RespawnPointId = point.Id, UnitId = hero.Id });
                if (!hero.GetComponent<StateComponent>().Has(StateId.Spawning) || hero.GetComponent<StateComponent>().Has(StateId.Invincible))
                    return Fail(100801, "Hero must start in spawning phase without consuming protection time.");
                life.SimulationTime = 1999; BrawlLifecycleHelper.Tick(scene);
                if (!hero.GetComponent<StateComponent>().Has(StateId.Spawning)) return Fail(100802, "Birth completed too early.");
                life.SimulationTime = 2000; BrawlLifecycleHelper.Tick(scene);
                if (hero.GetComponent<StateComponent>().Has(StateId.Spawning) || !hero.GetComponent<StateComponent>().Has(StateId.Invincible))
                    return Fail(100803, "Birth completion must enable hero protection.");
                life.SimulationTime = 5000;
                hero.GetComponent<StateComponent>().Expire(5000);
                hero.GetComponent<PropertyComponent>().Set(PropertyId.Health, 0);
                hero.GetComponent<PropertyComponent>().Set(PropertyId.SuperAmmo, 1.6);
                BrawlUnitLifeHelper.CheckDeath(hero);
                BrawlUnitLifeHelper.CheckDeath(hero);
                if (life.Notices.FindAll(n => n.UnitId == originalHeroId && n.Phase == BrawlLifePhase.Died).Count != 1)
                    return Fail(100804, "Death event must occur exactly once.");
                life.SimulationTime = 7999; BrawlLifecycleHelper.Tick(scene);
                if (units.Get(originalHeroId) == null) return Fail(100805, "Corpse removed before death duration ended.");
                life.SimulationTime = 8000; long rebornId = BrawlLifecycleHelper.Tick(scene);
                Unit reborn = units.Get(rebornId);
                if (units.Get(originalHeroId) != null || rebornId == originalHeroId || reborn == null ||
                    life.Participants[0].RespawnPointId != point.Id || math.any(reborn.Position != point.Position) ||
                    reborn.GetComponent<BrawlSpawnComponent>().CompleteAt != 10000)
                    return Fail(100806, "Hero must use a new entity at its stable respawn point after three seconds.");
                if (reborn.GetComponent<PropertyComponent>().GetDouble(PropertyId.SuperAmmo) != 1.6 ||
                    reborn.GetComponent<PropertyComponent>().GetInt(PropertyId.MaxSuperAmmo) != 2)
                    return Fail(100815, "Hero respawn did not restore fractional super ammo with configured capacity.");
                life.SimulationTime = 10000; BrawlLifecycleHelper.Tick(scene);
                if (!reborn.GetComponent<StateComponent>().Has(StateId.Invincible)) return Fail(100807, "Reborn hero has no protection.");
                life.SimulationTime = 13000;
                Unit npc = BrawlUnitFactory.Create(units, 2, BrawlPracticeUnitKind.NPC, BrawlFaction.NeutralHostile, 0, new float3(4, 0, 4));
                long npcId = npc.Id;
                npc.Position = new float3(5, 0, 5);
                npc.GetComponent<PropertyComponent>().Set(PropertyId.Health, 0);
                BrawlUnitLifeHelper.CheckDeath(npc);
                life.SimulationTime = 15999; BrawlLifecycleHelper.Tick(scene);
                if (units.Get(npcId) == null || life.NPCRespawns.Count != 0) return Fail(100808, "NPC respawn wait began before corpse removal.");
                life.SimulationTime = 16000; BrawlLifecycleHelper.Tick(scene);
                if (units.Get(npcId) != null || life.NPCRespawns.Count != 1 || life.NPCRespawns[0].ReadyAt != 21000)
                    return Fail(100809, "NPC delay must start at disposal: three plus five seconds.");
                life.SimulationTime = 20999; BrawlLifecycleHelper.Tick(scene);
                if (life.NPCRespawns.Count != 1) return Fail(100810, "NPC respawned early.");
                life.SimulationTime = 21000; BrawlLifecycleHelper.Tick(scene);
                Unit newNPC = null;
                foreach (Entity child in units.Children.Values) if (((Unit)child).ConfigId == 2) newNPC = (Unit)child;
                if (newNPC == null || newNPC.Id == npcId || math.any(newNPC.Position != new float3(4, 0, 4)) ||
                    newNPC.GetComponent<StateComponent>().Has(StateId.Spawning) || newNPC.GetComponent<StateComponent>().Has(StateId.Invincible) || life.NPCRespawns.Count != 0)
                    return Fail(100811, "NPC must respawn instantly at original position without hero protection.");
                Unit wall = BrawlUnitFactory.Create(units, 4, BrawlPracticeUnitKind.NPC, BrawlFaction.NeutralHostile, 0, new float3(8, 0, 8));
                long wallId = wall.Id;
                if (BrawlUnitMovementHelper.CanOccupy(units, 0, wall.Position.xz, 0.3f)) return Fail(100812, "Living wall did not block.");
                wall.GetComponent<PropertyComponent>().Set(PropertyId.Health, 0);
                BrawlUnitLifeHelper.CheckDeath(wall);
                if (!BrawlUnitMovementHelper.CanOccupy(units, 0, wall.Position.xz, 0.3f)) return Fail(100813, "Dead wall still blocks movement.");
                life.SimulationTime = 21200; BrawlLifecycleHelper.Tick(scene);
                if (units.Get(wallId) != null || life.NPCRespawns.Count != 0) return Fail(100814, "Zero delay means no respawn, not immediate respawn.");
                return ErrorCode.ERR_Success;
            }
            finally
            {
                fiber.AddSingleton(oldGlobal); fiber.AddSingleton(oldHeroes); fiber.AddSingleton(oldNPCs);
                fiber.AddSingleton(oldSkills);
                fiber.AddSingleton(oldPoints); fiber.AddSingleton(oldBlocks);
            }
        }

        private static int Fail(int code, string message) { Log.Console(message); return code; }
    }
}
