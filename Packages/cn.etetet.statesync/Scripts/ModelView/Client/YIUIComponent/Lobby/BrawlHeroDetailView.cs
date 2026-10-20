using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YIUIFramework;
using YooAsset;

namespace ET.Client
{
    /// <summary>由大厅持有的英雄详情控件与预览资源，不保存战斗状态。</summary>
    [EnableClass]
    public sealed class BrawlHeroDetailView
    {
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Role;
        public TextMeshProUGUI Rarity;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI Health;
        public TextMeshProUGUI MoveSpeed;
        public TextMeshProUGUI NormalName;
        public TextMeshProUGUI NormalDamage;
        public TextMeshProUGUI SuperName;
        public TextMeshProUGUI SuperDamage;
        public GameObject StatsOverlay;
        public TextMeshProUGUI StatsHealth;
        public TextMeshProUGUI StatsMoveSpeed;
        public BrawlHeroSkillDetailView NormalSkill;
        public BrawlHeroSkillDetailView SuperSkill;
        public RawImage PreviewImage;
        public TextMeshProUGUI PreviewStatus;
        public UI3DDisplay PreviewDisplay;
        public EntityRef<YIUI3DDisplayChild> PreviewDisplayChild;
        public GameObject PreviewStage;
        public AssetHandle PreviewHandle;
        public int PreviewVersion;
    }
}
