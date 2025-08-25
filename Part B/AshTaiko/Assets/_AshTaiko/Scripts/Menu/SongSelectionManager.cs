using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

namespace AshTaiko.Menu
{
    /*
        SongSelectionManager serves as the main orchestrator for the song selection screen.
        This class coordinates between SongListManager, DifficultySelector, SongInfoDisplay,
        and LeaderboardManager to provide a cohesive user experience.
        
        Architecture Design:
        Implements composition over inheritance by delegating specific responsibilities
        to specialized manager classes. This promotes loose coupling and easier testing.
        Uses event-driven communication between components for clean separation of concerns.
    */

    public class SongSelectionManager : MonoBehaviour
    {
        #region Component References

        [Header("Component Managers")]
        [SerializeField] private SongListManager _songListManager;
        [SerializeField] private DifficultySelector _difficultySelector;
        [SerializeField] private SongInfoDisplay _songInfoDisplay;
        [SerializeField] private LeaderboardManager _leaderboardManager;

        [Header("Main UI")]
        [SerializeField] private Button _backButton;
        
        [Header("Background System")]
        [SerializeField] private UnityEngine.UI.Image _backgroundImage;
        [SerializeField] private UnityEngine.UI.Image _backgroundOverlay;
        [SerializeField] private float _backgroundDim = 0.8f;

        #endregion

        #region Private Fields

        /*
            Current selection state tracking for coordination between components.
            These fields maintain the overall state of the song selection process.
        */
        private SongEntry _selectedSong;
        private ChartData _selectedChart;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateComponentReferences();
        }

        private void Start()
        {
            InitializeComponents();
            SetupEventHandlers();
            LoadInitialData();
        }

        #endregion

        #region Initialization

        /*
            ValidateComponentReferences ensures all required components are properly assigned.
            This prevents runtime errors and provides clear feedback during development.
        */
        private void ValidateComponentReferences()
        {
            if (_songListManager == null)
                Debug.LogError("SongSelectionManager: SongListManager reference is missing!");
            
            if (_difficultySelector == null)
                Debug.LogError("SongSelectionManager: DifficultySelector reference is missing!");
            
            if (_songInfoDisplay == null)
                Debug.LogError("SongSelectionManager: SongInfoDisplay reference is missing!");
            
            if (_leaderboardManager == null)
                Debug.LogError("SongSelectionManager: LeaderboardManager reference is missing!");
            
            if (_backButton == null)
                Debug.LogError("SongSelectionManager: BackButton reference is missing!");
            
            if (_backgroundImage == null)
                Debug.LogWarning("SongSelectionManager: BackgroundImage reference is missing - background system will be disabled");
            
            if (_backgroundOverlay == null)
                Debug.LogWarning("SongSelectionManager: BackgroundOverlay reference is missing - dimming system will be disabled");
        }

        /*
            InitializeComponents sets up the initial state of all component managers.
            This ensures proper coordination between different parts of the system.
        */
        private void InitializeComponents()
        {
            if (_songListManager != null)
                _songListManager.InitializeUI();
            
            if (_difficultySelector != null)
                _difficultySelector.ClearDifficultyButtons();
            
            if (_songInfoDisplay != null)
                _songInfoDisplay.ClearSongInfoDisplay();
            
            if (_leaderboardManager != null)
                _leaderboardManager.ClearLeaderboard();
        }

