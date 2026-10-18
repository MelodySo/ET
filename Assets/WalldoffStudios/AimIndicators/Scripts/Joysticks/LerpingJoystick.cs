using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace WalldoffStudios.Joysticks
{
    public abstract class LerpingJoystick : JoystickBase
    {
        [SerializeField] private float followAfterDistance = 40.0f;
        [SerializeField] private float lerpSpeed = 0.5f;
        private float distanceMultiplier = 1000.0f;
        private float multipliedDistance;

        private void Awake()
        {
            multipliedDistance = followAfterDistance * distanceMultiplier;
        }

        public override void OnDrag(PointerEventData eventData)
        {
            base.OnDrag(eventData);

            float distanceFromJoystick = ((Vector2)joystick.position - eventData.position).sqrMagnitude;
            
            if (distanceFromJoystick > multipliedDistance)
            {
                joystickBackground.position = Vector2.Lerp(
                    joystickBackground.position,
                    eventData.position,
                    lerpSpeed * Time.deltaTime);
            }
        }
    }   
}
