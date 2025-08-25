using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace AshTaiko.Menu
{
    /*
        SongListManager handles the song list display, filtering, sorting, and selection.
        This class encapsulates all song list functionality to improve code organization
        and maintainability.
        
        Data Structure Design:
        Uses Lists for dynamic song collections to allow runtime filtering and sorting.
        Implements event-driven architecture for UI updates and song selection.
        Stores search and filter state as string and enum values for persistent user preferences.
    */

    public class SongListManager : MonoBehaviour
    {
        #region UI References

        [Header("Song List")]
        [SerializeField] private Transform _songListContent;
        [SerializeField] private GameObject _songListItemPrefab;
        [SerializeField] private ScrollRect _songListScrollRect;
        
        [Header("Search and Filter")]
        [SerializeField] private TMP_InputField _searchInput;
        [SerializeField] private TMP_Dropdown _sortDropdown;
        [SerializeField] private TMP_Dropdown _sortOrderDropdown;
        [SerializeField] private TMP_Dropdown _difficultyFilterDropdown;

        #endregion

        #region Private Fields

        /*
            Song data management using Lists for dynamic collections that can be filtered and sorted.
            The filtered songs list allows for efficient UI updates without modifying the original database.
        */
        private List<SongEntry> _allSongs = new List<SongEntry>();
        private List<SongEntry> _filteredSongs = new List<SongEntry>();
        private List<SongListItem> _songListItems = new List<SongListItem>();
        
        /*
            Current selection state tracking with nullable references for safe access.
            Using SongEntry and ChartData references maintains consistency with the existing database system.
        */
        private SongEntry _selectedSong;
        private int _selectedSongIndex = -1;
        
        /*
            Search and filter state using string for search queries and enums for filter types.
            These values persist during the session and can be easily serialized for user preferences.
        */
        private string _currentSearchQuery = "";
        private SongSortType _currentSortType = SongSortType.Title;
        private SortOrder _currentSortOrder = SortOrder.Ascending;
        private Difficulty _currentDifficultyFilter = Difficulty.Normal;
        private bool _showAllDifficulties = true;

        #endregion

        #region Enums

        /*
            SongSortType enum provides type-safe sorting options for the song list.
            Each sort type corresponds to a different property of the SongEntry class.
        */
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

        /*
            SongSelected event provides decoupled communication for song selection.
            This allows other components to react to song selection without direct dependencies.
        */
        public System.Action<SongEntry> OnSongSelected;
        
        /*
            SongHovered event provides communication for when songs are hovered during selection.
            This allows the background system to change based on hover state.
        */
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

        /*
            InitializeUI sets up the UI components and initializes dropdowns.
            This method ensures all UI elements are properly configured before use.
        */
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

        /*
            InitializeSortDropdown populates the sorting dropdown with available options.
            This provides users with multiple ways to organize their song library.
        */
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
        
        /*
            InitializeSortOrderDropdown populates the sort order dropdown with ascending/descending options.
            This allows users to control the direction of sorting.
        */
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

        /*
            InitializeDifficultyFilterDropdown sets up the difficulty filtering system.
            This allows users to focus on songs of specific difficulty levels.
        */
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

        /*
            SetupEventListeners connects UI events to their corresponding handlers.
            This creates a responsive interface that updates in real-time.
        */
        private void SetupEventListeners()
        {
            // Event listeners are set up in InitializeUI for dropdowns
            // Search input listener is set up in InitializeUI
        }

        /*
            CleanupEventListeners removes event subscriptions to prevent memory leaks.
            This ensures proper cleanup when the component is destroyed.
        */
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

        /*
            LoadSongDatabase retrieves songs from the ChartDatabase and initializes the list.
            This method ensures the song list is populated with current database content.
        */
        public void LoadSongDatabase()
        {
            var database = ChartDatabase.Instance?.GetDatabase();
            if (database != null)
            {
                _allSongs = database.GetSongsList();
                RefreshSongList();
                Debug.Log($"Loaded {_allSongs.Count} songs from database");
            }
            else
            {
                Debug.LogWarning("ChartDatabase not found or no database loaded");
            }
        }

        #endregion

        #region Event Handlers

        /*
            OnSearchInputChanged handles real-time search input from the user.
            This provides immediate filtering feedback as the user types.
        */
        private void OnSearchInputChanged(string searchQuery)
        {
            _currentSearchQuery = searchQuery;
            RefreshSongList();
        }

        /*
            OnSortDropdownChanged handles user selection of sorting options.
            This allows users to organize their song library by different criteria.
        */
        private void OnSortDropdownChanged(int sortIndex)
        {
            _currentSortType = (SongSortType)sortIndex;
            RefreshSongList();
        }
        
        /*
            OnSortOrderDropdownChanged handles user selection of sort order.
            This allows users to control whether sorting is ascending or descending.
        */
        private void OnSortOrderDropdownChanged(int orderIndex)
        {
            _currentSortOrder = (SortOrder)orderIndex;
            RefreshSongList();
        }

        /*
            OnDifficultyFilterChanged handles difficulty-based filtering.
            This allows users to focus on songs of specific difficulty levels.
        */
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

        /*
            RefreshSongList updates the song list based on current filters and sorting.
            This method ensures the UI always reflects the current filter state.
        */
        public void RefreshSongList()
        {
            ApplyFilters();
            ApplySorting();
            UpdateSongListUI();
        }

        /*
            ApplyFilters processes the current search query and difficulty filter.
            This creates a focused view of songs matching user criteria.
        */
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

        /*
            ApplySorting organizes the filtered song list based on user preferences.
            This provides consistent and predictable song ordering with support for ascending/descending.
        */
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

        /*
            UpdateSongListUI refreshes the visual representation of the song list.
            This method efficiently updates only the necessary UI elements.
        */
        private void UpdateSongListUI()
        {
            ClearSongListItems();

            for (int i = 0; i < _filteredSongs.Count; i++)
            {
                CreateSongListItem(_filteredSongs[i], i);
            }

            UpdateSongItemSelection();
        }

        /*
            CreateSongListItem instantiates and configures a single song list item.
            Each item displays song information and handles user interaction.
        */
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

        /*
            ClearSongListItems removes all existing song list items and cleans up references.
            This prevents memory leaks and ensures clean UI state management.
        */
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

        /*
            OnSongItemHovered handles hover events from song list items.
            This method forwards the hover event to the parent system.
        */
        private void OnSongItemHovered(SongEntry song)
        {
            OnSongHovered?.Invoke(song);
        }
        
        /*
            OnSongItemSelected handles user selection of a song from the list.
            This method updates the selection state and notifies other components.
        */
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

        /*
            UpdateSongItemSelection updates the visual state of all song list items.
            This provides clear feedback about which song is currently selected.
        */
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

        /*
            GetSelectedSong provides access to the currently selected song.
            This allows other components to access the selection state.
        */
        public SongEntry GetSelectedSong()
        {
            return _selectedSong;
        }

        /*
            GetSelectedSongIndex provides the index of the currently selected song.
            This is useful for UI positioning and navigation.
        */
        public int GetSelectedSongIndex()
        {
            return _selectedSongIndex;
        }

        /*
            GetFilteredSongCount provides the current number of songs after filtering.
            This is useful for UI display and pagination.
        */
        public int GetFilteredSongCount()
        {
            return _filteredSongs.Count;
        }

        /*
            GetTotalSongCount provides the total number of songs in the database.
            This represents the complete song library size.
        */
        public int GetTotalSongCount()
        {
            return _allSongs.Count;
        }
        
        /*
            GetCurrentSortType provides the current sorting criteria being used.
            This allows other components to query the current sort state.
        */
        public SongSortType GetCurrentSortType()
        {
            return _currentSortType;
        }
        
        /*
            GetCurrentSortOrder provides the current sort order being used.
            This allows other components to query the current sort direction.
        */
        public SortOrder GetCurrentSortOrder()
        {
            return _currentSortOrder;
        }

        /*
            RefreshDatabase forces a reload of the song database.
            This is useful for updating the list after external changes.
        */
        public void RefreshDatabase()
        {
            LoadSongDatabase();
        }

        /*
            ClearSelection resets the current song selection.
            This provides a clean state for new selections.
        */
        public void ClearSelection()
        {
            _selectedSong = null;
            _selectedSongIndex = -1;
            UpdateSongItemSelection();
        }

        #endregion
    }
}