        /*
            SetupEventHandlers connects all component events to appropriate response methods.
            This creates the communication network between different managers.
        */
        private void SetupEventHandlers()
        {
            // Song list events
            if (_songListManager != null)
            {
                _songListManager.OnSongSelected += OnSongSelected;
                _songListManager.OnSongHovered += OnSongHovered;
            }

            // Difficulty selection events
            if (_difficultySelector != null)
            {
                _difficultySelector.OnDifficultySelected += OnDifficultySelected;
                _difficultySelector.OnPlayButtonClicked += OnPlayButtonClicked;
                _difficultySelector.OnBackButtonClicked += OnDifficultyBackButtonClicked;
            }

            // Button events
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackButtonClicked);
        }

        /*
            LoadInitialData populates the song list and sets up initial UI state.
            This provides immediate user interaction without waiting for user input.
        */
        private void LoadInitialData()
        {
            if (_songListManager != null)
            {
                _songListManager.LoadSongDatabase();
                _songListManager.RefreshSongList();
            }
        }

        #endregion

        #region Event Handlers

        /*
            OnSongSelected responds to song selection events from the SongListManager.
            This method coordinates the update of difficulty buttons, song info, and leaderboard.
        */
        private void OnSongSelected(SongEntry song)
        {
            if (song == null)
            {
                Debug.LogError("Attempted to select null song!");
                return;
            }

            _selectedSong = song;
            _selectedChart = null;
            Debug.Log($"Song selected: {song.Title} - {song.Artist} (Charts: {song.Charts?.Count ?? 0})");

            // Load the song's background image
            LoadSongBackground(song);

            // Update difficulty selector
            if (_difficultySelector != null)
            {
                Debug.Log("Updating difficulty selector...");
                _difficultySelector.UpdateDifficultyButtons(song);
            }
            else
            {
                Debug.LogError("DifficultySelector is null!");
            }

            // Update song info display
            if (_songInfoDisplay != null)
                _songInfoDisplay.UpdateSongInfo(song, null);

            // Clear leaderboard until difficulty is selected
            if (_leaderboardManager != null)
                _leaderboardManager.ClearLeaderboard();

            // Play button is now handled by the difficulty selector
        }

        /*
            OnDifficultySelected responds to difficulty selection events from the DifficultySelector.
            This method updates the song info display and leaderboard with chart-specific data.
        */
        private void OnDifficultySelected(ChartData chart)
        {
            if (chart == null)
            {
                Debug.LogError("Attempted to select null chart!");
                return;
            }

            _selectedChart = chart;
            Debug.Log($"Difficulty selected: {chart.Version} ({chart.Difficulty}) for song: {_selectedSong?.Title ?? "Unknown"}");

            // Update song info display with chart data
            if (_songInfoDisplay != null)
                _songInfoDisplay.UpdateSongInfo(_selectedSong, chart);

            // Update leaderboard for this specific chart
            if (_leaderboardManager != null)
                _leaderboardManager.UpdateLeaderboard(_selectedSong, chart);

            // Play button is now handled by the difficulty selector
        }

        /// <summary>
        /// Handles the main play action when both song and difficulty are selected.
        /// This method transitions to the game scene with the selected song data.
        /// </summary>
        /// <param name="song">The selected song entry.</param>
        /// <param name="chart">The selected chart data.</param>
        private void OnPlayButtonClicked(SongEntry song, ChartData chart)
        {
            Debug.Log("Play button clicked!");
            
            if (song == null || chart == null)
            {
                Debug.LogWarning("Cannot start game: Song or difficulty not selected");
                Debug.LogWarning($"Selected song: {song?.Title ?? "null"}");
                Debug.LogWarning($"Selected chart: {chart?.Version ?? "null"}");
                return;
            }

            Debug.Log($"Starting game with: {song.Title} - {chart.Version} ({chart.Difficulty})");
            
            // Update the current selection
            _selectedSong = song;
            _selectedChart = chart;
            
            // Load the selected song and chart into the game
            LoadSongIntoGame();
        }
        
        /*
            OnDifficultyBackButtonClicked responds to back button clicks from the DifficultySelector.
            This method closes the difficulty panel while keeping the song selected.
        */
        /// <summary>
        /// Responds to back button clicks from the DifficultySelector.
        /// This method closes the difficulty panel while keeping the song selected.
        /// </summary>
        private void OnDifficultyBackButtonClicked()
        {
            Debug.Log("Difficulty back button clicked - closing difficulty panel");
            
            // Just hide the difficulty panel, don't clear the song selection
            if (_difficultySelector != null)
            {
                _difficultySelector.HideDifficultyPanelOnly();
            }
            
            // Clear the selected chart but keep the song
            _selectedChart = null;
            
            // Restore the song's background image
            if (_selectedSong != null)
            {
                LoadSongBackground(_selectedSong);
            }
            
            // Update song info display to show only song info (no chart)
            if (_songInfoDisplay != null && _selectedSong != null)
            {
                _songInfoDisplay.UpdateSongInfo(_selectedSong, null);
            }
            
            // Clear leaderboard since no difficulty is selected
            if (_leaderboardManager != null)
            {
                _leaderboardManager.ClearLeaderboard();
            }
        }

        /*
            OnBackButtonClicked handles navigation back to the main menu.
            This provides a clear exit path for users.
        */
        private void OnBackButtonClicked()
        {
            // Return to main menu
            ReturnToMainMenu();
        }

        #endregion

        #region Game Management

        /*
            LoadSongIntoGame prepares the game scene with the selected song data.
            This method coordinates the transition from menu to gameplay.
        */
        private void LoadSongIntoGame()
        {
            Debug.Log("=== LOADING SONG INTO GAME ===");
            Debug.Log($"Selected song: {_selectedSong?.Title ?? "null"} - {_selectedSong?.Artist ?? "null"}");
            Debug.Log($"Selected chart: {_selectedChart?.Version ?? "null"} - {_selectedChart?.Difficulty}");
            Debug.Log($"Audio filename: {_selectedSong?.AudioFilename ?? "null"}");
            
            // Store the selected song data in a static variable that persists across scenes
            // The GameManager in the GameScene will access this data
            GameDataManager.SetSelectedSong(_selectedSong, _selectedChart);
            
            Debug.Log($"Song data stored for scene transition: {_selectedSong?.Title} - {_selectedChart?.Version}");
            
            // Load the game scene
            LoadGameScene();
        }

        /*
            ReturnToMainMenu handles navigation back to the main menu UI.
            This provides a consistent navigation pattern within the same scene.
        */
        private void ReturnToMainMenu()
        {
            // Return to main menu UI (same scene)
            LoadMainMenuScene();
        }

        #endregion

        #region UI State Management



        #endregion

        #region Public Interface

        /*
            GetSelectedSong provides external access to the currently selected song.
            This allows other systems to query the current selection state.
        */
        public SongEntry GetSelectedSong()
        {
            return _selectedSong;
        }

        /*
            GetSelectedChart provides external access to the currently selected chart.
            This allows other systems to query the current selection state.
        */
        public ChartData GetSelectedChart()
        {
            return _selectedChart;
        }

        /*
            RefreshDatabase triggers a reload of the song database.
            This allows for runtime updates when new songs are added.
        */
        public void RefreshDatabase()
        {
            if (_songListManager != null)
            {
                _songListManager.RefreshDatabase();
            }
        }

        /*
            ClearSelection resets all selection state and UI components.
            This provides a clean slate for new user interactions.
        */
        public void ClearSelection()
        {
            _selectedSong = null;
            _selectedChart = null;

            // Set default background
            SetDefaultBackground();

            if (_difficultySelector != null)
                _difficultySelector.ClearDifficultyButtons();
            
            if (_songInfoDisplay != null)
                _songInfoDisplay.ClearSongInfoDisplay();
            
            if (_leaderboardManager != null)
                _leaderboardManager.ClearLeaderboard();
        }

        #endregion

        #region Scene Management

        /// <summary>
        /// Transitions to the gameplay scene with the selected song and chart.
        /// </summary>
        private void LoadGameScene()
        {
            try
            {
                Debug.Log($"Loading game scene with: {_selectedSong?.Title} - {_selectedChart?.Version}");
                
                // Load the game scene by name
                // Make sure you have a scene named "GameScene" in your build settings
                UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load game scene: {e.Message}");
                
                // Fallback: try to find a scene with "Game" in the name
                var gameScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameScene");
                if (gameScene.IsValid())
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
                }
                else
                {
                    Debug.LogError("GameScene not found in build settings. Please add it to Build Settings > Scenes in Build.");
                }
            }
        }

        /// <summary>
        /// Transitions back to the main menu UI within the same scene.
        /// </summary>
        private void LoadMainMenuScene()
        {
            Debug.Log("Returning to main menu UI");
            
            // Clear current selection
            ClearSelection();
            
            // Hide song selection UI and show main menu UI
            // This assumes there's a MainMenuManager or similar component that can show/hide UI panels
            var mainMenuManager = FindObjectOfType<MainMenuManager>();
            if (mainMenuManager != null)
            {
                mainMenuManager.GoToMain();
                Debug.Log("Main menu UI activated");
            }
            else
            {
                Debug.LogWarning("MainMenuManager not found - cannot return to main menu UI");
            }
        }

        #endregion

        #region Background Management

        /// <summary>
        /// Handles background changes when songs are hovered during selection.
        /// </summary>
        /// <param name="song">The song being hovered, or null if no song is hovered.</param>
        private void OnSongHovered(SongEntry song)
        {
            if (song == null)
            {
                // No song hovered - show default background
                SetDefaultBackground();
                return;
            }

            // Only change background if we're not in difficulty selection mode
            if (_difficultySelector != null && _difficultySelector.HasChartsAvailable())
            {
                // In difficulty selection mode - keep current background
                return;
            }

            // Load the song's background image
            LoadSongBackground(song);
        }

        /// <summary>
        /// Loads and displays the background image for a specific song.
        /// </summary>
        /// <param name="song">The song to load the background for.</param>
        private void LoadSongBackground(SongEntry song)
        {
            if (_backgroundImage == null) return;

            if (song.HasImage())
            {
                string imagePath = song.GetBestAvailableImagePath();
                if (!string.IsNullOrEmpty(imagePath))
                {
                    StartCoroutine(LoadBackgroundImageCoroutine(imagePath));
                }
                else
                {
                    SetDefaultBackground();
                }
            }
            else
            {
                SetDefaultBackground();
            }
        }

        /// <summary>
        /// Coroutine to load background image asynchronously.
        /// </summary>
        /// <param name="imagePath">Path to the background image file.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private System.Collections.IEnumerator LoadBackgroundImageCoroutine(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) yield break;

            // Check if file exists
            if (!System.IO.File.Exists(imagePath))
            {
                SetDefaultBackground();
                yield break;
            }

            // Load image using UnityWebRequest
            string fullPath = "file://" + imagePath;
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(fullPath))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    if (texture != null)
                    {
                        // Create sprite from texture
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        
                        // Apply to background image
                        _backgroundImage.sprite = sprite;
                        _backgroundImage.color = Color.white;
                        
                        // Configure background image for proper display
                        ConfigureBackgroundImage(texture);
                        
                        // Apply dimming overlay
                        UpdateBackgroundDim();
                    }
                    else
                    {
                        SetDefaultBackground();
                    }
                }
                else
                {
                    SetDefaultBackground();
                }
            }
        }

        /// <summary>
        /// Configures the background image for proper display.
        /// </summary>
        /// <param name="texture">The loaded background texture.</param>
        private void ConfigureBackgroundImage(Texture2D texture)
        {
            if (_backgroundImage == null || texture == null) return;

            // Set the image type for proper scaling
            _backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
            _backgroundImage.preserveAspect = false;

            // Get the RectTransform of the background image
            RectTransform imageRect = _backgroundImage.rectTransform;
            if (imageRect == null) return;

            // Get the parent container's size (should be full screen)
            RectTransform parentRect = imageRect.parent as RectTransform;
            if (parentRect == null) return;

            Vector2 parentSize = parentRect.rect.size;
            
            // Calculate how the image should be sized to fit within the screen
            float textureAspect = (float)texture.width / texture.height;
            float screenAspect = parentSize.x / parentSize.y;
            
            Vector2 newSize;
            
            if (textureAspect > screenAspect)
            {
                // Texture is wider than screen - fit to width, may crop height
                newSize = new Vector2(parentSize.x, parentSize.x / textureAspect);
            }
            else
            {
                // Texture is taller than screen - fit to height, may crop width
                newSize = new Vector2(parentSize.y * textureAspect, parentSize.y);
            }
            
            // Ensure the image doesn't exceed screen boundaries
            if (newSize.x > parentSize.x)
            {
                newSize.x = parentSize.x;
                newSize.y = newSize.x / textureAspect;
            }
            
            if (newSize.y > parentSize.y)
            {
                newSize.y = parentSize.y;
                newSize.x = newSize.y * textureAspect;
            }
            
            // Set the size to fit within the screen while maintaining aspect ratio
            imageRect.sizeDelta = newSize;
            
            // Center the image
            imageRect.anchoredPosition = Vector2.zero;
        }

        /// <summary>
        /// Sets a default background when no image is available.
        /// </summary>
        private void SetDefaultBackground()
        {
            if (_backgroundImage == null) return;

            // Clear the sprite and set a default color
            _backgroundImage.sprite = null;
            _backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 1f); // Dark blue-gray
            
            // Apply dimming overlay
            UpdateBackgroundDim();
        }

        /// <summary>
        /// Updates the background dimming overlay.
        /// </summary>
        private void UpdateBackgroundDim()
        {
            if (_backgroundOverlay == null) return;

            // Set the overlay color based on dim level
            Color overlayColor = new Color(0f, 0f, 0f, _backgroundDim);
            _backgroundOverlay.color = overlayColor;
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Clean up event handlers to prevent memory leaks
            if (_songListManager != null)
            {
                _songListManager.OnSongSelected -= OnSongSelected;
                _songListManager.OnSongHovered -= OnSongHovered;
            }
            
            if (_difficultySelector != null)
                _difficultySelector.OnDifficultySelected -= OnDifficultySelected;
            
            if (_backButton != null)
                _backButton.onClick.RemoveListener(OnBackButtonClicked);
        }

        #endregion
    }
}
