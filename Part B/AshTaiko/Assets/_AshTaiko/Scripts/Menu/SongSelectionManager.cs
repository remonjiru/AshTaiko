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
        [SerializeField] private Transform _songListContent;
        [SerializeField] private GameObject _songItemPrefab;
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private Dropdown _difficultyFilter;
        [SerializeField] private Button _randomButton;
        [SerializeField] private Button _scanButton;
        
        [Header("Song Display")]
        [SerializeField] private TextMeshProUGUI _currentSongTitle;
        [SerializeField] private TextMeshProUGUI _currentSongArtist;
        [SerializeField] private TextMeshProUGUI _currentSongCreator;
        [SerializeField] private TextMeshProUGUI _currentSongLength;
        [SerializeField] private TextMeshProUGUI _currentSongBPM;
        [SerializeField] private Image _currentSongBackground;
        
        [Header("Difficulty Display")]
        [SerializeField] private Transform _difficultyButtonsParent;
        [SerializeField] private GameObject _difficultyButtonPrefab;
        
        private List<SongEntry> _allSongs = new List<SongEntry>();
        private List<SongEntry> _filteredSongs = new List<SongEntry>();
        private int _currentSongIndex = 0;
        private Difficulty _selectedDifficulty = Difficulty.Normal;
        
        private void Start()
        {
            InitializeUI();
            LoadSongs();
            UpdateSongDisplay();
        }
        
        private void InitializeUI()
        {
            // Initialize difficulty filter dropdown
            _difficultyFilter.ClearOptions();
            _difficultyFilter.AddOptions(new List<string> { "All", "Easy", "Normal", "Hard", "Insane", "Expert", "Master" });
            _difficultyFilter.onValueChanged.AddListener(OnDifficultyFilterChanged);
            
            // Initialize search input
            _searchInput.onValueChanged.AddListener(OnSearchChanged);
            
            // Initialize buttons
            _randomButton.onClick.AddListener(SelectRandomSong);
            _scanButton.onClick.AddListener(ScanForNewSongs);
            
            // Initialize difficulty buttons
            CreateDifficultyButtons();
        }
        
        private void CreateDifficultyButtons()
        {
            // Clear existing buttons
            foreach (Transform child in _difficultyButtonsParent)
            {
                Destroy(child.gameObject);
            }
            
            // Create difficulty buttons
            Difficulty[] difficulties = { Difficulty.Easy, Difficulty.Normal, Difficulty.Hard, Difficulty.Insane, Difficulty.Expert, Difficulty.Master };
            
            foreach (Difficulty diff in difficulties)
            {
                GameObject buttonObj = Instantiate(_difficultyButtonPrefab, _difficultyButtonsParent);
                Button button = buttonObj.GetComponent<Button>();
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                
                if (buttonText != null)
                {
                    buttonText.text = diff.ToString();
                }
                
                Difficulty capturedDiff = diff; // Capture for lambda
                button.onClick.AddListener(() => OnDifficultySelected(capturedDiff));
                
                // Set initial selected difficulty
                if (diff == _selectedDifficulty)
                {
                    button.interactable = false;
                }
            }
        }
        
        private void LoadSongs()
        {
            if (ChartDatabase.Instance != null)
            {
                _allSongs = ChartDatabase.Instance.GetSongs().ToList();
                _filteredSongs = _allSongs.ToList();
                RefreshSongList();
            }
        }
        
        private void RefreshSongList()
        {
            // Clear existing song items
            foreach (Transform child in _songListContent)
            {
                Destroy(child.gameObject);
            }
            
            // Create song items
            for (int i = 0; i < _filteredSongs.Count; i++)
            {
                GameObject songItemObj = Instantiate(_songItemPrefab, _songListContent);
                SongListItem songItem = songItemObj.GetComponent<SongListItem>();
                
                if (songItem != null)
                {
                    songItem.Initialize(_filteredSongs[i], i);
                    songItem.OnSongSelected += OnSongItemSelected;
                }
            }
        }
        
        private void UpdateSongDisplay()
        {
            if (_filteredSongs.Count == 0) return;
            
            SongEntry currentSong = _filteredSongs[_currentSongIndex];
            
            // Update song info
            _currentSongTitle.text = currentSong.Title;
            _currentSongArtist.text = currentSong.Artist;
            _currentSongCreator.text = currentSong.Creator;
            
            // Get the selected difficulty chart
            ChartData currentChart = currentSong.GetChart(_selectedDifficulty);
            if (currentChart == null)
            {
                // Try to find any available chart
                currentChart = currentSong.Charts != null ? currentSong.Charts.FirstOrDefault() : null;
            }
            
            if (currentChart != null)
            {
                _currentSongLength.text = FormatTime(currentChart.TotalLength);
                _currentSongBPM.text = currentChart.TimingPoints != null && currentChart.TimingPoints.Count > 0 ? 
                    $"{currentChart.TimingPoints[0].BPM:F0} BPM" : "Unknown BPM";
            }
            else
            {
                _currentSongLength.text = "No chart available";
                _currentSongBPM.text = "Unknown BPM";
            }
            
            // Update background image if available
            if (!string.IsNullOrEmpty(currentSong.BackgroundImage))
            {
                // Load background image (you'd need to implement image loading)
                // _currentSongBackground.sprite = LoadSprite(currentSong.BackgroundImage);
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
            for (int i = 0; i < _difficultyButtonsParent.childCount; i++)
            {
                Transform buttonTransform = _difficultyButtonsParent.GetChild(i);
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
                    if (diff == _selectedDifficulty && hasDifficulty)
                    {
                        button.interactable = false;
                        buttonText.color = Color.yellow;
                    }
                }
            }
        }
        
        private void OnSongItemSelected(int index)
        {
            _currentSongIndex = index;
            UpdateSongDisplay();
        }
        
        private void OnDifficultySelected(Difficulty difficulty)
        {
            _selectedDifficulty = difficulty;
            UpdateSongDisplay();
        }
        
        private void OnDifficultyFilterChanged(int value)
        {
            if (value == 0) // "All"
            {
                _filteredSongs = _allSongs.ToList();
            }
            else
            {
                Difficulty selectedDiff = (Difficulty)(value - 1);
                _filteredSongs = _allSongs.Where(s => s.Charts != null && s.Charts.Any(c => c.Difficulty == selectedDiff)).ToList();
            }
            
            _currentSongIndex = 0;
            RefreshSongList();
            UpdateSongDisplay();
        }
        
        private void OnSearchChanged(string searchQuery)
        {
            if (string.IsNullOrEmpty(searchQuery))
            {
                _filteredSongs = _allSongs.ToList();
            }
            else
            {
                _filteredSongs = _allSongs.Where(s => 
                    (s.Title != null && s.Title.ToLower().Contains(searchQuery.ToLower())) ||
                    (s.Artist != null && s.Artist.ToLower().Contains(searchQuery.ToLower())) ||
                    (s.Creator != null && s.Creator.ToLower().Contains(searchQuery.ToLower())) ||
                    (s.Tags != null && s.Tags.Any(tag => tag != null && tag.ToLower().Contains(searchQuery.ToLower())))
                ).ToList();
            }
            
            _currentSongIndex = 0;
            RefreshSongList();
            UpdateSongDisplay();
        }
        
        private void SelectRandomSong()
        {
            if (_filteredSongs.Count > 0)
            {
                _currentSongIndex = Random.Range(0, _filteredSongs.Count);
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
            if (_filteredSongs.Count > 0)
            {
                SongEntry selectedSong = _filteredSongs[_currentSongIndex];
                ChartData selectedChart = selectedSong.GetChart(_selectedDifficulty);
                
                if (selectedChart != null)
                {
                    // Start the game with the selected song and chart
                    GameManager.Instance.StartGame(selectedSong, selectedChart);
                }
                else
                {
                    Debug.LogWarning($"No chart found for difficulty {_selectedDifficulty} in song {selectedSong.Title}");
                }
            }
        }
        
        public void NextSong()
        {
            if (_filteredSongs.Count > 0)
            {
                _currentSongIndex = (_currentSongIndex + 1) % _filteredSongs.Count;
                UpdateSongDisplay();
            }
        }
        
        public void PreviousSong()
        {
            if (_filteredSongs.Count > 0)
            {
                _currentSongIndex = (_currentSongIndex - 1 + _filteredSongs.Count) % _filteredSongs.Count;
                UpdateSongDisplay();
            }
        }
    }
}
