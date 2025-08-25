using UnityEngine;
using UnityEngine.SceneManagement;

namespace AshTaiko
{
    /// <summary>
    /// Manages the pause menu functionality during gameplay.
    /// Handles pause state, menu visibility, and navigation between pause menu options.
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Menu References")]
        [SerializeField] 
        private GameObject _pauseMenuPanel;
        [SerializeField] 
        private GameObject _pauseMenuButtons;
        [SerializeField] 
        private GameObject _confirmQuitPanel;
        
        [Header("Game References")]
        [SerializeField] 
        private GameManager _gameManager;
        [SerializeField] 
        private SongManager _songManager;

        #endregion

        #region Private Fields

        private bool _isPaused = false;
        private bool _isConfirmQuitOpen = false;

        // Event for when pause menu is shown
        public event System.Action OnPauseMenuShown;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Ensure pause menu is hidden at start
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(false);
            }
            
            if (_confirmQuitPanel != null)
            {
                _confirmQuitPanel.SetActive(false);
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Toggles the pause state of the game.
        /// </summary>
        public void TogglePause()
        {
            if (_isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

        /// <summary>
        /// Pauses the game and shows the pause menu.
        /// </summary>
        public void PauseGame()
        {
            if (_isPaused) return;

            _isPaused = true;
            Time.timeScale = 0f;
            
            // Pause the audio system
            if (_songManager != null)
            {
                _songManager.PauseAudio();
            }
            
            // Notify GameManager of pause state
            if (_gameManager != null)
            {
                _gameManager.SetPauseState(true);
            }
            
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(true);
            }
            
            if (_pauseMenuButtons != null)
            {
                _pauseMenuButtons.SetActive(true);
            }
            
            if (_confirmQuitPanel != null)
            {
                _confirmQuitPanel.SetActive(false);
            }
            
            _isConfirmQuitOpen = false;

            // Notify that pause menu is shown
            OnPauseMenuShown?.Invoke();
        }

        /// <summary>
        /// Resumes the game and hides the pause menu.
        /// </summary>
        public void ResumeGame()
        {
            if (!_isPaused) return;

            _isPaused = false;
            Time.timeScale = 1f;
            
            // Resume the audio system
            if (_songManager != null)
            {
                _songManager.ResumeAudio();
            }
            
            // Notify GameManager of resume state
            if (_gameManager != null)
            {
                _gameManager.SetPauseState(false);
            }
            
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Shows the confirm quit panel.
        /// </summary>
        public void ShowConfirmQuit()
        {
            if (_pauseMenuButtons != null)
            {
                _pauseMenuButtons.SetActive(false);
            }
            
            if (_confirmQuitPanel != null)
            {
                _confirmQuitPanel.SetActive(true);
            }
            
            _isConfirmQuitOpen = true;
        }

        /// <summary>
        /// Hides the confirm quit panel and returns to pause menu.
        /// </summary>
        public void HideConfirmQuit()
        {
            if (_pauseMenuButtons != null)
            {
                _pauseMenuButtons.SetActive(true);
            }
            
            if (_confirmQuitPanel != null)
            {
                _confirmQuitPanel.SetActive(false);
            }
            
            _isConfirmQuitOpen = false;
        }

        /// <summary>
        /// Returns to the main menu scene.
        /// </summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Menu");
        }

        /// <summary>
        /// Retries the current song/chart.
        /// </summary>
        public void RetryGame()
        {
            Debug.Log("Retry button clicked - restarting current song");
            
            // Resume time scale before restarting
            Time.timeScale = 1f;
            
            // Hide pause menu
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.SetActive(false);
            }
            
            // Reset pause state
            _isPaused = false;
            
            // Notify GameManager to restart the current song
            if (_gameManager != null)
            {
                _gameManager.SetPauseState(false);
                _gameManager.RestartCurrentSong();
            }
            else
            {
                Debug.LogError("GameManager reference is null - cannot restart song");
            }
        }

        /// <summary>
        /// Quits the game application.
        /// </summary>
        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the game is currently paused.
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Gets whether the confirm quit panel is currently open.
        /// </summary>
        public bool IsConfirmQuitOpen => _isConfirmQuitOpen;

        #endregion
    }
}
