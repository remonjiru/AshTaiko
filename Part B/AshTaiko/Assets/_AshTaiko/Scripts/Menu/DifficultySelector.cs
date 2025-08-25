using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AshTaiko.Menu
{
    /*
        DifficultySelector handles the difficulty selection interface for songs.
        This class manages the creation, display, and interaction with difficulty buttons.
        
        Data Structure Design:
        Uses Lists for dynamic difficulty button collections to allow runtime generation.
        Implements event-driven architecture for difficulty selection and UI updates.
        Stores difficulty button references for proper cleanup and memory management.
    */

    public class DifficultySelector : MonoBehaviour
    {
        #region UI References

        [Header("Difficulty Selection")]
        [SerializeField] private Transform _difficultyButtonContainer;
        [SerializeField] private GameObject _difficultyButtonPrefab;
        [SerializeField] private GameObject _difficultyPanel; // The entire difficulty selection panel
        [SerializeField] private Button _playButton; // Play button integrated into difficulty panel
        [SerializeField] private Button _backButton; // Back button to close difficulty panel

        #endregion

        #region Private Fields

        /*
            Difficulty button management using Lists for dynamic UI element creation.
            This allows for runtime generation of difficulty selection buttons based on available charts.
        */
        private List<Button> _difficultyButtons = new List<Button>();
        private List<ChartData> _buttonCharts = new List<ChartData>(); // Maps buttons to charts
        private ChartData _selectedChart;
        private SongEntry _currentSong; // Store current song for context

        #endregion

        #region Events

        /*
            DifficultySelected event provides decoupled communication for difficulty selection.
            This allows other components to react to difficulty selection without direct dependencies.
        */
        public System.Action<ChartData> OnDifficultySelected;
        
        /*
            PlayButtonClicked event provides communication for when the play button is clicked.
            This allows the parent system to handle game launching.
        */
        public System.Action<SongEntry, ChartData> OnPlayButtonClicked;
        
        /*
            BackButtonClicked event provides communication for when the back button is clicked.
            This allows the parent system to handle closing the difficulty panel.
        */
        public System.Action OnBackButtonClicked;

        #endregion

        #region Initialization

        private void Awake()
        {
            // Ensure we have the required prefab
            if (_difficultyButtonPrefab == null)
            {
                Debug.LogError("DifficultyButtonPrefab is not assigned!");
            }
            
            // Hide the difficulty panel initially
            if (_difficultyPanel != null)
            {
                _difficultyPanel.SetActive(false);
            }
            
            // Setup play button if assigned
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(HandlePlayButtonClicked);
                // Initially hide the play button until a difficulty is selected
                _playButton.gameObject.SetActive(false);
            }
            
            // Setup back button if assigned
            if (_backButton != null)
            {
                _backButton.onClick.AddListener(HandleBackButtonClicked);
            }
        }

        #endregion

        #region Difficulty Button Management

        /*
            UpdateDifficultyButtons creates interactive difficulty selection buttons for the selected song.
            This system dynamically generates buttons based on available chart difficulties, providing
            a flexible interface that adapts to any song's chart configuration.
        */
        /// <summary>
        /// Creates interactive difficulty selection buttons for the selected song.
        /// This system dynamically generates buttons based on available chart difficulties.
        /// </summary>
        /// <param name="song">The song entry containing the charts to create buttons for.</param>
        public void UpdateDifficultyButtons(SongEntry song)
        {
            if (_difficultyButtonContainer == null || _difficultyButtonPrefab == null) return;

            // Clear existing difficulty buttons
            ClearDifficultyButtons();

            if (song == null || song.Charts == null || song.Charts.Count == 0)
            {
                Debug.Log("No charts available for difficulty selection");
                HideDifficultyPanel();
                return;
            }

            // Store the current song for context
            _currentSong = song;

            Debug.Log($"Creating {song.Charts.Count} difficulty buttons for song: {song.Title}");
            
            // Create difficulty buttons for each available chart
            foreach (var chart in song.Charts)
            {
                CreateDifficultyButton(chart);
            }

            // Show the difficulty panel
            ShowDifficultyPanel();

            // Select the first difficulty by default
            if (song.Charts.Count > 0)
            {
                Debug.Log($"Auto-selecting first difficulty: {song.Charts[0].Version} ({song.Charts[0].Difficulty})");
                OnDifficultySelected(song.Charts[0]);
            }
        }

        /*
            CreateDifficultyButton instantiates and configures a single difficulty selection button.
            Each button displays the difficulty name and connects to the selection system.
        */
        private void CreateDifficultyButton(ChartData chart)
        {
            GameObject buttonObj = Instantiate(_difficultyButtonPrefab, _difficultyButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (button != null && buttonText != null)
            {
                // Set button text - only show the chart version
                buttonText.text = chart.Version;



                // Add click listener
                button.onClick.AddListener(() => OnDifficultyButtonClicked(chart));

                // Store references for cleanup and mapping
                _difficultyButtons.Add(button);
                _buttonCharts.Add(chart);
            }
        }

        /// <summary>
        /// Removes all existing difficulty buttons and cleans up references.
        /// This prevents memory leaks and ensures clean UI state management.
        /// </summary>
        public void ClearDifficultyButtons()
        {
            foreach (var button in _difficultyButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    Destroy(button.gameObject);
                }
            }
            _difficultyButtons.Clear();
            _buttonCharts.Clear();
        }

        /// <summary>
        /// Handles user selection of a specific difficulty/chart.
        /// This method updates the selection state and notifies other components.
        /// </summary>
        /// <param name="chart">The chart that was selected.</param>
        private void OnDifficultyButtonClicked(ChartData chart)
        {
            if (chart == null)
            {
                Debug.LogError("Attempted to select null chart!");
                return;
            }

            _selectedChart = chart;
            Debug.Log($"Selected difficulty: {chart.Version} ({chart.Difficulty}) - Song: {_currentSong?.Title ?? "Unknown"}");

            // Update difficulty button selection visual state
            UpdateDifficultyButtonSelection(chart);

            // Show the play button since we now have a selected difficulty
            if (_playButton != null)
            {
                _playButton.gameObject.SetActive(true);
            }

            // Notify other components
            OnDifficultySelected?.Invoke(chart);
        }
        
        /// <summary>
        /// Handles the play button click when integrated into the difficulty panel.
        /// This method validates the selection and notifies the parent system.
        /// </summary>
        private void HandlePlayButtonClicked()
        {
            if (_currentSong == null || _selectedChart == null)
            {
                Debug.LogWarning("Cannot play: Song or difficulty not selected");
                return;
            }
            
            Debug.Log($"Play button clicked in difficulty panel: {_currentSong.Title} - {_selectedChart.Version}");
            
            // Notify parent system to handle game launching
            OnPlayButtonClicked?.Invoke(_currentSong, _selectedChart);
        }
        
        /// <summary>
        /// Handles the back button click to close the difficulty panel.
        /// This method notifies the parent system to handle the back action.
        /// </summary>
        private void HandleBackButtonClicked()
        {
            Debug.Log("Back button clicked in difficulty panel");
            
            // Notify parent system to handle closing the difficulty panel
            OnBackButtonClicked?.Invoke();
        }

        /// <summary>
        /// Updates the visual state of difficulty buttons to show which one is selected.
        /// This provides clear feedback about which difficulty is currently selected.
        /// </summary>
        /// <param name="selectedChart">The chart that is currently selected.</param>
        private void UpdateDifficultyButtonSelection(ChartData selectedChart)
        {
            // Note: Visual selection feedback is now handled by the button's default Unity UI behavior
            // The selected state will be managed by Unity's built-in button selection system
            Debug.Log($"Difficulty selection updated: {selectedChart.Version} ({selectedChart.Difficulty})");
        }

        /// <summary>
        /// Retrieves the chart data associated with a specific button.
        /// This is used for visual state management and selection feedback.
        /// </summary>
        /// <param name="button">The button to find the associated chart for.</param>
        /// <returns>The chart data associated with the button, or null if not found.</returns>
        private ChartData GetChartForButton(Button button)
        {
            // Find the button index and return the corresponding chart
            int buttonIndex = _difficultyButtons.IndexOf(button);
            if (buttonIndex >= 0 && buttonIndex < _buttonCharts.Count)
            {
                return _buttonCharts[buttonIndex];
            }
            return null;
        }

        #endregion

        #region Utility Methods





        #endregion

        #region Public Interface

        /// <summary>
        /// Provides access to the currently selected chart.
        /// This allows other components to access the selection state.
        /// </summary>
        /// <returns>The currently selected chart, or null if none is selected.</returns>
        public ChartData GetSelectedChart()
        {
            return _selectedChart;
        }
        
        /// <summary>
        /// Provides access to the current song being displayed.
        /// This allows other components to access the song context.
        /// </summary>
        /// <returns>The current song, or null if none is set.</returns>
        public SongEntry GetCurrentSong()
        {
            return _currentSong;
        }
        
        /// <summary>
        /// Checks if the current song has any charts available for selection.
        /// </summary>
        /// <returns>True if charts are available, false otherwise.</returns>
        public bool HasChartsAvailable()
        {
            return _currentSong != null && _currentSong.Charts != null && _currentSong.Charts.Count > 0;
        }
        
        /// <summary>
        /// Shows the difficulty selection panel.
        /// </summary>
        public void ShowDifficultyPanel()
        {
            if (_difficultyPanel != null)
            {
                _difficultyPanel.SetActive(true);
                Debug.Log("Difficulty panel shown");
            }
        }
        
        /// <summary>
        /// Hides the difficulty selection panel.
        /// </summary>
        public void HideDifficultyPanel()
        {
            if (_difficultyPanel != null)
            {
                _difficultyPanel.SetActive(false);
                Debug.Log("Difficulty panel hidden");
            }
        }
        
        /// <summary>
        /// Hides the difficulty panel without clearing the selection.
        /// This allows users to go back to the difficulty panel later.
        /// </summary>
        public void HideDifficultyPanelOnly()
        {
            if (_difficultyPanel != null)
            {
                _difficultyPanel.SetActive(false);
                Debug.Log("Difficulty panel hidden (selection preserved)");
            }
        }

        /// <summary>
        /// Resets the current difficulty selection.
        /// This provides a clean state for new selections.
        /// </summary>
        public void ClearSelection()
        {
            _selectedChart = null;
            _currentSong = null;
            ClearDifficultyButtons();
            HideDifficultyPanel();
            
            // Hide the play button since no difficulty is selected
            if (_playButton != null)
            {
                _playButton.gameObject.SetActive(false);
            }
        }

        /*
            IsChartSelected checks if a specific chart is currently selected.
            This is useful for UI state management and validation.
        */
        public bool IsChartSelected(ChartData chart)
        {
            return _selectedChart != null && _selectedChart.Version == chart.Version;
        }

        #endregion
        
        #region Cleanup
        
        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(HandlePlayButtonClicked);
            }
            
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackButtonClicked);
            }
        }
        
        #endregion
    }
}
