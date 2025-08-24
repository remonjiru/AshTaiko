using UnityEngine;
using TMPro;

namespace AshTaiko
{
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField]
        private bool _showMs = true;
        
        [SerializeField]
        private float _refresh = 0.5f;
        
        private float _timer;
        private float _avgFramerate;
        private TextMeshProUGUI _displayText;
        private float _deltaTime;

        private void Awake()
        {
            _displayText = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
            float ms = _deltaTime * 1000f;
            float fps = 1f / _deltaTime;

            float timeLapse = Time.unscaledDeltaTime;
            _timer = _timer <= 0 ? _refresh : _timer - timeLapse;

            if (_timer <= 0) 
            {
                _avgFramerate = (int)(1f / timeLapse);
            }
            
            if (_showMs) 
            {
                _displayText.text = string.Format("{0:0.0} ms ({1:0.} fps)", ms, fps);
            }
        }
    }
}

