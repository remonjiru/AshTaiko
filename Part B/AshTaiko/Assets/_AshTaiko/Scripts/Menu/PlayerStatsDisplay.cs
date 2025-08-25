using UnityEngine;
using TMPro;

namespace AshTaiko
{
    /// <summary>
    /// Displays player statistics when the pause menu is shown.
    /// Updates only when the pause menu becomes visible to show current player performance.
    /// </summary>
    public class PlayerStatsDisplay : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI Components")]
        [SerializeField] 
        private TextMeshProUGUI _statsText;

        [Header("Display Settings")]
        [SerializeField] 
        private bool _showProgress = true;
        [SerializeField] 
        private bool _showCombo = true;
        [SerializeField] 
        private bool _showAccuracy = true;

        #endregion

        #region Private Fields

        private GameManager _gameManager;
        private PauseMenuManager _pauseMenuManager;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _gameManager = GameManager.Instance;
            if (_gameManager == null)
            {
                Debug.LogError("PlayerStatsDisplay: GameManager not found!");
                return;
            }

            // Find PauseMenuManager to listen for pause state changes
            _pauseMenuManager = FindObjectOfType<PauseMenuManager>();
            if (_pauseMenuManager == null)
            {
                Debug.LogError("PlayerStatsDisplay: PauseMenuManager not found!");
                return;
            }

            // Subscribe to pause menu events
            _pauseMenuManager.OnPauseMenuShown += UpdateStatsDisplay;
        }

        private void OnDestroy()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.OnPauseMenuShown -= UpdateStatsDisplay;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the statistics display with current player data.
        /// Called when the pause menu is shown.
        /// </summary>
        private void UpdateStatsDisplay()
        {
            if (_statsText == null || _gameManager == null) return;

            string formattedText = FormatPlayerStats();
            _statsText.text = formattedText;
        }

        /// <summary>
        /// Formats the player statistics into a readable string.
        /// </summary>
        /// <returns>Formatted statistics string.</returns>
        private string FormatPlayerStats()
        {
            if (_gameManager == null) return "No game data available";

            var stats = new System.Text.StringBuilder();

            // Song Progress
            if (_showProgress)
            {
                float progressPercentage = _gameManager.GetSongProgressPercentage();
                stats.Append($"Song is {progressPercentage:F1}% complete!");
            }

            // Combo
            if (_showCombo)
            {
                if (stats.Length > 0) stats.Append(" ");
                int combo = _gameManager.Combo;
                stats.Append($"Your combo is {combo}x.");
            }

            // Accuracy
            if (_showAccuracy)
            {
                if (stats.Length > 0) stats.Append(" ");
                float accuracy = _gameManager.Accuracy;
                stats.Append($"Your accuracy is {accuracy:F2}%");
            }

            return stats.ToString();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces an immediate update of the statistics display.
        /// Useful for manual updates if needed.
        /// </summary>
        public void ForceUpdate()
        {
            UpdateStatsDisplay();
        }

        /// <summary>
        /// Sets whether to show the progress percentage.
        /// </summary>
        /// <param name="show">Whether to show progress.</param>
        public void SetShowProgress(bool show)
        {
            _showProgress = show;
        }

        /// <summary>
        /// Sets whether to show the combo counter.
        /// </summary>
        /// <param name="show">Whether to show combo.</param>
        public void SetShowCombo(bool show)
        {
            _showCombo = show;
        }

        /// <summary>
        /// Sets whether to show the accuracy percentage.
        /// </summary>
        /// <param name="show">Whether to show accuracy.</param>
        public void SetShowAccuracy(bool show)
        {
            _showAccuracy = show;
        }

        #endregion
    }
}
