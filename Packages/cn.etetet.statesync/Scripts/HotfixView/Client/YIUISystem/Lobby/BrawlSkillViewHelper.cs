using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    public static class BrawlSkillViewHelper
    {
        public static void Create(BrawlTrainingSession session, TMP_FontAsset font, BrawlHeroConfig hero, BrawlSkillConfigCategory skills)
        {
            session.NormalAttackButton = CreateButton(session, font, "NormalAttack", "普攻", new Vector2(-110, 110));
            session.NormalAttackLabel = session.NormalAttackButton.GetComponentInChildren<TextMeshProUGUI>();
            session.SuperSkillButton = CreateButton(session, font, "SuperSkill", "大招", new Vector2(-280, 110));
            session.SuperSkillLabel = session.SuperSkillButton.GetComponentInChildren<TextMeshProUGUI>();
            BrawlSkillConfig super = skills.GetOrDefault(hero.SuperSkillId);
            session.SuperSkillImplemented = super != null && BrawlSkillEmitterHelper.Create((BrawlEmitterType)super.EmitterType) != null;
            session.NormalAttackJoystick = BrawlSkillJoystickHelper.Attach(session, session.NormalAttackButton,
                BrawlSkillSlot.NormalAttack, hero.NormalAttackSkillId, skills.GetOrDefault(hero.NormalAttackSkillId));
            session.SuperSkillJoystick = BrawlSkillJoystickHelper.Attach(session, session.SuperSkillButton,
                BrawlSkillSlot.Super, hero.SuperSkillId, skills.GetOrDefault(hero.SuperSkillId));
        }

        public static void UpdateAmmo(BrawlTrainingSession session)
        {
            if (session.NormalAttackLabel == null || session.Closing || session.CancelRequested) return;
            foreach (BrawlPracticeUnitView view in session.PracticeUnits)
            {
                if (view.UnitId != session.MyUnitId) continue;
                // 只展示服务器快照，不由客户端推进装填或扣弹；手势始终可输入，由服务端验证释放。
                session.NormalAttackLabel.text = $"普攻\n{view.Ammo:0.00}/{view.MaxAmmo}";
                if (session.SuperSkillLabel != null)
                    session.SuperSkillLabel.text = $"大招\n{view.SuperAmmo:0.00}/{view.MaxSuperAmmo}" + (session.SuperSkillImplemented ? "" : "\n释放未实现");
                return;
            }
        }

        private static Button CreateButton(BrawlTrainingSession session, TMP_FontAsset font, string name, string text, Vector2 position)
        {
            GameObject buttonObject = new(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = (RectTransform)buttonObject.transform;
            rect.SetParent(session.Overlay.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1, 0);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(140, 100);
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.65f, 0.22f, 0.12f);
            Button button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            GameObject labelObject = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = font;
            label.fontSize = 26;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = text;
            return button;
        }

    }
}

