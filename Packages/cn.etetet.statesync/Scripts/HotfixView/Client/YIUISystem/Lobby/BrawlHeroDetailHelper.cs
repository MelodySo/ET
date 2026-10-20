using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ET.Client.BrawlHeroUIHelper;

namespace ET.Client
{
    /// <summary>英雄详情只展示配置，不接入升级、购买或战斗数值计算。</summary>
    public static class BrawlHeroDetailHelper
    {
        private const int DisplayLevel = 1;
        private const int MaxDisplayLevel = 11;

        public static void Create(LobbyPanelComponent self, Transform parent, Action back, Action trial)
        {
            BrawlHeroDetailView view = new();
            self.HeroDetails = view;
            RectTransform root = Panel("HeroDetail", parent, new Color(0.025f, 0.29f, 0.78f));
            Stretch(root, Vector2.zero, new Vector2(1, 0.88f));
            self.HeroDetailView = root.gameObject;

            // 用纯色块复现参考图的三列结构，不添加不存在的奖杯、货币或培养入口。
            Box(root, "ModelBackdrop", new Color(0.025f, 0.43f, 0.86f), 0.31f, 0, 0.69f, 1);
            Box(root, "ModelHighlight", new Color(0.04f, 0.52f, 0.91f), 0.36f, 0.09f, 0.64f, 0.96f);
            RectTransform portrait = Rect("ModelPreviewArea", root);
            Stretch(portrait, new Vector2(0.30f, 0.07f), new Vector2(0.70f, 0.96f));
            view.PreviewImage = Rect("ModelPreview", portrait).gameObject.AddComponent<RawImage>();
            Stretch(view.PreviewImage.rectTransform, Vector2.zero, Vector2.one);
            view.PreviewImage.raycastTarget = true;
            view.PreviewImage.enabled = false;
            AspectRatioFitter fit = view.PreviewImage.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = BrawlHeroPreviewHelper.AspectRatio;
            view.PreviewStatus = Text(self, portrait, "ModelStatus", "", 24, 0.04f, 0.38f, 0.96f, 0.62f);
            Text(self, portrait, "RotationHint", "左右拖动旋转", 19, 0.10f, 0, 0.90f, 0.05f).color = new Color(0.70f, 0.90f, 1);

            RectTransform banner = Box(root, "NameBanner", new Color(0.56f, 0.85f, 0.96f), 0, 0.66f, 0.29f, 0.90f);
            view.Name = Text(self, banner, "HeroName", "", 58, 0.13f, 0.38f, 0.96f, 0.95f, TextAlignmentOptions.Left);
            view.Name.color = new Color(0.035f, 0.08f, 0.16f);
            view.Role = Text(self, banner, "Role", "", 30, 0.15f, 0.07f, 0.95f, 0.37f, TextAlignmentOptions.Left);
            view.Role.color = new Color(0.035f, 0.08f, 0.16f);
            view.Rarity = Text(self, root, "Rarity", "", 25, 0.04f, 0.60f, 0.28f, 0.65f, TextAlignmentOptions.Right);
            view.Rarity.color = new Color(0.60f, 0.96f, 1);
            view.Description = Text(self, root, "Description", "", 28, 0.045f, 0.29f, 0.285f, 0.56f, TextAlignmentOptions.TopLeft, false);
            self.HeroDetailText = Text(self, root, "Status", "", 19, 0.04f, 0.16f, 0.29f, 0.27f, TextAlignmentOptions.BottomLeft, false);
            self.HeroDetailText.color = new Color(1, 0.85f, 0.55f);

            Button backButton = Button(self, root, "Back", "返回英雄列表", back);
            Stretch((RectTransform)backButton.transform, new Vector2(0.04f, 0.04f), new Vector2(0.23f, 0.14f));
            backButton.image.color = new Color(0.25f, 0.33f, 0.49f);
            Button trialButton = Button(self, root, "Trial", "试用", trial);
            Stretch((RectTransform)trialButton.transform, new Vector2(0.245f, 0.04f), new Vector2(0.345f, 0.14f));
            trialButton.image.color = new Color(0.03f, 0.60f, 1);

            RectTransform attributes = Box(root, "Attributes", new Color(0.025f, 0.14f, 0.37f), 0.71f, 0.23f, 0.975f, 0.83f);
            RectTransform level = Box(attributes, "PowerLevel", new Color(0.025f, 0.035f, 0.10f), 0, 0.80f, 1, 1);
            RectTransform badge = Box(level, "LevelBadge", new Color(0.80f, 0.06f, 0.67f), 0.025f, 0.12f, 0.18f, 0.94f);
            Text(self, badge, "Level", DisplayLevel.ToString(), 48, 0.06f, 0.08f, 0.94f, 0.92f);
            Text(self, level, "Title", "战力等级", 34, 0.22f, 0.39f, 0.72f, 0.94f, TextAlignmentOptions.Left);
            Text(self, level, "MaxLevel", $"最高{MaxDisplayLevel}级", 21, 0.70f, 0.39f, 0.98f, 0.94f, TextAlignmentOptions.Right).color = new Color(1, 0.56f, 0.85f);
            for (int i = 0; i < MaxDisplayLevel; ++i)
            {
                float x = 0.22f + i * 0.068f;
                Box(level, "LevelStep_" + i, i < DisplayLevel ? new Color(0.91f, 0.16f, 0.74f) : new Color(0.15f, 0.19f, 0.28f), x, 0.14f, x + 0.058f, 0.31f);
            }

            Action openStats = () => view.StatsOverlay.SetActive(true);
            RectTransform health = StatRow(self, attributes, "Health", "生命值", "生命", new Color(0.21f, 0.88f, 0.20f), 0.54f, 0.77f, openStats);
            view.Health = Text(self, health, "Value", "", 35, 0.23f, 0.26f, 0.97f, 0.65f, TextAlignmentOptions.Left);
            view.Health.richText = true;
            view.Health.enableWordWrapping = false;
            view.MoveSpeed = Text(self, health, "MoveSpeed", "", 19, 0.23f, 0.03f, 0.97f, 0.27f, TextAlignmentOptions.Left, false);
            RectTransform normal = StatRow(self, attributes, "NormalAttack", "普通攻击", "普攻", new Color(1, 0.28f, 0.34f), 0.29f, 0.52f, openStats);
            view.NormalName = Text(self, normal, "SkillName", "", 23, 0.23f, 0.36f, 0.97f, 0.65f, TextAlignmentOptions.Left);
            view.NormalDamage = Text(self, normal, "Damage", "", 30, 0.23f, 0.02f, 0.97f, 0.36f, TextAlignmentOptions.Left);
            view.NormalDamage.richText = true;
            view.NormalDamage.enableWordWrapping = false;
            RectTransform super = StatRow(self, attributes, "SuperSkill", "超级技能", "大招", new Color(1, 0.79f, 0.10f), 0.04f, 0.27f, openStats);
            view.SuperName = Text(self, super, "SkillName", "", 23, 0.23f, 0.36f, 0.97f, 0.65f, TextAlignmentOptions.Left);
            view.SuperDamage = Text(self, super, "Damage", "", 30, 0.23f, 0.02f, 0.97f, 0.36f, TextAlignmentOptions.Left);
            view.SuperDamage.richText = true;
            view.SuperDamage.enableWordWrapping = false;
            Button details = Button(self, root, "SkillDetails", "属性与技能详情", openStats);
            details.image.color = new Color(0.06f, 0.52f, 0.93f);
            Stretch((RectTransform)details.transform, new Vector2(0.71f, 0.12f), new Vector2(0.975f, 0.21f));
            CreateStats(self, root, view);
        }

