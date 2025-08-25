using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AshTaiko
{
    /// <summary>
    /// Handles drum input detection and converts raw input events into game events.
    /// Listens for Don and Ka hits from both left and right sides of the drum
    /// and raises events that the GameManager can respond to.
    /// </summary>
    public class Drum : MonoBehaviour
    {
        [SerializeField]
        private InputReader _input;

        /// <summary>
        /// Event raised when a drum hit is detected.
        /// </summary>
        public event UnityAction<HitType> OnHit;

        /// <summary>
        /// Sets up input event subscriptions for drum hits.
        /// </summary>
        private void Start()
        {
            _input.DonLeftEvent += DonLeft;
            _input.DonRightEvent += DonRight;
            _input.KaLeftEvent += KaLeft;
            _input.KaRightEvent += KaRight;
        }

        /// <summary>
        /// Handles left Don hit input.
        /// </summary>
        private void DonLeft()
        {
            OnHit?.Invoke(HitType.Don);
        }

        /// <summary>
        /// Handles right Don hit input.
        /// </summary>
        private void DonRight()
        {
            OnHit?.Invoke(HitType.Don);
        }

        /// <summary>
        /// Handles left Ka hit input.
        /// </summary>
        private void KaLeft()
        {
            OnHit?.Invoke(HitType.Ka);
        }

        /// <summary>
        /// Handles right Ka hit input.
        /// </summary>
        private void KaRight()
        {
            OnHit?.Invoke(HitType.Ka);
        }
    }

    /// <summary>
    /// Contains data about a drum hit event.
    /// </summary>
    public class HitData
    {
        /// <summary>
        /// Initializes a new HitData instance with the specified hit type.
        /// </summary>
        /// <param name="hitType">The type of hit that occurred.</param>
        public HitData(HitType hitType)
        {
            HitType = hitType;
        }

        /// <summary>
        /// The type of hit that occurred.
        /// </summary>
        public HitType HitType { get; private set; }
    }

    /// <summary>
    /// Represents the two types of drum hits in taiko gameplay.
    /// </summary>
    public enum HitType
    {
        /// <summary>
        /// Don hit (red circle) - typically hit with drumsticks.
        /// </summary>
        Don,
        
        /// <summary>
        /// Ka hit (blue circle) - typically hit with hands.
        /// </summary>
        Ka
    }
}
