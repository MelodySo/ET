using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
namespace ET
{
    public static class BrawlAIPlayVerificationMenu
    {
        [MenuItem("Brawl/Verification/RunAI")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play first.");
            FiberManager manager = FiberManager.Instance;
            Fiber main = (Fiber)typeof(FiberManager).GetField("mainFiber", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(manager);
            Fiber fiber = main?.GetFiber("Client");
            if (fiber == null || fiber.IsDisposed) throw new InvalidOperationException("Client Fiber is not ready.");
            Type type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("ET.Client.BrawlAIPlayVerification")).LastOrDefault(t => t != null);
            if (type == null) throw new InvalidOperationException("Verification hotfix assembly is not loaded.");
            fiber.ThreadSynchronizationContext.Post(() =>
            {
                if (!fiber.IsDisposed) ((ETTask)type.GetMethod("Run").Invoke(null, new object[] { fiber.Root })).Coroutine();
            });
        }
    }
}