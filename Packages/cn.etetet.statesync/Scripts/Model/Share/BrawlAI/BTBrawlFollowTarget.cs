namespace ET
{
    /// <summary>按当前连发的技能矩形纠正站位；超出射程或遇障碍时追击。</summary>
    public sealed class BTBrawlFollowTarget : BTAction
    {
        [BTInput(typeof(Unit))] public string Unit = "Unit";
        public float RangeMargin = 0.5f;
        public float SightWidth = 0.2f;
        public int RepathMilliseconds = 250;
        public int MaxSearchNodes = 10000;
    }
}