        private static void CreateStats(LobbyPanelComponent self, Transform parent, BrawlHeroDetailView view)
        {
            RectTransform overlay = Box(parent, "StatsOverlay", new Color(0, 0, 0, 0.78f), 0, 0, 1, 1);
            view.StatsOverlay = overlay.gameObject;
            RectTransform card = Box(overlay, "StatsCard", new Color(0.045f, 0.32f, 0.88f), 0.15f, 0.035f, 0.85f, 0.98f);
            RectTransform header = Box(card, "Header", new Color(0.10f, 0.52f, 0.94f), 0, 0.88f, 1, 1);
            Text(self, header, "Title", $"战力等级 {DisplayLevel} · 属性与技能", 38, 0.05f, 0.08f, 0.88f, 0.92f);
            Button close = Button(self, header, "Close", "关闭", () => view.StatsOverlay.SetActive(false));
            close.image.color = new Color(0.91f, 0.19f, 0.24f);
            Stretch((RectTransform)close.transform, new Vector2(0.89f, 0.12f), new Vector2(0.985f, 0.88f));
            RectTransform health = Box(card, "HealthAndSpeed", new Color(0.02f, 0.54f, 0.91f), 0.035f, 0.70f, 0.49f, 0.85f);
            Text(self, health, "HealthTitle", "生命值", 25, 0.03f, 0.55f, 0.53f, 0.96f, TextAlignmentOptions.Left).color = new Color(0.64f, 1, 0.52f);
            view.StatsHealth = ValueBox(self, health, "HealthValue", 0.03f, 0.08f, 0.53f, 0.56f);
            view.StatsHealth.richText = true;
            Text(self, health, "SpeedTitle", "移动速度", 25, 0.57f, 0.55f, 0.97f, 0.96f, TextAlignmentOptions.Left).color = new Color(0.64f, 1, 0.52f);
            view.StatsMoveSpeed = ValueBox(self, health, "SpeedValue", 0.57f, 0.08f, 0.97f, 0.56f);
            view.NormalSkill = SkillCard(self, card, "NormalSkill", "普通攻击", "普攻", new Color(1, 0.28f, 0.34f), 0.035f, 0.49f, true);
            view.SuperSkill = SkillCard(self, card, "SuperSkill", "超级技能", "大招", new Color(1, 0.79f, 0.10f), 0.51f, 0.965f, false);
            Text(self, card, "GrowthHint", "绿色数值为每升一级的增量", 20, 0.04f, 0.005f, 0.96f, 0.05f, TextAlignmentOptions.Center, false).color = new Color(0.65f, 1, 0.62f);
            view.StatsOverlay.SetActive(false);
        }

