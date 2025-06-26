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
            _donLeft.Hit();
        }

        private void DonRight()
        {
            _donRight.Hit();
        }

        private void KaLeft()
        {
            _kaLeft.Hit();
        }

        private void KaRight()
        {
            _kaRight.Hit();
        }
    }
}
