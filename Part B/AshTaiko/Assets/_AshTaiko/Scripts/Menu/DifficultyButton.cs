using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AshTaiko.Menu
{
    /*
        DifficultyButton represents a single difficulty selection button in the song selection interface.
        This component provides visual feedback and handles user interaction for difficulty selection.
        
        Data Structure Design:
        Uses Button component for standard Unity button functionality and event handling.
        Implements visual state management with color changes and text updates.
        Stores difficulty information for easy access by the parent SongSelectionManager.
    */

    public class DifficultyButton : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Button _button;
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private Image _backgroundImage;
        
        [Header("Visual States")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _hoverColor = Color.cyan;
        
        private ChartData _associatedChart;
        private bool _isSelected = false;
        
        public System.Action<ChartData> OnDifficultySelected;

        private void Awake()
        {
            // Auto-find components if not assigned
            if (_button == null)
                _button = GetComponent<Button>();
                
            if (_buttonText == null)
                _buttonText = GetComponentInChildren<TextMeshProUGUI>();
                
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            // Setup button click event
            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClicked);
            }
        }

        /*
            Initialize sets up the difficulty button with chart data and visual configuration.
            This method is called by the SongSelectionManager when creating difficulty buttons.
        */
        public void Initialize(ChartData chart, Color difficultyColor)
        {
            _associatedChart = chart;
            
            if (_buttonText != null)
            {
                string difficultyName = GetDifficultyDisplayName(chart.Difficulty);
                _buttonText.text = $"{chart.Version}\n({difficultyName})";
            }

            // Set button colors
            if (_button != null)
            {
                ColorBlock colors = _button.colors;
                colors.normalColor = difficultyColor;
                colors.selectedColor = difficultyColor * 1.2f;
                colors.highlightedColor = difficultyColor * 1.1f;
                colors.pressedColor = difficultyColor * 0.9f;
                _button.colors = colors;
            }

            // Set background color
            if (_backgroundImage != null)
            {
                _backgroundImage.color = difficultyColor;
            }
        }

        /*
            SetSelected updates the visual state of the button to show selection.
            This provides clear feedback about which difficulty is currently selected.
        */
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisualState();
        }

        /*
            UpdateVisualState applies the appropriate visual styling based on selection state.
            This ensures consistent visual feedback across all difficulty buttons.
        */
        private void UpdateVisualState()
        {
            if (_buttonText != null)
            {
                _buttonText.color = _isSelected ? _selectedColor : _normalColor;
            }

            if (_backgroundImage != null)
            {
                Color currentColor = _backgroundImage.color;
                _backgroundImage.color = _isSelected ? 
                    new Color(currentColor.r, currentColor.g, currentColor.b, 1f) : 
                    new Color(currentColor.r, currentColor.g, currentColor.b, 0.8f);
            }
        }

        /*
            OnButtonClicked handles user interaction with the difficulty button.
            This triggers the difficulty selection event for the parent system.
        */
        private void OnButtonClicked()
        {
            if (_associatedChart != null)
            {
                OnDifficultySelected?.Invoke(_associatedChart);
            }
        }

        /*
            GetDifficultyDisplayName converts difficulty enums to human-readable names.
            This provides consistent difficulty labeling across the interface.
        */
        private string GetDifficultyDisplayName(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return "Easy";
                case Difficulty.Normal: return "Normal";
                case Difficulty.Hard: return "Hard";
                case Difficulty.Insane: return "Oni/Insane";
                case Difficulty.Expert: return "Expert";
                case Difficulty.Master: return "Master";
                default: return difficulty.ToString();
            }
        }

        /*
            GetAssociatedChart provides access to the chart data associated with this button.
            This allows other components to access difficulty information.
        */
        public ChartData GetAssociatedChart()
        {
            return _associatedChart;
        }

        /*
            IsSelected provides the current selection state of the button.
            This allows other components to check the button's visual state.
        */
        public bool IsSelected()
        {
            return _isSelected;
        }
    }
}

