namespace ET
{
    /// <summary>按值保存，不持有 Buff/装备 Entity；比例属性上的加法表示百分点相加。</summary>
    public struct PropertyModifier
    {
        public long Id;
        public long SourceInstanceId;
        public PropertyId Property;
        public double Value;
    }
}
