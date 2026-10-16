namespace ET.Client
{
    /// <summary>无状态适配器；所有可变资源归会话，避免热更新静态缓存。</summary>
    [EnableClass]
    public sealed class BrawlSkillAimPreview : IBrawlSkillAimPreview
    {
        public void ShowOrUpdate(BrawlTrainingSession session, BrawlSkillAimPreviewData data) => BrawlSkillAimVisualHelper.Show(session, data);
        public void Hide(BrawlTrainingSession session, BrawlSkillSlot slot) => BrawlSkillAimVisualHelper.Hide(session, slot);
    }
}
