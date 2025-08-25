using UnityEngine;
using UnityEngine.InputSystem;

namespace AshTaiko
{
    /// <summary>
    /// Manages input system configuration and provides access to input actions.
    /// Currently serves as a placeholder for future input system integration.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance providing global access to input management.
        /// </summary>
        public static InputManager Instance { get; private set;}

        [SerializeField]
        private InputActionAsset _inputActions;
        
        /// <summary>
        /// Validates that the InputActionAsset is assigned.
        /// </summary>
        private void Start()
        {
            if (_inputActions == null)
            {
                GameDebug.LogError("InputActionAsset is currently unassigned.");
            }
        }
    }
}
