namespace ET
{
    public static class BrawlSkillEmitterHelper
    {
        public static IBrawlSkillEmitter Create(BrawlEmitterType type)
        {
            // 新发射器实现同一接口；尚未实现的类型明确不接受释放。
            return type == BrawlEmitterType.Shotgun ? new BrawlShotgunEmitter() : null;
        }
    }
}
