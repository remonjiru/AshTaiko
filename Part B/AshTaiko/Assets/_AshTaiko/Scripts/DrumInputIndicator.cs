using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace AshTaiko
{
    public class DrumInputIndicator : MonoBehaviour
    {
        [SerializeField]
        private InputReader _input;

        [Header("Elements")]
        [SerializeField]
        private DrumInputIndicatorPart _donLeft;
        [SerializeField]
        private DrumInputIndicatorPart _donRight;

        [SerializeField]
        private DrumInputIndicatorPart _kaLeft;
        [SerializeField]
        private DrumInputIndicatorPart _kaRight;

        private void Start()
        {
            _input.DonLeftEvent += DonLeft;
            _input.DonRightEvent += DonRight;
            _input.KaLeftEvent += KaLeft;
            _input.KaRightEvent += KaRight;
        }

        private void DonLeft()
        {
            Debug.Log("DL");
            _donLeft.Hit();
        }

        private void DonRight()
        {
            Debug.Log("DR");
            _donRight.Hit();
        }

        private void KaLeft()
        {
            Debug.Log("KL");
            _kaLeft.Hit();
        }

        private void KaRight()
        {
            Debug.Log("KR");
            _kaRight.Hit();
        }
    }
}
