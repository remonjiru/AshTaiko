using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AshTaiko
{
    /// <summary>
    /// Provides UI controls for adjusting background dimming during gameplay.
    /// This allows players to optimize visibility based on their preferences and lighting conditions.
    /// </summary>
    public class BackgroundControls : MonoBehaviour
    {
        [Header("UI Controls")]
        [SerializeField] private Slider _dimSlider;
        [SerializeField] private TextMeshProUGUI _dimValueText;
        [SerializeField] private Button _toggleBackgroundButton;
        [SerializeField] private TextMeshProUGUI _toggleButtonText;
        
        [Header("Settings")]
        [SerializeField] private float _defaultDim = 0.7f;
        [SerializeField] private bool _showControlsByDefault = true;
        
        private GameManager _gameManager;
        private bool _backgroundEnabled = true;
        
        private void Start()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null)
            {
                Debug.LogError("BackgroundControls: GameManager not found!");
                return;
            }
            
            InitializeControls();
            SetupEventHandlers();
            
            // Set initial state
            SetBackgroundEnabled(_showControlsByDefault);
        }
        
        /// <summary>
        /// Initializes the UI controls with default values.
        /// </summary>
        private void InitializeControls()
        {
            if (_dimSlider != null)
            {
                _dimSlider.minValue = 0f;
                _dimSlider.maxValue = 1f;
                _dimSlider.value = _defaultDim;
            }
            
            if (_toggleButtonText != null)
            {
                _toggleButtonText.text = _backgroundEnabled ? "Hide Background" : "Show Background";
            }
            
            UpdateDimValueDisplay();
        }
        
        /// <summary>
        /// Sets up event handlers for UI controls.
        /// </summary>
        private void SetupEventHandlers()
        {
            if (_dimSlider != null)
            {
                _dimSlider.onValueChanged.AddListener(OnDimSliderChanged);
            }
            
            if (_toggleBackgroundButton != null)
            {
                _toggleBackgroundButton.onClick.AddListener(OnToggleBackgroundClicked);
            }
        }
        
        /// <summary>
        /// Handles changes to the dim slider.
        /// </summary>
        /// <param name="value">New dim value from slider.</param>
        private void OnDimSliderChanged(float value)
        {
            if (_gameManager != null)
            {
                _gameManager.SetBackgroundDim(value);
                UpdateDimValueDisplay();
            }
        }
        
        /// <summary>
        /// Handles clicks on the toggle background button.
        /// </summary>
        private void OnToggleBackgroundClicked()
        {
            _backgroundEnabled = !_backgroundEnabled;
            SetBackgroundEnabled(_backgroundEnabled);
        }
        
        /// <summary>
        /// Sets whether the background system is enabled.
        /// </summary>
        /// <param name="enabled">Whether to enable the background.</param>
        private void SetBackgroundEnabled(bool enabled)
        {
            _backgroundEnabled = enabled;
            
            if (_gameManager != null)
            {
                _gameManager.SetBackgroundEnabled(enabled);
            }
            
            if (_toggleButtonText != null)
            {
                _toggleButtonText.text = _backgroundEnabled ? "Hide Background" : "Show Background";
            }
            
            // Update slider interactability
            if (_dimSlider != null)
            {
                _dimSlider.interactable = _backgroundEnabled;
            }
            
            if (_dimValueText != null)
            {
                _dimValueText.color = _backgroundEnabled ? Color.white : Color.gray;
            }
        }
        
        /// <summary>
        /// Updates the dim value display text.
        /// </summary>
        private void UpdateDimValueDisplay()
        {
            if (_dimValueText != null && _dimSlider != null)
            {
                float dimValue = _dimSlider.value;
                _dimValueText.text = $"Dim: {(dimValue * 100):F0}%";
            }
        }
        
        /// <summary>
        /// Sets the background dim level programmatically.
        /// </summary>
        /// <param name="dimLevel">Dim level from 0 to 1.</param>
        public void SetDimLevel(float dimLevel)
        {
            if (_dimSlider != null)
            {
                _dimSlider.value = Mathf.Clamp01(dimLevel);
            }
        }
        
        /// <summary>
        /// Gets the current dim level.
        /// </summary>
        /// <returns>Current dim level from 0 to 1.</returns>
        public float GetDimLevel()
        {
            return _dimSlider != null ? _dimSlider.value : _defaultDim;
        }
        
        /// <summary>
        /// Resets the dim level to default.
        /// </summary>
        public void ResetToDefault()
        {
            SetDimLevel(_defaultDim);
        }
        
        private void OnDestroy()
        {
            if (_dimSlider != null)
            {
                _dimSlider.onValueChanged.RemoveListener(OnDimSliderChanged);
            }
            
            if (_toggleBackgroundButton != null)
            {
                _toggleBackgroundButton.onClick.RemoveListener(OnToggleBackgroundClicked);
            }
        }
    }
}

