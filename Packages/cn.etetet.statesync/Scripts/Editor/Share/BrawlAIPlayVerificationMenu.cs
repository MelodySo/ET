using UnityEditor;
using UnityEngine;
namespace ET
{
    public static class BrawlAIPlayVerificationMenu
    {
        [MenuItem("Brawl/Verification/RunAI")]
        public static void Run()
        {
            Debug.LogWarning("RunAI 是旧公牛 5 米追击且不攻击的验证，已停用。雪莉/柯尔特战斗 AI 请按 Book/Braw_AI开发交接.md 的最新清单手动验收。");
        }
    }
}
