using System;
using ET.Client;
using ET.Server;

namespace ET.Test
{
    public static class TestHelper
    {
        private const string RobotTestPassword = "RobotTest-Password1";
        public static async ETTask<Fiber> CreateRobot(Fiber fiber, string robotName)
        {
            string routerManagerAddress = GetRouterManagerAddress(fiber);
            Fiber robot = await fiber.CreateFiber(IdGenerater.Instance.GenerateId(), SceneType.Client, robotName);
            Scene root = robot.Root;
            EntityRef<Scene> rootRef = root;
            string account = $"test_{unchecked((ulong)robotName.GetLongHashCode()):x16}";
            await LoginHelper.Login(root, routerManagerAddress, account, RobotTestPassword);
            root = rootRef;
            await EnterMapHelper.EnterMapAsync(root);
            return robot;
        }

        public static string GetRouterManagerAddress(Fiber fiber)
        {
            Fiber routerManagerFiber = fiber.GetFiber("RouterManager");
            if (routerManagerFiber == null)
            {
                throw new Exception($"RouterManager fiber not found under fiber: {fiber?.Name}");
            }

            string address = routerManagerFiber.Root.GetComponent<RouterManagerAddressComponent>()?.Address;
            if (string.IsNullOrWhiteSpace(address))
            {
                throw new Exception("RouterManagerAddressComponent is missing on RouterManager scene.");
            }

            return address;
        }
        
        public static Fiber GetMap(Fiber testFiber, Fiber robotFiber)
        {
            Scene clientScene = robotFiber.Root;
            string mapName = clientScene.CurrentScene().Name;
            Fiber map = testFiber.GetFiber("MapManager").GetFiber(mapName);
            return map;
        }

        public static Unit GetServerUnit(Fiber testFiber, Fiber robotFiber)
        {
            Scene clientScene = robotFiber.Root;
            string mapName = clientScene.CurrentScene().Name;
            Fiber map = testFiber.GetFiber("MapManager").GetFiber(mapName);
            ET.Client.PlayerComponent player = clientScene.GetComponent<ET.Client.PlayerComponent>();
            Unit unit = map.Root.GetComponent<UnitComponent>().Get(player.MyId);
            return unit;
        }
    }
}
