using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace AshTaiko
{
    public class DrumInputIndicatorPart : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        private void OnEnable()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Hit()
        {
            _spriteRenderer.DOFade(1, 0.001f);
            _spriteRenderer.DOFade(0, 0.15f);
        }
    }
}
