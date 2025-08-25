using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AshTaiko.Menu
{
    /// <summary>
    /// Handles the display and management of song leaderboards.
    /// This class provides player performance tracking and competitive elements.
    /// Uses Lists for dynamic leaderboard entry collections to allow runtime updates.
    /// Implements event-driven architecture for leaderboard updates and score display.
    /// Stores leaderboard data with proper cleanup and memory management.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        #region UI References

        [Header("Leaderboard")]
        [SerializeField] 
        private Transform _leaderboardContent;
        [SerializeField] 
        private GameObject _leaderboardEntryPrefab;

        #endregion

        #region Private Fields

        /// <summary>
        /// Leaderboard entry management using Lists for dynamic UI element creation.
        /// This allows for runtime generation of leaderboard entries based on available score data.
        /// </summary>
        private List<GameObject> _leaderboardEntries = new List<GameObject>();

        #endregion

        #region Public Interface

        /// <summary>
        /// Displays player performance data for the selected song and difficulty.
        /// This system provides motivation and competitive elements by showing high scores and rankings.
        /// </summary>
        /// <param name="song">The song to display leaderboard for.</param>
        /// <param name="chart">The chart/difficulty to display leaderboard for.</param>
        public void UpdateLeaderboard(SongEntry song, ChartData chart)
        {
            if (_leaderboardContent == null || _leaderboardEntryPrefab == null) return;

            // Clear existing leaderboard
            ClearLeaderboard();

            if (song == null || chart == null)
            {
                ShowNoScoresMessage();
                return;
            }

            // Get leaderboard data for this song/chart combination
            var leaderboardData = GetLeaderboardData(song.UniqueId, chart.Version);

            if (leaderboardData.Count == 0)
            {
                ShowNoScoresMessage();
                return;
            }

            // Create leaderboard entries
            for (int i = 0; i < leaderboardData.Count; i++)
            {
                CreateLeaderboardEntry(leaderboardData[i], i + 1);
            }
        }

        /// <summary>
        /// Removes all existing leaderboard entries and cleans up references.
        /// This prevents memory leaks and ensures clean UI state management.
        /// </summary>
        public void ClearLeaderboard()
        {
            foreach (var entry in _leaderboardEntries)
            {
                if (entry != null)
                {
                    Destroy(entry);
                }
            }
            _leaderboardEntries.Clear();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Displays a message when no scores are available for the selected song.
        /// This provides clear feedback to users about the current leaderboard state.
        /// </summary>
        private void ShowNoScoresMessage()
        {
            if (_leaderboardContent == null) return;

            GameObject messageObj = new GameObject("NoScoresMessage");
            messageObj.transform.SetParent(_leaderboardContent, false);

            var textComponent = messageObj.AddComponent<TextMeshProUGUI>();
            textComponent.text = "No scores recorded for this song yet.\nBe the first to set a record!";
            textComponent.fontSize = 16;
            textComponent.color = Color.gray;
            textComponent.alignment = TextAlignmentOptions.Center;

            // Position the message
            var rectTransform = messageObj.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.sizeDelta = new Vector2(300, 100);
            }

            _leaderboardEntries.Add(messageObj);
        }

        /// <summary>
        /// Instantiates and configures a single leaderboard entry.
        /// Each entry displays player performance information in a structured format.
        /// </summary>
        /// <param name="entry">The leaderboard entry data to display.</param>
        /// <param name="rank">The rank position of this entry.</param>
        private void CreateLeaderboardEntry(LeaderboardEntry entry, int rank)
        {
            GameObject entryObj = Instantiate(_leaderboardEntryPrefab, _leaderboardContent);
            
            // Find and configure UI components
            var rankText = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            var playerNameText = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            var scoreText = entryObj.GetComponentInChildren<TextMeshProUGUI>();
            var accuracyText = entryObj.GetComponentInChildren<TextMeshProUGUI>();

            if (rankText != null)
                rankText.text = $"#{rank}";
            
            if (playerNameText != null)
                playerNameText.text = entry.PlayerName ?? "Unknown Player";
            
            if (scoreText != null)
                scoreText.text = FormatScore(entry.Score);
            
            if (accuracyText != null)
                accuracyText.text = $"{entry.Accuracy:F1}%";

            _leaderboardEntries.Add(entryObj);
        }

        /// <summary>
        /// Retrieves leaderboard information for a specific song and difficulty.
        /// This method provides sample data for demonstration purposes.
        /// </summary>
        /// <param name="songId">The unique identifier of the song.</param>
        /// <param name="chartVersion">The version/difficulty of the chart.</param>
        /// <returns>A list of leaderboard entries for the specified song and chart.</returns>
        private List<LeaderboardEntry> GetLeaderboardData(string songId, string chartVersion)
        {
            // For now, return sample data
            // In a real implementation, this would query a persistent database
            var leaderboardData = new List<LeaderboardEntry>();

            // Sample entries
            leaderboardData.Add(new LeaderboardEntry
            {
                PlayerName = "TaikoMaster",
                Score = 985000,
                Accuracy = 98.5f,
                MaxCombo = 450,
                Date = System.DateTime.Now.AddDays(-1)
            });

            leaderboardData.Add(new LeaderboardEntry
            {
                PlayerName = "RhythmKing",
                Score = 972000,
                Accuracy = 97.2f,
                MaxCombo = 420,
                Date = System.DateTime.Now.AddDays(-2)
            });

            leaderboardData.Add(new LeaderboardEntry
            {
                PlayerName = "BeatMaster",
                Score = 958000,
                Accuracy = 95.8f,
                MaxCombo = 398,
                Date = System.DateTime.Now.AddDays(-3)
            });

            // Sort by score (highest first)
            leaderboardData.Sort((a, b) => b.Score.CompareTo(a.Score));

            return leaderboardData;
        }

        /// <summary>
        /// Converts raw score values to human-readable format with separators.
        /// This improves readability for large score values.
        /// </summary>
        /// <param name="score">The raw score value to format.</param>
        /// <returns>A formatted string representation of the score.</returns>
        private string FormatScore(int score)
        {
            return score.ToString("N0");
        }

        #endregion

        #region Data Structures

        /// <summary>
        /// Represents a single player's performance record.
        /// This structure stores all relevant information for leaderboard display.
        /// </summary>
        [System.Serializable]
        public class LeaderboardEntry
        {
            public string PlayerName;
            public int Score;
            public float Accuracy;
            public int MaxCombo;
            public System.DateTime Date;
        }

        #endregion
    }
}

