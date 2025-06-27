using AshTaiko.Input;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AshTaiko
{
    [CreateAssetMenu(menuName = "Input/InputReader")]
    public class InputReader : ScriptableObject, GameInput.IGameplayActions
    {
        private GameInput _gameInput;

        private void OnEnable()
        {
            if (_gameInput == null)
            {
                _gameInput = new GameInput();

                _gameInput.Gameplay.SetCallbacks(this);
                _gameInput.Gameplay.Enable();
            }
        }

        private void OnDisable()
        {
            _gameInput.Gameplay.Disable();
        }

        public event UnityAction DonLeftEvent = delegate { };
        public event UnityAction DonRightEvent = delegate { };
        public event UnityAction DonBigEvent = delegate { };

        public event UnityAction KaLeftEvent = delegate { };
        public event UnityAction KaRightEvent = delegate { };

        public void OnDon_Left(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                DonLeftEvent?.Invoke();
            }
        }

        public void OnDon_Right(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                DonRightEvent?.Invoke();
            }
        }

        public void OnKa_Left(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                KaLeftEvent?.Invoke();
            }
        }

        public void OnKa_Right(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Performed)
            {
                KaRightEvent?.Invoke();
            }
        }
    }
}
