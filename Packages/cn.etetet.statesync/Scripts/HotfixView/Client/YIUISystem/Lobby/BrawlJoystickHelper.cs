using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ET.Client
{
    public static class BrawlJoystickHelper
    {
        private const float HandleRange = 60;
        // 摇杆行程的 20%（HandleRange=60 时为 12 UI 单位）；死区外只取方向，不按力度降速。
        private const float DeadZone = 0.2f;

        public static void Create(BrawlTrainingSession session)
        {
            GameObject background = new("MoveJoystick", typeof(RectTransform), typeof(Image), typeof(EventTrigger));
            RectTransform rect = (RectTransform)background.transform;
            rect.SetParent(session.Overlay.transform, false);
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(120, 120);
            rect.sizeDelta = new Vector2(160, 160);
            background.GetComponent<Image>().color = new Color(0.1f, 0.15f, 0.22f, 0.65f);
            session.JoystickBase = rect;
            GameObject knob = new("Handle", typeof(RectTransform), typeof(Image));
            session.JoystickKnob = (RectTransform)knob.transform;
            session.JoystickKnob.SetParent(rect, false);
            session.JoystickKnob.anchorMin = session.JoystickKnob.anchorMax = new Vector2(0.5f, 0.5f);
            session.JoystickKnob.sizeDelta = new Vector2(48, 48);
            Image image = knob.GetComponent<Image>();
            image.color = new Color(0.65f, 0.8f, 1, 0.9f);
            image.raycastTarget = false;
            EventTrigger trigger = background.GetComponent<EventTrigger>();
            Add(trigger, EventTriggerType.PointerDown, data =>
            {
                PointerEventData pointer = (PointerEventData)data;
                if (session.Loading || session.Closing || session.JoystickPointer.HasValue) return;
                session.JoystickPointer = pointer.pointerId;
                Drag(session, pointer);
            });
            Add(trigger, EventTriggerType.Drag, data => Drag(session, (PointerEventData)data));
            Add(trigger, EventTriggerType.PointerUp, data =>
            {
                if (session.JoystickPointer == ((PointerEventData)data).pointerId) Reset(session);
            });
            Add(trigger, EventTriggerType.Cancel, _ => Reset(session));
            background.SetActive(false);
        }

        private static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
        {
            EventTrigger.Entry entry = new() { eventID = type };
            entry.callback.AddListener(action);
            trigger.triggers.Add(entry);
        }

        private static void Drag(BrawlTrainingSession session, PointerEventData pointer)
        {
            if (session.JoystickPointer != pointer.pointerId || session.Closing) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(session.JoystickBase, pointer.position, pointer.pressEventCamera, out Vector2 point)) return;
            Vector2 value = Vector2.ClampMagnitude(point / HandleRange, 1);
            session.JoystickDirection = value.sqrMagnitude <= DeadZone * DeadZone ? Vector2.zero : value.normalized;
            session.JoystickKnob.anchoredPosition = value * HandleRange;
        }

        public static void Reset(BrawlTrainingSession session)
        {
            session.JoystickPointer = null;
            session.JoystickDirection = Vector2.zero;
            if (session.JoystickKnob != null) session.JoystickKnob.anchoredPosition = Vector2.zero;
        }
    }
}
