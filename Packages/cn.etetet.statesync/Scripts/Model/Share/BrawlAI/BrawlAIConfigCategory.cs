using System;
using System.Collections.Generic;
namespace ET
{
    [ConfigProcess(ConfigType.Code)]
    public sealed class BrawlAIConfigCategory : Singleton<BrawlAIConfigCategory>, IConfig
    {
        public Dictionary<int, BrawlAIRoot> Trees = new();
        public Dictionary<int, int> PracticeOpponents = new();

        public int GetPracticeOpponent(int heroId)
        {
            if (!PracticeOpponents.TryGetValue(heroId, out int opponent))
                throw new InvalidOperationException($"英雄 {heroId} 尚未配置练习赛对手。");
            return opponent;
        }
        public void ResolveRef()
        {
            foreach (var pair in Trees)
                if (pair.Key <= 0 || pair.Value == null) throw new InvalidOperationException("Brawl AI requires a hero and tree.");
            foreach (var pair in PracticeOpponents)
                if (!Trees.ContainsKey(pair.Key) || !Trees.ContainsKey(pair.Value) || pair.Key == pair.Value ||
                    !PracticeOpponents.TryGetValue(pair.Value, out int reverse) || reverse != pair.Key)
                    throw new InvalidOperationException("Brawl practice opponents must be reciprocal and have AI trees.");
        }
    }
}
