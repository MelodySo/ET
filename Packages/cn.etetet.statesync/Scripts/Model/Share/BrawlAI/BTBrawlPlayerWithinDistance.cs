namespace ET
{
    public sealed class BTBrawlPlayerWithinDistance : BTCondition
    {
        [BTInput(typeof(Unit))] public string Unit = "Unit";
        public float Distance = 5;
    }
}