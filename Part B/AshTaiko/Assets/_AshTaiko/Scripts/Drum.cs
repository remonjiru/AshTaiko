using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace AshTaiko
{
    public class Drum : MonoBehaviour
    {
        [SerializeField]
        private InputReader _input;

        public event UnityAction<HitType> OnHit;

        private void Start()
        {
            _input.DonLeftEvent += DonLeft;
            _input.DonRightEvent += DonRight;
            _input.KaLeftEvent += KaLeft;
            _input.KaRightEvent += KaRight;
        }

        private void DonLeft()
        {
            OnHit?.Invoke(HitType.Don);
        }

        private void DonRight()
        {
            OnHit?.Invoke(HitType.Don);
        }

        private void KaLeft()
        {
            OnHit?.Invoke(HitType.Ka);
        }

        private void KaRight()
        {
            OnHit?.Invoke(HitType.Ka);
        }
    }

    public class HitData
    {
        public HitData(HitType hitType)
        {
            HitType = hitType;
        }

        public HitType HitType { get; private set; }
    }

    public enum HitType
    {
        Don,
        Ka
    }
}
