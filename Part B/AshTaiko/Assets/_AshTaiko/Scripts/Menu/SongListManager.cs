using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace AshTaiko.Menu
{
    /// <summary>
    /// Handles the song list display, filtering, sorting, and selection.
    /// This class encapsulates all song list functionality to improve code organization and maintainability.
    /// Uses Lists for dynamic song collections to allow runtime filtering and sorting.
    /// Implements event-driven architecture for UI updates and song selection.
    /// Stores search and filter state as string and enum values for persistent user preferences.
    /// </summary>
    public class SongListManager : MonoBehaviour
    {
        #region UI References

        [Header("Song List")]
        [SerializeField] 
        private Transform _songListContent;
        [SerializeField] 
        private GameObject _songListItemPrefab;
        [SerializeField] 
        private ScrollRect _songListScrollRect;
        
        [Header("Search and Filter")]
        [SerializeField] 
        private TMP_InputField _searchInput;
        [SerializeField] 
        private TMP_Dropdown _sortDropdown;
        [SerializeField] 
        private TMP_Dropdown _sortOrderDropdown;
        [SerializeField] 
        private TMP_Dropdown _difficultyFilterDropdown;

        #endregion

        #region Private Fields

        /// <summary>
        /// Song data management using Lists for dynamic collections that can be filtered and sorted.
        /// The filtered songs list allows for efficient UI updates without modifying the original database.
        /// </summary>
        private List<SongEntry> _allSongs = new List<SongEntry>();
        private List<SongEntry> _filteredSongs = new List<SongEntry>();
        private List<SongListItem> _songListItems = new List<SongListItem>();
        
        /// <summary>
        /// Current selection state tracking with nullable references for safe access.
        /// Using SongEntry and ChartData references maintains consistency with the existing database system.
        /// </summary>
        private SongEntry _selectedSong;
        private int _selectedSongIndex = -1;
        
        /// <summary>
        /// Search and filter state using string for search queries and enums for filter types.
        /// These values persist during the session and can be easily serialized for user preferences.
        /// </summary>
        private string _currentSearchQuery = "";
        private SongSortType _currentSortType = SongSortType.Title;
        private SortOrder _currentSortOrder = SortOrder.Ascending;
        private Difficulty _currentDifficultyFilter = Difficulty.Normal;
        private bool _showAllDifficulties = true;

        #endregion

        #region Enums

        /// <summary>
        /// Provides type-safe sorting options for the song list.
        /// Each sort type corresponds to a different property of the SongEntry class.
        /// </summary>
        public enum SongSortType
        {
            Title,
            Artist,
            Creator,
            Length,
            Difficulty,
            DateAdded
        }
        
        public enum SortOrder
        {
            Ascending,
            Descending
        }

        #endregion

        #region Events

        /// <summary>
        /// Provides decoupled communication for song selection.
        /// This allows other components to react to song selection without direct dependencies.
        /// </summary>
        public System.Action<SongEntry> OnSongSelected;
        
        /// <summary>
        /// Provides communication for when songs are hovered during selection.
        /// This allows the background system to change based on hover state.
        /// </summary>
        public System.Action<SongEntry> OnSongHovered;

        #endregion

        #region Initialization

        private void Awake()
        {
            InitializeUI();
        }

        private void Start()
        {
            LoadSongDatabase();
            SetupEventListeners();
        }

        private void OnDestroy()
        {
            CleanupEventListeners();
        }

        /// <summary>
        /// Sets up the UI components and initializes dropdowns.
        /// This method ensures all UI elements are properly configured before use.
        /// </summary>
        public void InitializeUI()
        {
            if (_searchInput != null)
            {
                _searchInput.onValueChanged.AddListener(OnSearchInputChanged);
            }

            InitializeSortDropdown();
            InitializeSortOrderDropdown();
            InitializeDifficultyFilterDropdown();
        }

        /// <summary>
        /// Populates the sorting dropdown with available options.
        /// This provides users with multiple ways to organize their song library.
        /// </summary>
        private void InitializeSortDropdown()
        {
            if (_sortDropdown == null) return;

            _sortDropdown.ClearOptions();
            var options = new List<string>
            {
                "Title",
                "Artist", 
                "Creator",
                "Length",
                "Difficulty",
                "Date Added"
            };
            _sortDropdown.AddOptions(options);
            _sortDropdown.onValueChanged.AddListener(OnSortDropdownChanged);
        }
        
        /// <summary>
        /// Populates the sort order dropdown with ascending/descending options.
        /// This allows users to control the direction of sorting.
        /// </summary>
        private void InitializeSortOrderDropdown()
        {
            if (_sortOrderDropdown == null) return;

            _sortOrderDropdown.ClearOptions();
            var options = new List<string>
            {
                "Ascending",
                "Descending"
            };
            _sortOrderDropdown.AddOptions(options);
            _sortOrderDropdown.onValueChanged.AddListener(OnSortOrderDropdownChanged);
        }

        /// <summary>
        /// Sets up the difficulty filtering system.
        /// This allows users to focus on songs of specific difficulty levels.
        /// </summary>
        private void InitializeDifficultyFilterDropdown()
        {
            if (_difficultyFilterDropdown == null) return;

            _difficultyFilterDropdown.ClearOptions();
            var options = new List<string>
            {
                "All Difficulties",
                "Easy",
                "Normal", 
                "Hard",
                "Oni/Insane",
                "Expert",
                "Master"
            };
            _difficultyFilterDropdown.AddOptions(options);
            _difficultyFilterDropdown.onValueChanged.AddListener(OnDifficultyFilterChanged);
        }

        #endregion

        #region Event Management

        /// <summary>
        /// Connects UI events to their corresponding handlers.
        /// This creates a responsive interface that updates in real-time.
        /// </summary>
        private void SetupEventListeners()
        {
            // Event listeners are set up in InitializeUI for dropdowns
            // Search input listener is set up in InitializeUI
        }

        /// <summary>
        /// Removes event subscriptions to prevent memory leaks.
        /// This ensures proper cleanup when the component is destroyed.
        /// </summary>
        private void CleanupEventListeners()
        {
            if (_searchInput != null)
            {
                _searchInput.onValueChanged.RemoveListener(OnSearchInputChanged);
            }

            if (_sortDropdown != null)
            {
                _sortDropdown.onValueChanged.RemoveListener(OnSortDropdownChanged);
            }

            if (_difficultyFilterDropdown != null)
            {
                _difficultyFilterDropdown.onValueChanged.RemoveListener(OnDifficultyFilterChanged);
            }
        }

        #endregion

        #region Database Management

        /// <summary>
        /// Retrieves songs from the ChartDatabase and initializes the list.
        /// This method ensures the song list is populated with current database content.
        /// </summary>
        public void LoadSongDatabase()
        {
            var database = ChartDatabase.Instance?.GetDatabase();
            if (database != null)
            {
                _allSongs = database.GetSongsList();
                Debug.Log($"Loaded {_allSongs.Count} songs from database");
                
                // Log summary for debugging
                int totalCharts = _allSongs.Sum(s => s.Charts?.Count ?? 0);
                Debug.Log($"Loaded {_allSongs.Count} songs with {totalCharts} total charts from database");
                
                RefreshSongList();
            }
            else
            {
                Debug.LogWarning("ChartDatabase not found or no database loaded");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles real-time search input from the user.
        /// This provides immediate filtering feedback as the user types.
        /// </summary>
        /// <param name="searchQuery">The search query entered by the user.</param>
        private void OnSearchInputChanged(string searchQuery)
        {
            _currentSearchQuery = searchQuery;
            RefreshSongList();
        }

        /// <summary>
        /// Handles user selection of sorting options.
        /// This allows users to organize their song library by different criteria.
        /// </summary>
        /// <param name="sortIndex">The index of the selected sort option.</param>
        private void OnSortDropdownChanged(int sortIndex)
        {
            _currentSortType = (SongSortType)sortIndex;
            RefreshSongList();
        }
        
        /// <summary>
        /// Handles user selection of sort order.
        /// This allows users to control whether sorting is ascending or descending.
        /// </summary>
        /// <param name="orderIndex">The index of the selected sort order.</param>
        private void OnSortOrderDropdownChanged(int orderIndex)
        {
            _currentSortOrder = (SortOrder)orderIndex;
            RefreshSongList();
        }

        /// <summary>
        /// Handles difficulty-based filtering.
        /// This allows users to focus on songs of specific difficulty levels.
        /// </summary>
        /// <param name="filterIndex">The index of the selected difficulty filter.</param>
        private void OnDifficultyFilterChanged(int filterIndex)
        {
            if (filterIndex == 0)
            {
                _showAllDifficulties = true;
            }
            else
            {
                _showAllDifficulties = false;
                _currentDifficultyFilter = (Difficulty)(filterIndex - 1);
            }
            RefreshSongList();
        }

        #endregion

        #region Song List Management

        /// <summary>
        /// Updates the song list based on current filters and sorting.
        /// This method ensures the UI always reflects the current filter state.
        /// </summary>
        public void RefreshSongList()
        {
            Debug.Log($"RefreshSongList called - {_allSongs.Count} total songs, {_filteredSongs.Count} filtered songs");
            ApplyFilters();
            ApplySorting();
            UpdateSongListUI();
            Debug.Log($"RefreshSongList completed - {_filteredSongs.Count} songs after filtering");
        }

        /// <summary>
        /// Processes the current search query and difficulty filter.
        /// This creates a focused view of songs matching user criteria.
        /// </summary>
        private void ApplyFilters()
        {
            _filteredSongs = _allSongs.ToList();

            // Apply search filter
            if (!string.IsNullOrEmpty(_currentSearchQuery))
            {
                _filteredSongs = _filteredSongs.Where(s => 
                    (s.Title != null && s.Title.ToLower().Contains(_currentSearchQuery.ToLower())) ||
                    (s.Artist != null && s.Artist.ToLower().Contains(_currentSearchQuery.ToLower())) ||
                    (s.Creator != null && s.Creator.ToLower().Contains(_currentSearchQuery.ToLower()))
                ).ToList();
            }

            // Apply difficulty filter
            if (!_showAllDifficulties)
            {
                _filteredSongs = _filteredSongs.Where(s => 
                    s.Charts != null && s.Charts.Any(c => c.Difficulty == _currentDifficultyFilter)
                ).ToList();
            }
        }

        /// <summary>
        /// Organizes the filtered song list based on user preferences.
        /// This provides consistent and predictable song ordering with support for ascending/descending.
        /// </summary>
        private void ApplySorting()
        {
            IOrderedEnumerable<SongEntry> orderedSongs = null;
            
            // First, apply the primary sorting
            switch (_currentSortType)
            {
                case SongSortType.Title:
                    orderedSongs = _filteredSongs.OrderBy(s => s.Title ?? "");
                    break;
                case SongSortType.Artist:
                    orderedSongs = _filteredSongs.OrderBy(s => s.Artist ?? "");
                    break;
                case SongSortType.Creator:
                    orderedSongs = _filteredSongs.OrderBy(s => s.Creator ?? "");
                    break;
                case SongSortType.Length:
                    orderedSongs = _filteredSongs.OrderBy(s => 
                        s.Charts?.Count > 0 ? s.Charts[0].TotalLength : 0
                    );
                    break;
                case SongSortType.Difficulty:
                    orderedSongs = _filteredSongs.OrderBy(s => 
                        s.Charts?.Count > 0 ? s.Charts[0].Difficulty : Difficulty.Easy
                    );
                    break;
                case SongSortType.DateAdded:
                    // For now, maintain current order (could be enhanced with actual date tracking)
                    orderedSongs = _filteredSongs.OrderBy(s => 0);
                    break;
            }
            
            // Then apply the sort order
            if (orderedSongs != null)
            {
                if (_currentSortOrder == SortOrder.Descending)
                {
                    _filteredSongs = orderedSongs.Reverse().ToList();
                }
                else
                {
                    _filteredSongs = orderedSongs.ToList();
                }
            }
        }

        /// <summary>
        /// Refreshes the visual representation of the song list.
        /// This method efficiently updates only the necessary UI elements.
        /// </summary>
        private void UpdateSongListUI()
        {
            ClearSongListItems();

            for (int i = 0; i < _filteredSongs.Count; i++)
            {
                CreateSongListItem(_filteredSongs[i], i);
            }

            UpdateSongItemSelection();
        }

        /// <summary>
        /// Instantiates and configures a single song list item.
        /// Each item displays song information and handles user interaction.
        /// </summary>
        /// <param name="song">The song data to display.</param>
        /// <param name="index">The index of the song in the filtered list.</param>
        private void CreateSongListItem(SongEntry song, int index)
        {
            GameObject itemObj = Instantiate(_songListItemPrefab, _songListContent);
            SongListItem item = itemObj.GetComponent<SongListItem>();

            if (item != null)
            {
                item.Initialize(song, index);
                item.OnSongSelected += OnSongItemSelected;
                item.OnSongHovered += OnSongItemHovered;
                _songListItems.Add(item);
            }
        }

        /// <summary>
        /// Removes all existing song list items and cleans up references.
        /// This prevents memory leaks and ensures clean UI state management.
        /// </summary>
        private void ClearSongListItems()
        {
            foreach (var item in _songListItems)
            {
                if (item != null)
                {
                    item.OnSongSelected -= OnSongItemSelected;
                    item.OnSongHovered -= OnSongItemHovered;
                    Destroy(item.gameObject);
                }
            }
            _songListItems.Clear();
        }

        /// <summary>
        /// Handles hover events from song list items.
        /// This method forwards the hover event to the parent system.
        /// </summary>
        /// <param name="song">The song being hovered, or null if no song is hovered.</param>
        private void OnSongItemHovered(SongEntry song)
        {
            OnSongHovered?.Invoke(song);
        }
        
        /// <summary>
        /// Handles user selection of a song from the list.
        /// This method updates the selection state and notifies other components.
        /// </summary>
        /// <param name="songIndex">The index of the selected song.</param>
        private void OnSongItemSelected(int songIndex)
        {
            if (songIndex >= 0 && songIndex < _filteredSongs.Count)
            {
                _selectedSongIndex = songIndex;
                _selectedSong = _filteredSongs[songIndex];
                
                UpdateSongItemSelection();
                OnSongSelected?.Invoke(_selectedSong);
                
                Debug.Log($"Selected song: {_selectedSong.Title} - {_selectedSong.Artist}");
            }
        }

        /// <summary>
        /// Updates the visual state of all song list items.
        /// This provides clear feedback about which song is currently selected.
        /// </summary>
        private void UpdateSongItemSelection()
        {
            for (int i = 0; i < _songListItems.Count; i++)
            {
                if (_songListItems[i] != null)
                {
                    bool isSelected = (i == _selectedSongIndex);
                    _songListItems[i].SetSelected(isSelected);
                }
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Provides access to the currently selected song.
        /// This allows other components to access the selection state.
        /// </summary>
        /// <returns>The currently selected song, or null if none is selected.</returns>
        public SongEntry GetSelectedSong()
        {
            return _selectedSong;
        }

        /// <summary>
        /// Provides the index of the currently selected song.
        /// This is useful for UI positioning and navigation.
        /// </summary>
        /// <returns>The index of the selected song, or -1 if none is selected.</returns>
        public int GetSelectedSongIndex()
        {
            return _selectedSongIndex;
        }

        /// <summary>
        /// Provides the current number of songs after filtering.
        /// This is useful for UI display and pagination.
        /// </summary>
        /// <returns>The number of songs in the filtered list.</returns>
        public int GetFilteredSongCount()
        {
            return _filteredSongs.Count;
        }

        /// <summary>
        /// Provides the total number of songs in the database.
        /// This represents the complete song library size.
        /// </summary>
        /// <returns>The total number of songs in the database.</returns>
        public int GetTotalSongCount()
        {
            return _allSongs.Count;
        }
        
        /// <summary>
        /// Provides the current sorting criteria being used.
        /// This allows other components to query the current sort state.
        /// </summary>
        /// <returns>The current sort type.</returns>
        public SongSortType GetCurrentSortType()
        {
            return _currentSortType;
        }
        
        /// <summary>
        /// Provides the current sort order being used.
        /// This allows other components to query the current sort direction.
        /// </summary>
        /// <returns>The current sort order.</returns>
        public SortOrder GetCurrentSortOrder()
        {
            return _currentSortOrder;
        }

        /// <summary>
        /// Forces a reload of the song database.
        /// This is useful for updating the list after external changes.
        /// </summary>
        public void RefreshDatabase()
        {
            LoadSongDatabase();
        }

        /// <summary>
        /// Resets the current song selection.
        /// This provides a clean state for new selections.
        /// </summary>
        public void ClearSelection()
        {
            _selectedSong = null;
            _selectedSongIndex = -1;
            UpdateSongItemSelection();
        }

        #endregion
    }
}
