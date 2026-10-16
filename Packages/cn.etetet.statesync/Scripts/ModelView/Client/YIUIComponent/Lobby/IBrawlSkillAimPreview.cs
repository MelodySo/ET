namespace ET.Client
{
    /// <summary>客户端范围表现扩展点。不得在这里改变人物面向或发网络包。</summary>
    public interface IBrawlSkillAimPreview
    {
        void ShowOrUpdate(BrawlTrainingSession session, BrawlSkillAimPreviewData data);
        void Hide(BrawlTrainingSession session, BrawlSkillSlot slot);
    }
}
