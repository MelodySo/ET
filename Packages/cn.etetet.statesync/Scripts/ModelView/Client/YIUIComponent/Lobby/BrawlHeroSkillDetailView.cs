using TMPro;

namespace ET.Client
{
    /// <summary>普通技能或超级技能的只读展示控件。</summary>
    [EnableClass]
    public sealed class BrawlHeroSkillDetailView
    {
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI Damage;
        public TextMeshProUGUI Range;
        public TextMeshProUGUI Reload;
    }
}
