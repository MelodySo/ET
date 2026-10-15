using System;
namespace ET
{
    [ConfigProcess(ConfigType.Code)]
    public sealed class BrawlAIConfigCategory : Singleton<BrawlAIConfigCategory>, IConfig
    {
        public int HeroId;
        public BrawlAIRoot Tree;
        public void ResolveRef()
        {
            if (HeroId <= 0 || Tree == null) throw new InvalidOperationException("Brawl AI requires a hero and tree.");
        }
    }
}