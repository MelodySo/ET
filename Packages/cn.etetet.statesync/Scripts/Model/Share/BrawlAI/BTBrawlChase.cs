namespace ET
{
    public sealed class BTBrawlChase : BTAction
    {
        [BTInput(typeof(Unit))] public string Unit = "Unit";
        public int RepathMilliseconds = 250;
        public int MaxSearchNodes = 10000;
    }
}