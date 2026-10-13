namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public sealed class BrawlPracticeClientComponent : Entity, IAwake<BrawlTrainingSession>, IUpdate, IDestroy
    {
        public BrawlTrainingSession Session;
    }
}
