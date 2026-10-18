using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    /// <summary>大厅内的轻量英雄界面；随 YIUI 大厅一同隐藏、销毁，不创建独立 UI 根。</summary>
    public static class BrawlHeroMenuHelper
    {
        public static void Create(LobbyPanelComponent self)
        {
            RectTransform parent = self.UIBase.OwnerRectTransform;
            // 大厅的装饰字体 DwarvenAxe 不包含中文；使用主包已有的思源黑体。
            self.HeroMenuFont = Resources.Load<TMP_FontAsset>("Fonts/SourceHanSansSC-VF SDF");
            if (self.HeroMenuFont == null)
                throw new InvalidOperationException("缺少英雄界面中文字体：Resources/Fonts/SourceHanSansSC-VF SDF");
            RectTransform root = Rect("BrawlHeroMenu", parent);
            Stretch(root, Vector2.zero, Vector2.one);
            root.SetAsLastSibling();
            self.HeroMenuRoot = root.gameObject;
            EntityRef<LobbyPanelComponent> owner = self;
            Button entry = Button(self, root, "Heroes", "英雄", () =>
            {
                LobbyPanelComponent current = owner;
                if (current != null) ShowList(current);
            });
            Place((RectTransform)entry.transform, 30, 30, 180, 65);

            RectTransform modal = Panel("HeroModal", root, new Color(0.025f, 0.035f, 0.065f, 0.98f));
            Stretch(modal, Vector2.zero, Vector2.one);
            self.HeroMenuModal = modal.gameObject;
            TextMeshProUGUI title = Label(self, modal, "Title", "英雄", 32);
            Place(title.rectTransform, 35, 24, 280, 50);
            Button close = Button(self, modal, "Close", "关闭", () =>
            {
                LobbyPanelComponent current = owner;
                if (current != null) current.HeroMenuModal.SetActive(false);
            });
            RectTransform closeRect = (RectTransform)close.transform;
            Place(closeRect, -165, 25, 135, 50);
            closeRect.anchorMin = closeRect.anchorMax = Vector2.one;

            RectTransform list = Rect("HeroList", modal);
            Stretch(list, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.84f));
            self.HeroListView = list.gameObject;
            Image listBackground = list.gameObject.AddComponent<Image>();
            listBackground.color = new Color(0.08f, 0.11f, 0.17f);
            ScrollRect scroll = list.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            RectTransform viewport = Rect("Viewport", list);
            Stretch(viewport, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 10;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            foreach (BrawlHeroConfig hero in self.Fiber().GetSingleton<BrawlHeroConfigCategory>().GetAll().Values.OrderBy(h => h.Id))
            {
                int id = hero.Id;
                string state = hero.UnlockPrice == 0 ? "已解锁" : "未解锁";
                Button item = Button(self, content, "Hero_" + id, $"{hero.Name}    {state}    战力等级 1", () =>
                {
                    LobbyPanelComponent current = owner;
                    if (current != null) ShowDetail(current, id);
                });
                LayoutElement element = item.gameObject.AddComponent<LayoutElement>();
                element.minHeight = element.preferredHeight = 70;
            }

            RectTransform detail = Panel("HeroDetail", modal, new Color(0.08f, 0.11f, 0.17f));
            Stretch(detail, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.84f));
            self.HeroDetailView = detail.gameObject;
            self.HeroDetailText = Label(self, detail, "Attributes", "", 28);
            Stretch(self.HeroDetailText.rectTransform, new Vector2(0.06f, 0.32f), new Vector2(0.94f, 0.93f));
            self.HeroDetailText.alignment = TextAlignmentOptions.TopLeft;
            Button back = Button(self, detail, "Back", "返回英雄列表", () =>
            {
                LobbyPanelComponent current = owner;
                if (current != null) ShowList(current);
            });
            Stretch((RectTransform)back.transform, new Vector2(0.06f, 0.08f), new Vector2(0.46f, 0.23f));
            Button trial = Button(self, detail, "Trial", "试用", () =>
            {
                LobbyPanelComponent current = owner;
                if (current != null) current.HeroTrialView.SetActive(true);
            });
            Stretch((RectTransform)trial.transform, new Vector2(0.54f, 0.08f), new Vector2(0.94f, 0.23f));

            RectTransform trialOverlay = Panel("TrialChoice", modal, new Color(0, 0, 0, 0.8f));
            Stretch(trialOverlay, Vector2.zero, Vector2.one);
            self.HeroTrialView = trialOverlay.gameObject;
            RectTransform card = Panel("ChoiceCard", trialOverlay, new Color(0.10f, 0.15f, 0.24f));
            Stretch(card, new Vector2(0.2f, 0.24f), new Vector2(0.8f, 0.76f));
            TextMeshProUGUI prompt = Label(self, card, "Prompt", "选择试用模式", 28);
            Stretch(prompt.rectTransform, new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.94f));
            Button practice = Button(self, card, "Practice", "练习赛", () =>
            {
                LobbyPanelComponent current = owner;
                if (current != null) OnPracticeClicked(current);
            });
            Stretch((RectTransform)practice.transform, new Vector2(0.08f, 0.43f), new Vector2(0.47f, 0.66f));
            Button training = Button(self, card, "Training", "训练场", () =>
            {
                LobbyPanelComponent current = owner;
                if (current != null) OnTrainingClicked(current);
            });
            Stretch((RectTransform)training.transform, new Vector2(0.53f, 0.43f), new Vector2(0.92f, 0.66f));
            Button cancel = Button(self, card, "Cancel", "返回", () =>
            {
                LobbyPanelComponent current = owner;
                if (current != null) current.HeroTrialView.SetActive(false);
            });
            Stretch((RectTransform)cancel.transform, new Vector2(0.25f, 0.10f), new Vector2(0.75f, 0.31f));
            self.HeroTrialView.SetActive(false);
            self.HeroDetailView.SetActive(false);
            self.HeroMenuModal.SetActive(false);
        }

        private static void ShowList(LobbyPanelComponent self)
        {
            self.HeroMenuRoot.transform.SetAsLastSibling();
            self.HeroMenuModal.SetActive(true);
            self.HeroListView.SetActive(true);
            self.HeroDetailView.SetActive(false);
            self.HeroTrialView.SetActive(false);
            self.SelectedHeroId = 0;
        }

        private static void ShowDetail(LobbyPanelComponent self, int id)
        {
            BrawlHeroConfig hero = self.Fiber().GetSingleton<BrawlHeroConfigCategory>().Get(id);
            self.SelectedHeroId = id;
            string speed = hero.MoveSpeedTier switch { 1 => "慢", 2 => "中", 3 => "快", 4 => "非常快", _ => "未知" };
            self.HeroDetailText.text = $"{hero.Name}\n\n战力等级：1\n生命值：{hero.BaseHp}\n移动速度：{speed}";
            self.HeroListView.SetActive(false);
            self.HeroDetailView.SetActive(true);
            self.HeroTrialView.SetActive(false);
        }

        private static void OnPracticeClicked(LobbyPanelComponent self)
        {
            BrawlTrainingHelper.Enter(self, 10201, "练习赛").Coroutine();
        }

        private static void OnTrainingClicked(LobbyPanelComponent self)
        {
            BrawlTrainingHelper.Enter(self, 10101, "训练场").Coroutine();
        }

        public static void Destroy(LobbyPanelComponent self)
        {
            BrawlTrainingHelper.Cancel(self);
            if (self.HeroMenuRoot != null)
            {
                foreach (Button button in self.HeroMenuRoot.GetComponentsInChildren<Button>(true)) button.onClick.RemoveAllListeners();
                UnityEngine.Object.Destroy(self.HeroMenuRoot);
            }
            self.HeroMenuRoot = self.HeroMenuModal = self.HeroListView = self.HeroDetailView = self.HeroTrialView = null;
            self.HeroDetailText = null;
            self.HeroMenuFont = null;
            self.SelectedHeroId = 0;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            GameObject go = new(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform Panel(string name, Transform parent, Color color)
        {
            RectTransform rect = Rect(name, parent);
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        private static TextMeshProUGUI Label(LobbyPanelComponent self, Transform parent, string name, string text, int size)
        {
            TextMeshProUGUI label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = self.HeroMenuFont;
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.richText = false;
            label.enableWordWrapping = true;
            return label;
        }

        private static Button Button(LobbyPanelComponent self, Transform parent, string name, string text, Action action)
        {
            RectTransform rect = Panel(name, parent, new Color(0.18f, 0.28f, 0.43f));
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            Navigation navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            button.onClick.AddListener(() => action());
            TextMeshProUGUI label = Label(self, rect, "Label", text, 24);
            Stretch(label.rectTransform, new Vector2(0.03f, 0.03f), new Vector2(0.97f, 0.97f));
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
