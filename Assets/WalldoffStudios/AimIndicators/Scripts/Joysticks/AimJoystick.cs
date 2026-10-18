using UnityEngine;
using UnityEngine.EventSystems;

namespace WalldoffStudios.Joysticks
{
    public class AimJoystick : JoystickBase
    {
        public ToggleAim JoystickToggleAim;
        public Attack JoystickAttack;
        public delegate void ToggleAim(bool toggle);
        public delegate void Attack(Vector2 aimDir);

        private Vector3 previousAim;
        

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            JoystickToggleAim(true);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            JoystickToggleAim(false);

            if (timeHeldDown > 0.01f)
            {
                JoystickAttack(joystickDir);
                previousAim = joystickDir;
            }
            else
            {
                JoystickAttack(previousAim);
            }
            base.OnPointerUp(eventData);
        }
    }   
}
