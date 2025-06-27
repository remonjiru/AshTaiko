using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System;

namespace AshTaiko
{
    public class DrumInputIndicator : MonoBehaviour
    {
        [SerializeField]
        private InputReader _input;

        [SerializeField]
        private DrumInputIndicatorPart _receptacle;

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
            GameManager.Instance.OnNoteHit += OnHit;

            _donLeft.Reset();
            _donRight.Reset();
            _kaLeft.Reset();
            _kaRight.Reset();
        }

        private void OnHit(Note note)
        {
            _receptacle.Hit();
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