        private static BrawlHeroSkillDetailView SkillCard(LobbyPanelComponent self, Transform parent, string name, string title,
            string icon, Color color, float left, float right, bool normal)
        {
            RectTransform card = Box(parent, name, new Color(0.02f, 0.53f, 0.91f), left, 0.06f, right, 0.66f);
            RectTransform symbol = Box(card, "Icon", color, 0.025f, 0.81f, 0.15f, 0.98f);
            Text(self, symbol, "Label", icon, 25, 0.02f, 0.08f, 0.98f, 0.92f);
            Text(self, card, "Title", title, 26, 0.18f, 0.91f, 0.97f, 0.99f, TextAlignmentOptions.Left).color = color;
            BrawlHeroSkillDetailView view = new();
            view.Name = Text(self, card, "Name", "", 33, 0.18f, 0.78f, 0.97f, 0.91f, TextAlignmentOptions.Left);
            view.Description = Text(self, card, "Description", "", 26, 0.035f, 0.43f, 0.965f, 0.75f, TextAlignmentOptions.TopLeft, false);
            Text(self, card, "DamageTitle", "伤害（每轮）", 23, 0.035f, 0.35f, 0.55f, 0.43f, TextAlignmentOptions.Left).color = color;
            view.Damage = ValueBox(self, card, "DamageValue", 0.035f, 0.23f, 0.55f, 0.35f);
            view.Damage.richText = true;
            Text(self, card, "RangeTitle", "射程", 23, 0.59f, 0.35f, 0.965f, 0.43f, TextAlignmentOptions.Left).color = color;
            view.Range = ValueBox(self, card, "RangeValue", 0.59f, 0.23f, 0.965f, 0.35f);
            if (normal)
            {
                Text(self, card, "ReloadTitle", "装弹时间", 23, 0.035f, 0.15f, 0.55f, 0.23f, TextAlignmentOptions.Left).color = color;
                view.Reload = ValueBox(self, card, "ReloadValue", 0.035f, 0.03f, 0.55f, 0.15f);
            }
            return view;
        }

        public static void Show(LobbyPanelComponent self, BrawlHeroConfig hero)
        {
            BrawlHeroDetailView view = self.HeroDetails;
            view.StatsOverlay.SetActive(false);
            self.HeroDetailText.text = "";
            view.Name.text = hero.Name;
            view.Role.text = hero.Role switch { 1 => "输出", 2 => "坦克", 3 => "辅助", 4 => "射手", 5 => "投弹", 6 => "控场", 7 => "刺客", _ => "定位待定" };
            view.Rarity.text = hero.Rarity switch { 1 => "普通", 2 => "稀有", 3 => "超稀有", 4 => "史诗", 5 => "神话", 6 => "传奇", 7 => "超凡", _ => "品质待定" };
            view.Description.text = string.IsNullOrWhiteSpace(hero.Description) ? "暂无英雄描述" : hero.Description;
            string speed = hero.MoveSpeedTier switch { 1 => "慢", 2 => "中", 3 => "快", 4 => "非常快", _ => "暂无数据" };
            view.Health.text = hero.BaseHp > 0 ? Growth(hero.BaseHp, hero.HpGrowthPerLevel) : "暂无数据";
            view.MoveSpeed.text = "移动速度：" + speed;
            view.StatsHealth.text = view.Health.text;
            view.StatsMoveSpeed.text = speed;
            BrawlSkillConfigCategory skills = self.Fiber().GetSingleton<BrawlSkillConfigCategory>();
            BrawlProjectileConfigCategory projectiles = self.Fiber().GetSingleton<BrawlProjectileConfigCategory>();
            // 普通技能就是 NormalAttackSkillId，本页不读取 SpecialSkillId。
            BrawlSkillConfig normal = skills.GetOrDefault(hero.NormalAttackSkillId);
            BrawlSkillConfig super = skills.GetOrDefault(hero.SuperSkillId);
            FillSkill(view.NormalSkill, normal, projectiles);
            FillSkill(view.SuperSkill, super, projectiles);
            view.NormalName.text = view.NormalSkill.Name.text;
            view.NormalDamage.text = view.NormalSkill.Damage.text;
            view.SuperName.text = view.SuperSkill.Name.text;
            view.SuperDamage.text = view.SuperSkill.Damage.text;
            BrawlHeroPreviewHelper.Show(self, hero.ModelPath).Coroutine();
        }

