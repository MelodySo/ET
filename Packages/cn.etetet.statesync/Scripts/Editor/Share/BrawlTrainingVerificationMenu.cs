using System;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace ET
{
    public static class BrawlTrainingVerificationMenu
    {
        [MenuItem("Brawl/Verification/RunTerrain")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("请先运行 StateSync 的 Init 场景，等待登录界面出现。");
            var manager = FiberManager.Instance;
            if (manager == null) throw new InvalidOperationException("ET 尚未初始化，请从 StateSync 的 Init 场景启动。");
            Fiber main = (Fiber)typeof(FiberManager).GetField("mainFiber", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
            Fiber fiber = main?.GetFiber("Client");
            if (fiber == null || fiber.IsDisposed) throw new InvalidOperationException("Client Fiber 尚未就绪，请等待登录界面出现。");
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ET.Client.BrawlTrainingVerification")).LastOrDefault(t => t != null);
            if (type == null) throw new InvalidOperationException("未加载训练场验证代码，请重新编译后进入 Play 模式。");
            fiber.ThreadSynchronizationContext.Post(() =>
            {
                if (!fiber.IsDisposed) ((ETTask)type.GetMethod("Run").Invoke(null, new object[] { fiber.Root })).Coroutine();
            });
        }
    }
}
