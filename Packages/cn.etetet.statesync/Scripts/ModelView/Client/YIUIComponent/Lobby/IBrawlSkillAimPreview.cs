namespace ET.Client
{
    /// <summary>客户端范围表现扩展点。本轮不提供 mesh 实现；不得在这里改变人物面向或发网络包。</summary>
    public interface IBrawlSkillAimPreview
    {
        void ShowOrUpdate(BrawlSkillAimPreviewData data);
        void Hide(BrawlSkillSlot slot);
    }
}