        public static void Hide(LobbyPanelComponent self)
        {
            // Create 之前 / Destroy 之后可以没有详情视图。
            if (self.HeroDetails == null) return;
            if (self.HeroDetails.StatsOverlay != null) self.HeroDetails.StatsOverlay.SetActive(false);
            BrawlHeroPreviewHelper.Release(self.HeroDetails);
        }

        private static void FillSkill(BrawlHeroSkillDetailView view, BrawlSkillConfig skill, BrawlProjectileConfigCategory projectiles)
        {
            view.Name.text = string.IsNullOrWhiteSpace(skill?.Name) ? "暂未配置" : skill.Name;
            view.Description.text = string.IsNullOrWhiteSpace(skill?.Description) ? "暂无技能描述" : skill.Description;
            BrawlProjectileConfig projectile = skill == null ? null : projectiles.GetOrDefault(skill.ProjectileId);
            view.Damage.text = projectile == null || skill.ProjectilesPerVolley <= 0 ? "暂无数据" :
                (skill.ProjectilesPerVolley > 1 ? $"{skill.ProjectilesPerVolley} × " : "") + Growth(projectile.Value, projectile.ValueGrowthPerLevel);
            view.Range.text = skill != null && skill.SkillRangeParam1 > 0 ? Number(skill.SkillRangeParam1) + " 米" : "暂无数据";
            if (view.Reload != null)
                view.Reload.text = skill == null ? "暂无数据" : skill.ReloadTime > 0 ? Number(skill.ReloadTime) + " 秒 / 发" : "无需装弹";
        }

        private static string Growth(int value, int growth) => $"{value} <color=#45FF35>{(growth >= 0 ? "+" : "")}{growth}</color>";
        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static RectTransform StatRow(LobbyPanelComponent self, Transform parent, string name, string title,
            string icon, Color color, float bottom, float top, Action action)
        {
            Button button = Button(self, parent, name, "", action);
            RectTransform rect = (RectTransform)button.transform;
            Stretch(rect, new Vector2(0.04f, bottom), new Vector2(0.96f, top));
            button.image.color = new Color(0.015f, 0.075f, 0.20f);
            RectTransform symbol = Box(rect, "Icon", color, 0.02f, 0.10f, 0.19f, 0.90f);
            Text(self, symbol, "Label", icon, 25, 0.03f, 0.05f, 0.97f, 0.95f);
            Text(self, rect, "Title", title, 25, 0.23f, 0.65f, 0.97f, 0.97f, TextAlignmentOptions.Left).color = color;
            return rect;
        }

        private static TextMeshProUGUI ValueBox(LobbyPanelComponent self, Transform parent, string name, float left, float bottom, float right, float top)
        {
            RectTransform box = Box(parent, name, new Color(0.025f, 0.18f, 0.30f), left, bottom, right, top);
            TextMeshProUGUI value = Text(self, box, "Value", "", 31, 0.05f, 0.03f, 0.96f, 0.97f, TextAlignmentOptions.Left);
            value.enableWordWrapping = false;
            return value;
        }

        private static RectTransform Box(Transform parent, string name, Color color, float left, float bottom, float right, float top)
        {
            RectTransform rect = Panel(name, parent, color);
            Stretch(rect, new Vector2(left, bottom), new Vector2(right, top));
            return rect;
        }

        private static TextMeshProUGUI Text(LobbyPanelComponent self, Transform parent, string name, string text, int size,
            float left, float bottom, float right, float top, TextAlignmentOptions alignment = TextAlignmentOptions.Center, bool bold = true)
        {
            TextMeshProUGUI label = Label(self, parent, name, text, size);
            Stretch(label.rectTransform, new Vector2(left, bottom), new Vector2(right, top));
            label.alignment = alignment;
            label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = size * 0.55f;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }
    }
}
