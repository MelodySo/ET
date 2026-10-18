using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace WalldoffStudios
{
    [CreateAssetMenu(fileName = "inputReader", menuName = "ScriptableObjects/Gameplay/InputReader")]
    public class InputReader : ScriptableObject, Controls.IGameplayActions
    {
        #region Events

        public event UnityAction<Vector2> MoveEvent;
        public event UnityAction<Vector2> AimEvent;
        public event UnityAction<bool> AimToggleEvent;
        public event UnityAction ShotEvent;
        public event UnityAction IndicatorResetEvent;
        public event UnityAction<bool> IndicatorTexToggleEvent;

        #endregion Events
        
        private Controls _controls;

        private void OnEnable()
        {
            if (_controls != null) return;

            _controls = new Controls();
            _controls.Gameplay.SetCallbacks(this);
        }

        private void OnDisable() => DisableAllInput();

        public void ToggleInput(bool toggle)
        {
            if (toggle && !_controls.Gameplay.enabled) EnableGameplayInput();
            else if (!toggle && _controls.Gameplay.enabled) DisableAllInput();
        }

        private void EnableGameplayInput() => _controls.Gameplay.Enable();
        private void DisableAllInput() => _controls.Gameplay.Disable();

        public void OnMovement(InputAction.CallbackContext ctx)
        {
            if (ctx.phase == InputActionPhase.Performed)
            {
                MoveEvent?.Invoke(ctx.ReadValue<Vector2>());
            }

            if (ctx.phase == InputActionPhase.Canceled)
            {
                MoveEvent?.Invoke(Vector2.zero);
            }
        }

        public void OnAiming(InputAction.CallbackContext ctx)
        {
            if (ctx.started)
            {
                AimToggleEvent?.Invoke(true);    
            }
            
            if (ctx.performed)
            {
                AimEvent?.Invoke(ctx.ReadValue<Vector2>());
            }

            if (ctx.canceled)
            {
                AimToggleEvent?.Invoke(false);
            }
        }

        public void OnAttack(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) ShotEvent?.Invoke();
        }

        public void OnReset(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) IndicatorResetEvent?.Invoke();
        }

        public void OnToggleRight(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) IndicatorTexToggleEvent?.Invoke(true);
        }

        public void OnToggleLeft(InputAction.CallbackContext ctx)
        {
            if (ctx.performed) IndicatorTexToggleEvent?.Invoke(false);
        }
    }
}