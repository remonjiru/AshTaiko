using UnityEngine;
using TMPro;

namespace AshTaiko
{
    /// <summary>
    /// Displays real-time frame rate and frame time information.
    /// Provides performance monitoring during gameplay to help identify
    /// performance issues and optimize the game.
    /// </summary>
    public class FPSCounter : MonoBehaviour
    {
        [SerializeField]
        private bool _showMs = true;
        
        [SerializeField]
        private float _refresh = 0.5f;
        
        /// <summary>
        /// Timer for refreshing the FPS display.
        /// </summary>
        private float _timer;
        
        /// <summary>
        /// Average frame rate calculated over time.
        /// </summary>
        private float _avgFramerate;
        
        /// <summary>
        /// Text component for displaying FPS information.
        /// </summary>
        private TextMeshProUGUI _displayText;
        
        /// <summary>
        /// Smoothed delta time for more stable FPS calculation.
        /// </summary>
        private float _deltaTime;

        /// <summary>
        /// Gets the TextMeshProUGUI component for display.
        /// </summary>
        private void Awake()
        {
            _displayText = GetComponent<TextMeshProUGUI>();
        }

        /// <summary>
        /// Updates FPS calculation and display text.
        /// Calculates smoothed frame time and updates the display at regular intervals.
        /// </summary>
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

