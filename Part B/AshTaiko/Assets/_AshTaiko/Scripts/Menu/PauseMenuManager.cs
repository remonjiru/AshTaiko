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
        [SerializeField] private GameObject _pauseMenuPanel;
        [SerializeField] private GameObject _pauseMenuButtons;
        [SerializeField] private GameObject _confirmQuitPanel;
        
        [Header("Game References")]
        [SerializeField] private GameManager _gameManager;

        #endregion

        #region Private Fields

        private bool _isPaused = false;
        private bool _isConfirmQuitOpen = false;

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
        }

        /// <summary>
        /// Resumes the game and hides the pause menu.
        /// </summary>
        public void ResumeGame()
        {
            if (!_isPaused) return;

            _isPaused = false;
            Time.timeScale = 1f;
            
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
