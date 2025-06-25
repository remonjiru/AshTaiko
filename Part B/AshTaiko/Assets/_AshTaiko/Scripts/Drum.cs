using UnityEngine;
using UnityEngine.InputSystem;

namespace AshTaiko
{
    public class Drum : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset _actions;

        private void Update()
        {
            if(_actions.FindAction("KaLeft").WasPressedThisFrame())
            {
                Debug.Log("lk");
            }
        }
    }
}
