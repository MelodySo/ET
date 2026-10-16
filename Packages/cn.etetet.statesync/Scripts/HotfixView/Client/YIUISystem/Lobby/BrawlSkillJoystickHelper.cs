using Unity.Mathematics;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ET.Client
{
    /// <summary>普攻/大招复用的本地手势。只有有效松手才请求释放，不同步准备过程。</summary>
    public static class BrawlSkillJoystickHelper
    {
        public const float HandleRange = 60;
        public const float DeadZone = 0.2f;

        public static BrawlSkillJoystick Attach(BrawlTrainingSession session, Button button, BrawlSkillSlot slot,
            int skillId, BrawlSkillConfig config)
        {
            BrawlSkillJoystick joystick = new()
            {
                Slot = slot, SkillId = skillId, Config = config, Area = (RectTransform)button.transform,
            };
            GameObject handleObject = new("AimHandle", typeof(RectTransform), typeof(Image));
            joystick.Handle = (RectTransform)handleObject.transform;
            joystick.Handle.SetParent(joystick.Area, false);
            joystick.Handle.anchorMin = joystick.Handle.anchorMax = new Vector2(0.5f, 0.5f);
            joystick.Handle.sizeDelta = new Vector2(40, 40);
            Image image = handleObject.GetComponent<Image>();
            image.color = new Color(1, 1, 1, 0.35f);
            image.raycastTarget = false;
            handleObject.SetActive(false);
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();
            Add(trigger, EventTriggerType.InitializePotentialDrag, data => ((PointerEventData)data).useDragThreshold = false);
            Add(trigger, EventTriggerType.PointerDown, data =>
            {
                PointerEventData pointer = (PointerEventData)data;
                if (!CanInteract(session) || !button.interactable || joystick.PointerId.HasValue ||
                    pointer.button != PointerEventData.InputButton.Left) return;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(joystick.Area, pointer.position,
                        pointer.pressEventCamera, out Vector2 point)) return;
                joystick.PointerId = pointer.pointerId;
                joystick.PressPoint = point;
                BrawlSkillAimHelper.Begin(joystick.Aim);
                joystick.Handle.anchoredPosition = Vector2.zero;
                joystick.Handle.gameObject.SetActive(true);
            });
            Add(trigger, EventTriggerType.Drag, data => Sample(session, joystick, (PointerEventData)data));
            Add(trigger, EventTriggerType.PointerUp, data =>
            {
                PointerEventData pointer = (PointerEventData)data;
                if (joystick.PointerId != pointer.pointerId) return;
                if (!CanInteract(session)) { Cancel(session, joystick); return; }
                // PointerUp 最终位置必须参与判定，不能依赖最后一个 Drag 事件。
                if (!Sample(session, joystick, pointer)) { Cancel(session, joystick); return; }
                Vector2 move = BrawlMovementInputHelper.ClickDirection(session);
                float2 result = BrawlSkillAimHelper.Release(joystick.Aim, new float2(move.x, move.y));
                Cancel(session, joystick);
                if (math.lengthsq(result) > 0) BrawlSkillInputHelper.Cast(session, joystick.Slot, new Vector2(result.x, result.y));
            });
            Add(trigger, EventTriggerType.Cancel, _ => Cancel(session, joystick));
            return joystick;
        }

        private static bool CanInteract(BrawlTrainingSession session)
        {
            if (session.Loading || session.Closing || session.CancelRequested || !Application.isFocused ||
                session.Hero == null || !session.Hero.activeInHierarchy || session.SkillAimUnitId != session.MyUnitId) return false;
            foreach (BrawlPracticeUnitView view in session.PracticeUnits)
            {
                if (view.UnitId != session.MyUnitId) continue;
                return !(view.States.TryGetValue(StateId.Dead, out bool dead) && dead) &&
                    !(view.States.TryGetValue(StateId.Spawning, out bool spawning) && spawning);
            }
            return false;
        }

        private static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private static bool Sample(BrawlTrainingSession session, BrawlSkillJoystick joystick, PointerEventData pointer)
        {
            if (joystick.PointerId != pointer.pointerId || !CanInteract(session)) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(joystick.Area, pointer.position,
                    pointer.pressEventCamera, out Vector2 point)) return false;
            Vector2 offset = point - joystick.PressPoint;
            Vector2 worldOffset = BrawlCameraInputHelper.ToWorld(session.BattleCamera, offset);
            BrawlSkillAimHelper.Sample(joystick.Aim, new float2(worldOffset.x, worldOffset.y), HandleRange * DeadZone);
            joystick.Handle.anchoredPosition = Vector2.ClampMagnitude(offset, HandleRange);
            UpdatePreview(session, joystick);
            return true;
        }

        public static void Update(BrawlTrainingSession session)
        {
            if (session.SkillAimUnitId != session.MyUnitId)
            {
                CancelAll(session);
                session.SkillAimUnitId = session.MyUnitId;
            }
            if (!CanInteract(session)) { CancelAll(session); return; }
            // 英雄在准备瞄准时仍可移动，预留表现原点每帧跟随英雄。
            UpdatePreview(session, session.NormalAttackJoystick);
            UpdatePreview(session, session.SuperSkillJoystick);
        }

        private static void UpdatePreview(BrawlTrainingSession session, BrawlSkillJoystick joystick)
        {
            if (joystick == null) return;
            bool visible = joystick.Aim.Active && math.lengthsq(joystick.Aim.Direction) > 0;
            if (!visible)
            {
                if (joystick.PreviewVisible) session.SkillAimPreview?.Hide(session, joystick.Slot);
                joystick.PreviewVisible = false;
                return;
            }
            joystick.PreviewVisible = true;
            session.SkillAimPreview?.ShowOrUpdate(session, new BrawlSkillAimPreviewData
            {
                Slot = joystick.Slot, SkillId = joystick.SkillId, Config = joystick.Config,
                Origin = session.Hero != null ? session.Hero.transform.position : Vector3.zero,
                Direction = new Vector2(joystick.Aim.Direction.x, joystick.Aim.Direction.y),
            });
        }

        public static void SetPreview(BrawlTrainingSession session, IBrawlSkillAimPreview preview)
        {
            // 替换表现实现前隐藏旧实例，不将范围对象留在场景里。
            if (session.NormalAttackJoystick?.PreviewVisible == true) session.SkillAimPreview?.Hide(session, BrawlSkillSlot.NormalAttack);
            if (session.SuperSkillJoystick?.PreviewVisible == true) session.SkillAimPreview?.Hide(session, BrawlSkillSlot.Super);
            session.SkillAimPreview = preview;
            Update(session);
        }

        public static void CancelAll(BrawlTrainingSession session)
        {
            Cancel(session, session.NormalAttackJoystick);
            Cancel(session, session.SuperSkillJoystick);
        }

        private static void Cancel(BrawlTrainingSession session, BrawlSkillJoystick joystick)
        {
            if (joystick == null) return;
            BrawlSkillAimHelper.Reset(joystick.Aim);
            joystick.PointerId = null;
            if (joystick.PreviewVisible) session.SkillAimPreview?.Hide(session, joystick.Slot);
            joystick.PreviewVisible = false;
            if (joystick.Handle == null) return;
            joystick.Handle.anchoredPosition = Vector2.zero;
            joystick.Handle.gameObject.SetActive(false);
        }
    }
}

