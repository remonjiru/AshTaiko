using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace AshTaiko.Menu
{
    public class SongSelectionManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform songListContent;
        [SerializeField] private GameObject songItemPrefab;
        [SerializeField] private TMP_InputField searchInput;
        [SerializeField] private Dropdown difficultyFilter;
        [SerializeField] private Button randomButton;
        [SerializeField] private Button scanButton;
        
        [Header("Song Display")]
        [SerializeField] private TextMeshProUGUI currentSongTitle;
        [SerializeField] private TextMeshProUGUI currentSongArtist;
        [SerializeField] private TextMeshProUGUI currentSongCreator;
        [SerializeField] private TextMeshProUGUI currentSongLength;
        [SerializeField] private TextMeshProUGUI currentSongBPM;
        [SerializeField] private Image currentSongBackground;
        
        [Header("Difficulty Display")]
        [SerializeField] private Transform difficultyButtonsParent;
        [SerializeField] private GameObject difficultyButtonPrefab;
        
        private List<SongEntry> allSongs = new List<SongEntry>();
        private List<SongEntry> filteredSongs = new List<SongEntry>();
        private int currentSongIndex = 0;
        private Difficulty selectedDifficulty = Difficulty.Normal;
        
        private void Start()
        {
            InitializeUI();
            LoadSongs();
            UpdateSongDisplay();
        }
        
        private void InitializeUI()
        {
            // Initialize difficulty filter dropdown
            difficultyFilter.ClearOptions();
            difficultyFilter.AddOptions(new List<string> { "All", "Easy", "Normal", "Hard", "Insane", "Expert", "Master" });
            difficultyFilter.onValueChanged.AddListener(OnDifficultyFilterChanged);
            
            // Initialize search input
            searchInput.onValueChanged.AddListener(OnSearchChanged);
            
            // Initialize buttons
            randomButton.onClick.AddListener(SelectRandomSong);
            scanButton.onClick.AddListener(ScanForNewSongs);
            
            // Initialize difficulty buttons
            CreateDifficultyButtons();
        }
        
        private void CreateDifficultyButtons()
        {
            // Clear existing buttons
            foreach (Transform child in difficultyButtonsParent)
            {
                Destroy(child.gameObject);
            }
            
            // Create difficulty buttons
            Difficulty[] difficulties = { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard, Difficulty.Insane, Difficulty.Expert, Difficulty.Master };
            
            foreach (Difficulty diff in difficulties)
            {
                GameObject buttonObj = Instantiate(difficultyButtonPrefab, difficultyButtonsParent);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                if (buttonText != null)
                {
                    buttonText.text = diff.ToString();
                }
                
                Difficulty capturedDiff = diff; // Capture for lambda
                button.onClick.AddListener(() => OnDifficultySelected(capturedDiff));
                
                // Set initial selected difficulty
                if (diff == selectedDifficulty)
                {
                    button.interactable = false;
                }
            }
        }
        
        private void LoadSongs()
        {
            if (ChartDatabase.Instance != null)
            {
                allSongs = ChartDatabase.Instance.GetSongs().ToList();
                filteredSongs = allSongs.ToList();
                RefreshSongList();
            }
        }
        
        private void RefreshSongList()
        {
            // Clear existing song items
            foreach (Transform child in songListContent)
            {
                Destroy(child.gameObject);
            }
            
            // Create song items
            for (int i = 0; i < filteredSongs.Count; i++)
            {
                GameObject songItemObj = Instantiate(songItemPrefab, songListContent);
                SongListItem songItem = songItemObj.GetComponent<SongListItem>();
                
                if (songItem != null)
                {
                    songItem.Initialize(filteredSongs[i], i);
                    songItem.OnSongSelected += OnSongItemSelected;
                }
            }
        }
        
        private void UpdateSongDisplay()
        {
            if (filteredSongs.Count == 0) return;
            
            SongEntry currentSong = filteredSongs[currentSongIndex];
            
            // Update song info
            currentSongTitle.text = currentSong.Title;
            currentSongArtist.text = currentSong.Artist;
            currentSongCreator.text = currentSong.Creator;
            
            // Get the selected difficulty chart
            ChartData currentChart = currentSong.GetChart(selectedDifficulty);
            if (currentChart == null)
            {
                // Try to find any available chart
                currentChart = currentSong.Charts != null ? currentSong.Charts.FirstOrDefault() : null;
            }
            
            if (currentChart != null)
            {
                currentSongLength.text = FormatTime(currentChart.TotalLength);
                currentSongBPM.text = currentChart.TimingPoints != null && currentChart.TimingPoints.Count > 0 ? 
                    $"{currentChart.TimingPoints[0].BPM:F0} BPM" : "Unknown BPM";
            }
            else
            {
                currentSongLength.text = "No chart available";
                currentSongBPM.text = "Unknown BPM";
            }
            
            // Update background image if available
            if (!string.IsNullOrEmpty(currentSong.BackgroundImage))
            {
                // Load background image (you'd need to implement image loading)
                // currentSongBackground.sprite = LoadSprite(currentSong.BackgroundImage);
            }
            
            // Safety check for Charts list
            if (currentSong.Charts == null)
            {
                Debug.LogWarning($"Charts list is null for song: {currentSong.Title}");
                return; // Exit early if no charts
            }
            
            // Update difficulty buttons
            UpdateDifficultyButtons(currentSong);
        }
        
        private void UpdateDifficultyButtons(SongEntry song)
        {
            if (song.Charts == null)
            {
                Debug.LogWarning($"Charts list is null for song: {song.Title}");
                return;
            }
            
            // Update difficulty button states based on available charts
            for (int i = 0; i < difficultyButtonsParent.childCount; i++)
            {
                Transform buttonTransform = difficultyButtonsParent.GetChild(i);
                Button button = buttonTransform.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonTransform.GetComponentInChildren<TextMeshProUGUI>();
                
                if (button != null && buttonText != null)
                {
                    // Check if this difficulty is available
                    Difficulty diff = (Difficulty)i;
                    bool hasDifficulty = song.Charts.Any(c => c.Difficulty == diff);
                    
                    button.interactable = hasDifficulty;
                    buttonText.color = hasDifficulty ? Color.white : Color.gray;
                    
                    // Highlight selected difficulty
                    if (diff == selectedDifficulty && hasDifficulty)
                    {
                        button.interactable = false;
                        buttonText.color = Color.yellow;
                    }
                }
            }
        }
        
        private void OnSongItemSelected(int index)
        {
            currentSongIndex = index;
            UpdateSongDisplay();
        }
        
        private void OnDifficultySelected(Difficulty difficulty)
        {
            selectedDifficulty = difficulty;
            UpdateSongDisplay();
        }
        
        private void OnDifficultyFilterChanged(int value)
        {
            if (value == 0) // "All"
            {
                filteredSongs = allSongs.ToList();
            }
            else
            {
                Difficulty selectedDiff = (Difficulty)(value - 1);
                filteredSongs = allSongs.Where(s => s.Charts != null && s.Charts.Any(c => c.Difficulty == selectedDiff)).ToList();
            }
            
            currentSongIndex = 0;
            RefreshSongList();
            UpdateSongDisplay();
        }
        
        private void OnSearchChanged(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                filteredSongs = allSongs.ToList();
            }
            else
            {
                filteredSongs = allSongs.Where(s => 
                    (s.Title != null && s.Title.ToLower().Contains(searchQuery.ToLower())) ||
                    (s.Artist != null && s.Artist.ToLower().Contains(searchQuery.ToLower())) ||
                    (s.Creator != null && s.Creator.ToLower().Contains(searchQuery.ToLower())) ||
                    (s.Tags != null && s.Tags.Any(tag => tag != null && tag.ToLower().Contains(searchQuery.ToLower())))
                ).ToList();
            }
            
            currentSongIndex = 0;
            RefreshSongList();
            UpdateSongDisplay();
        }
        
        private void SelectRandomSong()
        {
            if (filteredSongs.Count > 0)
            {
                currentSongIndex = Random.Range(0, filteredSongs.Count);
                UpdateSongDisplay();
            }
        }
        
        private void ScanForNewSongs()
        {
            if (ChartDatabase.Instance != null)
            {
                ChartDatabase.Instance.ScanForSongs();
                LoadSongs();
            }
        }
        
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        
        public void StartGame()
        {
            if (filteredSongs.Count > 0)
            {
                SongEntry selectedSong = filteredSongs[currentSongIndex];
                ChartData selectedChart = selectedSong.GetChart(selectedDifficulty);
                
                if (selectedChart != null)
                {
                    // Start the game with the selected song and chart
                    GameManager.Instance.StartGame(selectedSong, selectedChart);
                }
                else
                {
                    Debug.LogWarning($"No chart found for difficulty {selectedDifficulty} in song {selectedSong.Title}");
                }
            }
        }
        
        public void NextSong()
        {
            if (filteredSongs.Count > 0)
            {
                currentSongIndex = (currentSongIndex + 1) % filteredSongs.Count;
                UpdateSongDisplay();
            }
        }
        
        public void PreviousSong()
        {
            if (filteredSongs.Count > 0)
            {
                currentSongIndex = (currentSongIndex - 1 + filteredSongs.Count) % filteredSongs.Count;
                UpdateSongDisplay();
            }
        }
    }
}
