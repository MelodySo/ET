using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public sealed class BrawlSkillJoystick
    {
        public BrawlSkillSlot Slot;
        public int SkillId;
        public BrawlSkillConfig Config;
        public RectTransform Area;
        public RectTransform Handle;
        public int? PointerId;
        public Vector2 PressPoint;
        public readonly BrawlSkillAimState Aim = new();
        public bool PreviewVisible;
    }
}
