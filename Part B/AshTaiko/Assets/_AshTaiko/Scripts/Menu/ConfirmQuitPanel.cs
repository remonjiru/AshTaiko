using UnityEngine;
using UnityEngine.UI;

namespace AshTaiko
{
    /// <summary>
    /// Handles the confirm quit panel interactions.
    /// Provides confirmation dialog before quitting the game.
    /// </summary>
    public class ConfirmQuitPanel : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Button References")]
        [SerializeField] 
        private Button _confirmQuitButton;
        [SerializeField] 
        private Button _cancelButton;

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
            if (_confirmQuitButton != null)
            {
                _confirmQuitButton.onClick.AddListener(OnConfirmQuitClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(OnCancelClicked);
            }
        }

        #endregion

        #region Button Event Handlers

        /// <summary>
        /// Handles confirm quit button click.
        /// </summary>
        private void OnConfirmQuitClicked()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.QuitGame();
            }
        }

        /// <summary>
        /// Handles cancel button click.
        /// </summary>
        private void OnCancelClicked()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.HideConfirmQuit();
            }
        }

        #endregion
    }
}
