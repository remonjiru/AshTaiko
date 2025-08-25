using System;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Manages data persistence between scenes for the song selection system.
    /// This allows song and chart data to be passed from the menu scene to the game scene.
    /// </summary>
    public static class GameDataManager
    {
        // Static variables to store song selection data across scene transitions
        private static SongEntry _selectedSong;
        private static ChartData _selectedChart;
        private static bool _hasSongData = false;

        /// <summary>
        /// Sets the selected song and chart data for scene transition.
        /// </summary>
        /// <param name="song">The selected song entry.</param>
        /// <param name="chart">The selected chart data.</param>
        public static void SetSelectedSong(SongEntry song, ChartData chart)
        {
            Debug.Log("=== GAMEDATAMANAGER: SETTING SONG DATA ===");
            Debug.Log($"Song: {song?.Title ?? "null"} - {song?.Artist ?? "null"}");
            Debug.Log($"Chart: {chart?.Version ?? "null"} - {chart?.Difficulty}");
            Debug.Log($"Audio: {song?.AudioFilename ?? "null"}");
            
            _selectedSong = song;
            _selectedChart = chart;
            _hasSongData = true;
            
            Debug.Log($"GameDataManager: Song data set successfully - {song?.Title ?? "null"} - {chart?.Version ?? "null"}");
            Debug.Log("=== END SETTING SONG DATA ===");
        }

        /// <summary>
        /// Gets the selected song data.
        /// </summary>
        /// <returns>The selected song entry, or null if none is set.</returns>
        public static SongEntry GetSelectedSong()
        {
            Debug.Log($"GameDataManager: GetSelectedSong called - returning {_selectedSong?.Title ?? "null"}");
            return _selectedSong;
        }

        /// <summary>
        /// Gets the selected chart data.
        /// </summary>
        /// <returns>The selected chart data, or null if none is set.</returns>
        public static ChartData GetSelectedChart()
        {
            Debug.Log($"GameDataManager: GetSelectedChart called - returning {_selectedChart?.Version ?? "null"}");
            return _selectedChart;
        }

        /// <summary>
        /// Checks if song data is available.
        /// </summary>
        /// <returns>True if song data is available, false otherwise.</returns>
        public static bool HasSongData()
        {
            Debug.Log($"GameDataManager: HasSongData called - returning {_hasSongData}");
            return _hasSongData;
        }

        /// <summary>
        /// Clears the stored song data.
        /// </summary>
        public static void ClearSongData()
        {
            _selectedSong = null;
            _selectedChart = null;
            _hasSongData = false;
            
            Debug.Log("GameDataManager: Song data cleared");
        }

        /// <summary>
        /// Gets a formatted string describing the current song selection.
        /// </summary>
        /// <returns>A formatted string with song and chart information.</returns>
        public static string GetSongSelectionInfo()
        {
            if (!_hasSongData)
            {
                return "No song selected";
            }
            
            return $"{_selectedSong?.Title ?? "Unknown"} - {_selectedChart?.Version ?? "Unknown"} ({_selectedChart?.Difficulty ?? Difficulty.Normal})";
        }
    }
}
