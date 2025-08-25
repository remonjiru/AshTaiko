using UnityEngine;
using UnityEngine.UI;

namespace AshTaiko
{
    /// <summary>
    /// Handles button interactions for the pause menu.
    /// Connects UI buttons to PauseMenuManager functionality.
    /// </summary>
    public class PauseMenuButtons : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Button References")]
        [SerializeField] 
        private Button _resumeButton;
        [SerializeField] 
        private Button _retryButton;
        [SerializeField] 
        private Button _mainMenuButton;
        [SerializeField] 
        private Button _quitButton;

        [Header("Manager Reference")]
        [SerializeField] 
        private PauseMenuManager _pauseMenuManager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            SetupButtonListeners();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets up button click event listeners.
        /// </summary>
        private void SetupButtonListeners()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (_retryButton != null)
            {
                _retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }

            if (_quitButton != null)
            {
                _quitButton.onClick.AddListener(OnQuitClicked);
            }
        }

        #endregion

        #region Button Event Handlers

        /// <summary>
        /// Handles resume button click.
        /// </summary>
        private void OnResumeClicked()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.ResumeGame();
            }
        }

        /// <summary>
        /// Handles retry button click.
        /// </summary>
        private void OnRetryClicked()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.RetryGame();
            }
        }

        /// <summary>
        /// Handles main menu button click.
        /// </summary>
        private void OnMainMenuClicked()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.ReturnToMainMenu();
            }
        }

        /// <summary>
        /// Handles quit button click.
        /// </summary>
        private void OnQuitClicked()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.ShowConfirmQuit();
            }
        }

        #endregion
    }
}
