using UnityEngine;
using UnityEngine.UI;

namespace AshTaiko.Menu
{
    /// <summary>
    /// Debug helper for troubleshooting the song selection system.
    /// Add this to your song selection scene to get detailed logging.
    /// </summary>
    public class SongSelectionDebugHelper : MonoBehaviour
    {
        [Header("Debug Controls")]
        [SerializeField] private Button _testSongSelectionButton;
        [SerializeField] private Button _testDifficultySelectionButton;
        [SerializeField] private Button _testPlayButton;
        
        [Header("References")]
        [SerializeField] private SongSelectionManager _songSelectionManager;
        
        private void Start()
        {
            if (_testSongSelectionButton != null)
                _testSongSelectionButton.onClick.AddListener(TestSongSelection);
            
            if (_testDifficultySelectionButton != null)
                _testDifficultySelectionButton.onClick.AddListener(TestDifficultySelection);
            
            if (_testPlayButton != null)
                _testPlayButton.onClick.AddListener(TestPlayButton);
        }
        
        private void TestSongSelection()
        {
            Debug.Log("=== TESTING SONG SELECTION ===");
            
            if (_songSelectionManager == null)
            {
                Debug.LogError("SongSelectionManager reference is null!");
                return;
            }
            
            var selectedSong = _songSelectionManager.GetSelectedSong();
            var selectedChart = _songSelectionManager.GetSelectedChart();
            
            Debug.Log($"Selected Song: {selectedSong?.Title ?? "null"}");
            Debug.Log($"Selected Chart: {selectedChart?.Version ?? "null"}");
            Debug.Log("================================");
        }
        
        private void TestDifficultySelection()
        {
            Debug.Log("=== TESTING DIFFICULTY SELECTION ===");
            
            if (_songSelectionManager == null)
            {
                Debug.LogError("SongSelectionManager reference is null!");
                return;
            }
            
            var selectedSong = _songSelectionManager.GetSelectedSong();
            if (selectedSong != null)
            {
                Debug.Log($"Current Song: {selectedSong.Title}");
                Debug.Log($"Available Charts: {selectedSong.Charts?.Count ?? 0}");
                
                if (selectedSong.Charts != null)
                {
                    foreach (var chart in selectedSong.Charts)
                    {
                        Debug.Log($"  - {chart.Version} ({chart.Difficulty}): {chart.HitObjects?.Count ?? 0} notes");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No song is currently selected!");
            }
            
            Debug.Log("================================");
        }
        
        private void TestPlayButton()
        {
            Debug.Log("=== TESTING PLAY BUTTON ===");
            
            if (_songSelectionManager == null)
            {
                Debug.LogError("SongSelectionManager reference is null!");
                return;
            }
            
            var selectedSong = _songSelectionManager.GetSelectedSong();
            var selectedChart = _songSelectionManager.GetSelectedChart();
            
            if (selectedSong != null && selectedChart != null)
            {
                Debug.Log($"Ready to play: {selectedSong.Title} - {selectedChart.Version}");
                Debug.Log("You can now click the actual Play button to start the game!");
            }
            else
            {
                Debug.LogWarning("Cannot play - missing song or chart selection!");
                Debug.LogWarning($"Song: {selectedSong?.Title ?? "null"}");
                Debug.LogWarning($"Chart: {selectedChart?.Version ?? "null"}");
            }
            
            Debug.Log("================================");
        }
        
        private void OnDestroy()
        {
            if (_testSongSelectionButton != null)
                _testSongSelectionButton.onClick.RemoveListener(TestSongSelection);
            
            if (_testDifficultySelectionButton != null)
                _testDifficultySelectionButton.onClick.RemoveListener(TestDifficultySelection);
            
            if (_testPlayButton != null)
                _testPlayButton.onClick.RemoveListener(TestPlayButton);
        }
    }
}

