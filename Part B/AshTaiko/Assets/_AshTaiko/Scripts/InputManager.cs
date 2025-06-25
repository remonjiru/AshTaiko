using UnityEngine;
using UnityEngine.InputSystem;

namespace AshTaiko
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set;}

        [SerializeField]
        private InputActionAsset _inputActions;
        
        private void Start()
        {
            if (_inputActions == null)
            {
                GameDebug.LogError("InputActionAsset is currently unassigned.");
            }
        }
    }
}
