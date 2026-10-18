using UnityEngine;
using UnityEngine.EventSystems;

namespace WalldoffStudios.Joysticks
{
    public class MoveJoystick : LerpingJoystick
    {
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            JoystickUpdateDirection(Vector2.zero);
        }
    }   
}
