using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Creates a parallax effect for menu backgrounds based on mouse position.
    /// Provides subtle movement to make the menu more visually engaging.
    /// </summary>
    public class MenuBackgroundParallax : MonoBehaviour
    {
        [SerializeField]
        private float _offsetMultiplier = 1f;
        
        [SerializeField]
        private float _smoothTime = 0.3f;

        private Vector3 _startPosition;
        private Vector3 _velocity;

        /// <summary>
        /// Stores the initial position of the background for parallax calculations.
        /// </summary>
        private void Start()
        {
            _startPosition = new Vector3(transform.position.x, transform.position.y, 0);
        }

        /// <summary>
        /// Updates the background position based on mouse movement for parallax effect.
        /// </summary>
        private void Update()
        {
            Vector2 offset = Camera.main.ScreenToViewportPoint(UnityEngine.Input.mousePosition);
            transform.position = Vector3.SmoothDamp(transform.position, _startPosition + (Vector3)(offset * _offsetMultiplier), ref _velocity, _smoothTime);
        }
    }
}
