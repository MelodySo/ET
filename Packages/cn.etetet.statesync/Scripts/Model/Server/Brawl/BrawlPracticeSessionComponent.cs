namespace ET.Server
{
    /// <summary>连接持有练习房间，断线时子 Scene 与单位随 Entity 树释放。</summary>
    [ComponentOf(typeof(Session))]
    public sealed class BrawlPracticeSessionComponent : Entity, IAwake, IUpdate
    {
        public EntityRef<Scene> Room;
        public long EntryId;
        public int HeroId;
        public int LevelId;
        public int CellSize;
        public long MyUnitId;
        public long LastUpdateTime;
        public long AccumulatorMilliseconds;
        public long Tick;
        public long LastInputTime;
        public long LastInputSequence;
        public long InputWindowTime;
        public int InputWindowCount;
        public long InputRateWindowTime;
        public int InputRateCount;
    }
}
