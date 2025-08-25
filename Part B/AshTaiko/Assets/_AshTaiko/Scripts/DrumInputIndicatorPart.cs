using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

namespace AshTaiko
{
    /// <summary>
    /// Individual visual indicator for a specific drum input area.
    /// Provides fade in/out animations to show when input is detected.
    /// </summary>
    public class DrumInputIndicatorPart : MonoBehaviour
    {
        /// <summary>
        /// Sprite renderer component for controlling opacity.
        /// </summary>
        private SpriteRenderer _spriteRenderer;

        /// <summary>
        /// Gets the SpriteRenderer component when the object is enabled.
        /// </summary>
        private void OnEnable()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Triggers a hit animation with quick fade in followed by fade out.
        /// </summary>
        public void Hit()
        {
            _spriteRenderer.DOFade(1, 0.001f);
            _spriteRenderer.DOFade(0, 0.15f);
        }

        /// <summary>
        /// Resets the indicator to its default transparent state.
        /// </summary>
        public void Reset()
        {
            _spriteRenderer.DOFade(0, 0.001f);
        }
    }
}
