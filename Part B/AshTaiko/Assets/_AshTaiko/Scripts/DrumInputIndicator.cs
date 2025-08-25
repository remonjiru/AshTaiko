using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using System;

namespace AshTaiko
{
    /// <summary>
    /// Manages visual feedback for drum input detection.
    /// Coordinates input events with visual indicators to show
    /// when different parts of the drum are hit during gameplay.
    /// </summary>
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

        /// <summary>
        /// Sets up input event subscriptions and initializes indicator states.
        /// </summary>
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

        /// <summary>
        /// Handles note hit events by triggering the receptacle indicator.
        /// </summary>
        /// <param name="note">The note that was hit.</param>
        private void OnHit(Note note)
        {
            _receptacle.Hit();
        }

        /// <summary>
        /// Handles left Don input by triggering the left Don indicator.
        /// </summary>
        private void DonLeft()
        {
            _donLeft.Hit();
        }

        /// <summary>
        /// Handles right Don input by triggering the right Don indicator.
        /// </summary>
        private void DonRight()
        {
            _donRight.Hit();
        }

        /// <summary>
        /// Handles left Ka input by triggering the left Ka indicator.
        /// </summary>
        private void KaLeft()
        {
            _kaLeft.Hit();
        }

        /// <summary>
        /// Handles right Ka input by triggering the right Ka indicator.
        /// </summary>
        private void KaRight()
        {
            _kaRight.Hit();
        }
    }
}
