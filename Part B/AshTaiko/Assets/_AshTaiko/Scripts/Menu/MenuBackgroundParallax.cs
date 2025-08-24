using UnityEngine;

namespace AshTaiko
{
    public class MenuBackgroundParallax : MonoBehaviour
    {
        [SerializeField]
        private float _offsetMultiplier = 1f;
        
        [SerializeField]
        private float _smoothTime = 0.3f;

        private Vector3 _startPosition;
        private Vector3 _velocity;

        private void Start()
        {
            _startPosition = new Vector3(transform.position.x, transform.position.y, 0);
        }

        private void Update()
        {
            Vector2 offset = Camera.main.ScreenToViewportPoint(UnityEngine.Input.mousePosition);
            transform.position = Vector3.SmoothDamp(transform.position, _startPosition + (Vector3)(offset * _offsetMultiplier), ref _velocity, _smoothTime);
        }
    }
}
