using UnityEngine;
using UnityEngine.EventSystems;

namespace WalldoffStudios.Joysticks
{
    public abstract class JoystickBase : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] protected RectTransform joystick = null;
        [SerializeField] protected RectTransform joystickBackground = null;
        [SerializeField] private float joystickRange = 50;
        
        private Vector2 startPos;
        private Vector2 pointerDownPos;
        private Vector2 containerStartPos;
        protected Vector2 joystickDir;

        public UpdateDirection JoystickUpdateDirection;

        public delegate void UpdateDirection(Vector2 dir);

        private bool invertedControls;
        private bool initialized;

        protected float timeHeldDown;

        public void Initialize(bool inverted = false)
        {
            invertedControls = inverted;
            initialized = true;
        }

        private void Start()
        {
            startPos = joystick.anchoredPosition;
            containerStartPos = joystickBackground.anchoredPosition;
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (initialized == false || eventData == null) return;
            timeHeldDown = 0.0f;
            
            
            joystickBackground.position = eventData.position;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground, 
                eventData.position, 
                eventData.pressEventCamera,
                out pointerDownPos);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (initialized == false || eventData == null) return;

            timeHeldDown += Time.deltaTime;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground, 
                eventData.position,
                eventData.pressEventCamera, 
                out Vector2 position);

            Vector2 delta = position - pointerDownPos;
            
            delta = Vector2.ClampMagnitude(delta, joystickRange);
            joystick.anchoredPosition = startPos + delta;

            joystickDir.x = delta.x / joystickRange;
            joystickDir.y = delta.y / joystickRange;
            
            JoystickUpdateDirection(invertedControls? -joystickDir : joystickDir);
        }
        
        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (initialized == false || eventData == null) return;
            
            joystick.anchoredPosition = startPos;
            joystickBackground.anchoredPosition = containerStartPos;
        }
    }
}
