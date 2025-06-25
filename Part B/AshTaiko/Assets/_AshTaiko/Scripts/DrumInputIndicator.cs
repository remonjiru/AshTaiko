using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace AshTaiko
{
    public class DrumInputIndicator : MonoBehaviour
    {
        [SerializeField]
        private InputActionAsset _inputActions;

        [Header("Elements")]
        [SerializeField]
        private SpriteRenderer _kaLeftSprite;
        [SerializeField]
        private SpriteRenderer _donLeftSprite;

        private void OnEnable()
        {
            if (_inputActions == null)
            {
                GameDebug.LogError("InputActionAsset is currently unassigned.");
            }
        }

        private void Update()
        {
            CheckForInputs();
        }

        private void CheckForInputs()
        {
            if (_inputActions.FindAction("Ka_Left").WasPressedThisFrame()) KaLeft();
            if (_inputActions.FindAction("Don_Left").WasPressedThisFrame()) DonLeft();
            if (_inputActions.FindAction("Don_Right").WasPressedThisFrame()) DonRight();
            if (_inputActions.FindAction("Ka_Right").WasPressedThisFrame()) KaRight();
        }

        private void DonLeft()
        {

        }

        private void DonRight()
        {

        }

        private void KaLeft()
        {

        }

        private void KaRight()
        {

        }
    }
}
