using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AshTaiko.Editor
{
    public class SongSelectionEditor : EditorWindow
    {
        private Vector2 songListScrollPosition;
        private Vector2 chartListScrollPosition;
        private string searchQuery = "";
        private int selectedSongIndex = 0;
        private int selectedChartIndex = 0;
        private bool showAdvancedOptions = false;
        private bool autoStartOnSelection = true;
        private float refreshInterval = 5f;
        private double lastRefreshTime = 0;
        
        // Filter options
        private Difficulty selectedDifficultyFilter = Difficulty.Normal;
        private bool showAllDifficulties = true;
        // Remove hardcoded difficulty names - will be generated dynamically
        private List<string> availableDifficultyNames = new List<string>();
        private List<Difficulty> availableDifficulties = new List<Difficulty>();
        
        // Song database reference
        private SongDatabase songDatabase;
        private List<SongEntry> filteredSongs = new List<SongEntry>();
        
        // Persistent selection keys for EditorPrefs
        private const string SELECTED_SONG_INDEX_KEY = "AshTaiko_SelectedSongIndex";
        private const string SELECTED_CHART_INDEX_KEY = "AshTaiko_SelectedChartIndex";
        private const string SEARCH_QUERY_KEY = "AshTaiko_SearchQuery";
        private const string DIFFICULTY_FILTER_KEY = "AshTaiko_DifficultyFilter";
        private const string SHOW_ALL_DIFFICULTIES_KEY = "AshTaiko_ShowAllDifficulties";
        
        [MenuItem("AshTaiko/Song Selection Editor")]
        public static void ShowWindow()
        {
            GetWindow<SongSelectionEditor>("Song Selection");
        }
        
        private void OnEnable()
        {
            RefreshSongDatabase();
            EditorApplication.update += OnEditorUpdate;
            LoadSelectionState();
            SyncWithGameManager(); // Sync editor selection with GameManager on open
        }
        
        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            SaveSelectionState();
        }
        
        private void OnEditorUpdate()
        {
            // Auto-refresh the database periodically
            if (EditorApplication.timeSinceStartup - lastRefreshTime > refreshInterval)
            {
                RefreshSongDatabase();
                lastRefreshTime = EditorApplication.timeSinceStartup;
            }
        }
        
        private void RefreshSongDatabase()
        {
            // Try to get the database from ChartDatabase instance
            var chartDatabase = FindFirstObjectByType<ChartDatabase>();
            if (chartDatabase != null)
            {
                songDatabase = chartDatabase.GetDatabase();
            }
            
            // If still null, try to load from Resources
            if (songDatabase == null)
            {
                songDatabase = Resources.Load<SongDatabase>("SongDatabase");
            }
            
            if (songDatabase != null)
            {
                UpdateAvailableDifficulties();
                ApplyFilters();
                UpdateGameManagerSelection();
            }
        }
        
        // Method to update available difficulties based on current database
        private void UpdateAvailableDifficulties()
        {
            if (songDatabase == null || songDatabase.Songs == null) return;
            
            availableDifficulties.Clear();
            availableDifficultyNames.Clear();
            
            // Collect all unique difficulties from all songs
            var allDifficulties = new HashSet<Difficulty>();
            foreach (var song in songDatabase.Songs)
            {
                if (song.Charts != null)
                {
                    foreach (var chart in song.Charts)
                    {
                        allDifficulties.Add(chart.Difficulty);
                    }
                }
            }
            
            // Convert to sorted lists
            availableDifficulties = allDifficulties.OrderBy(d => GetDifficultyOrder(d)).ToList();
            availableDifficultyNames = availableDifficulties.Select(d => GetDifficultyDisplayName(d)).ToList();
            
            // Validate that the currently selected difficulty filter is still available
            if (!availableDifficulties.Contains(selectedDifficultyFilter) && availableDifficulties.Count > 0)
            {
                Debug.Log($"Selected difficulty filter '{selectedDifficultyFilter}' no longer available, switching to '{availableDifficulties[0]}'");
                selectedDifficultyFilter = availableDifficulties[0];
            }
            
            Debug.Log($"Updated available difficulties: {string.Join(", ", availableDifficultyNames)}");
        }
        
        // Helper method to get difficulty display names
        private string GetDifficultyDisplayName(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return "Easy";
                case Difficulty.Normal: return "Normal";
                case Difficulty.Hard: return "Hard";
                case Difficulty.Insane: return "Oni/Insane";
                case Difficulty.Expert: return "Expert";
                case Difficulty.Master: return "Master";
                default: return difficulty.ToString();
            }
        }
        
        // Helper method to get difficulty ordering for consistent display
        private int GetDifficultyOrder(Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Easy: return 0;
                case Difficulty.Normal: return 1;
                case Difficulty.Hard: return 2;
                case Difficulty.Insane: return 3;
                case Difficulty.Expert: return 4;
                case Difficulty.Master: return 5;
                default: return 999; // Unknown difficulties at the end
            }
        }
        
        private void ApplyFilters()
        {
            if (songDatabase == null || songDatabase.Songs == null) return;
            
            filteredSongs = songDatabase.Songs.ToList();
            
            // Apply search filter
            if (!string.IsNullOrEmpty(searchQuery))
            {
                filteredSongs = filteredSongs.Where(s => 
                    (s.Title != null && s.Title.ToLower().Contains(searchQuery.ToLower())) ||
                    (s.Artist != null && s.Artist.ToLower().Contains(searchQuery.ToLower())) ||
                    (s.Creator != null && s.Creator.ToLower().Contains(searchQuery.ToLower()))
                ).ToList();
            }
            
            // Apply difficulty filter
            if (!showAllDifficulties)
            {
                filteredSongs = filteredSongs.Where(s => 
                    s.Charts != null && s.Charts.Any(c => c.Difficulty == selectedDifficultyFilter)
                ).ToList();
            }
            
            // Reset selection if out of bounds
            if (selectedSongIndex >= filteredSongs.Count)
            {
                selectedSongIndex = 0;
                selectedChartIndex = 0;
            }
            if (filteredSongs.Count > 0 && selectedChartIndex >= filteredSongs[selectedSongIndex].Charts.Count)
            {
                selectedChartIndex = 0;
            }
        }
        
        private void OnGUI()
        {
            if (songDatabase == null)
            {
                EditorGUILayout.HelpBox("No SongDatabase found! Please create one first.", MessageType.Error);
                if (GUILayout.Button("Create Song Database"))
                {
                    CreateSongDatabase();
                }
                return;
            }
            
            DrawHeader();
            DrawSearchAndFilters();
            DrawSongAndChartLists();
            DrawSelectedSongInfo();
            DrawActionButtons();
            DrawAdvancedOptions();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🎵 Song Selection Editor", EditorStyles.largeLabel);
            EditorGUILayout.LabelField($"Database: {songDatabase.name} ({songDatabase.GetSongCount()} songs)", EditorStyles.miniLabel);
            
            // Show difficulty statistics
            if (availableDifficulties.Count > 0)
            {
                EditorGUILayout.LabelField($"Available Difficulties: {GetDifficultyStatistics()}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.Space();
        }
        
        private void DrawSearchAndFilters()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Search field
            string newSearchQuery = EditorGUILayout.TextField("Search Songs:", searchQuery);
            if (newSearchQuery != searchQuery)
            {
                searchQuery = newSearchQuery;
                ApplyFilters();
            }
            
            // Refresh button
            if (GUILayout.Button("🔄", GUILayout.Width(30)))
            {
                RefreshSongDatabase();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Difficulty filter
            EditorGUILayout.BeginHorizontal();
            showAllDifficulties = EditorGUILayout.Toggle("Show All Difficulties", showAllDifficulties);
            if (!showAllDifficulties)
            {
                if (availableDifficultyNames.Count > 0)
                {
                    // Find the index of the currently selected difficulty
                    int currentIndex = availableDifficulties.IndexOf(selectedDifficultyFilter);
                    if (currentIndex == -1) currentIndex = 0; // Default to first available
                    
                    int newIndex = EditorGUILayout.Popup("Difficulty Filter:", currentIndex, availableDifficultyNames.ToArray());
                    if (newIndex != currentIndex && newIndex < availableDifficulties.Count)
                    {
                        selectedDifficultyFilter = availableDifficulties[newIndex];
                        ApplyFilters();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Difficulty Filter: No difficulties available", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }
        
        private void DrawSongAndChartLists()
        {
            EditorGUILayout.BeginHorizontal();
            
            // Song list
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));
            EditorGUILayout.LabelField("Songs", EditorStyles.boldLabel);
            
            songListScrollPosition = EditorGUILayout.BeginScrollView(songListScrollPosition, GUILayout.Height(200));
            
            for (int i = 0; i < filteredSongs.Count; i++)
            {
                var song = filteredSongs[i];
                bool isSelected = i == selectedSongIndex;
                
                EditorGUILayout.BeginHorizontal();
                
                // Selection indicator
                if (GUILayout.Button(isSelected ? "▶" : "  ", GUILayout.Width(20)))
                {
                    selectedSongIndex = i;
                    selectedChartIndex = 0;
                    GUI.changed = true;
                    SaveSelectionState(); // Save the new selection
                    UpdateGameManagerSelection(); // Immediately update GameManager
                }
                
                // Song info
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(song.Title ?? "Unknown Title", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Artist: {song.Artist ?? "Unknown Artist"}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Charts: {song.Charts?.Count ?? 0}", EditorStyles.miniLabel);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                
                if (i < filteredSongs.Count - 1)
                {
                    EditorGUILayout.Space(2);
                }
            }
            
            if (filteredSongs.Count == 0)
            {
                EditorGUILayout.HelpBox("No songs found matching the current filters.", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            // Chart list
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
            EditorGUILayout.LabelField("Charts", EditorStyles.boldLabel);
            
            if (filteredSongs.Count > 0 && selectedSongIndex < filteredSongs.Count)
            {
                var selectedSong = filteredSongs[selectedSongIndex];
                
                chartListScrollPosition = EditorGUILayout.BeginScrollView(chartListScrollPosition, GUILayout.Height(200));
                
                if (selectedSong.Charts != null && selectedSong.Charts.Count > 0)
                {
                    for (int i = 0; i < selectedSong.Charts.Count; i++)
                    {
                        var chart = selectedSong.Charts[i];
                        bool isSelected = i == selectedChartIndex;
                        
                        EditorGUILayout.BeginHorizontal();
                        
                        // Selection indicator
                        if (GUILayout.Button(isSelected ? "▶" : "  ", GUILayout.Width(20)))
                        {
                            selectedChartIndex = i;
                            GUI.changed = true;
                            SaveSelectionState(); // Save the new selection
                            UpdateGameManagerSelection(); // Immediately update GameManager
                        }
                        
                        // Chart info
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField(chart.Version ?? "Unknown", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField($"Difficulty: {chart.Difficulty}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"Notes: {chart.HitObjects?.Count ?? 0}", EditorStyles.miniLabel);
                        EditorGUILayout.EndVertical();
                        
                        EditorGUILayout.EndHorizontal();
                        
                        if (i < selectedSong.Charts.Count - 1)
                        {
                            EditorGUILayout.Space(2);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No charts available for this song.", MessageType.Warning);
                }
                
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.HelpBox("No song selected.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSelectedSongInfo()
        {
            if (filteredSongs.Count == 0 || selectedSongIndex >= filteredSongs.Count) return;
            
            var selectedSong = filteredSongs[selectedSongIndex];
            var selectedChart = selectedSong.Charts != null && selectedSong.Charts.Count > 0 && selectedChartIndex < selectedSong.Charts.Count 
                ? selectedSong.Charts[selectedChartIndex] 
                : null;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected Song Info", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            EditorGUILayout.LabelField($"Title: {selectedSong.Title ?? "Unknown"}");
            EditorGUILayout.LabelField($"Artist: {selectedSong.Artist ?? "Unknown"}");
            EditorGUILayout.LabelField($"Creator: {selectedSong.Creator ?? "Unknown"}");
            EditorGUILayout.LabelField($"Audio: {selectedSong.AudioFilename ?? "None"}");
            
            // Show available difficulties for this song
            if (selectedSong.Charts != null && selectedSong.Charts.Count > 0)
            {
                var difficulties = selectedSong.Charts.Select(c => $"{c.Version} ({c.Difficulty})").ToArray();
                EditorGUILayout.LabelField($"Available Difficulties: {string.Join(", ", difficulties)}");
            }
            
            if (selectedChart != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Chart: {selectedChart.Version}");
                EditorGUILayout.LabelField($"Difficulty: {selectedChart.Difficulty}");
                EditorGUILayout.LabelField($"Notes: {selectedChart.HitObjects?.Count ?? 0}");
                EditorGUILayout.LabelField($"Timing Points: {selectedChart.TimingPoints?.Count ?? 0}");
                EditorGUILayout.LabelField($"Length: {selectedChart.TotalLength:F1}s");
                
                if (selectedChart.HitObjects != null && selectedChart.HitObjects.Count > 0)
                {
                    var firstNote = selectedChart.HitObjects[0];
                    var lastNote = selectedChart.HitObjects[selectedChart.HitObjects.Count - 1];
                    EditorGUILayout.LabelField($"First Note: {firstNote.Time:F3}s");
                    EditorGUILayout.LabelField($"Last Note: {lastNote.Time:F3}s");
                }
            }
            
            EditorGUILayout.EndVertical();
            
            // Show current GameManager selection status
            DrawGameManagerStatus();
        }
        
        private void DrawGameManagerStatus()
        {
            if (!Application.isPlaying) return;
            
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Game Manager Status", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical("box");
            
            string currentSongInfo = gameManager.GetCurrentSongInfo();
            EditorGUILayout.LabelField($"Current Song: {currentSongInfo}");
            
            bool isManualSelection = gameManager.IsManualSelectionActive();
            EditorGUILayout.LabelField($"Manual Selection: {(isManualSelection ? "Active" : "Inactive")}");
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh Status"))
            {
                // Force a refresh of the status
                Repaint();
            }
            
            if (GUILayout.Button("Sync with Game"))
            {
                SyncWithGameManager();
                Debug.Log("Editor selection synced with GameManager");
            }
            
            if (GUILayout.Button("Clear Selection"))
            {
                ClearCurrentSelection();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            
            // Start Game button
            if (filteredSongs.Count > 0 && selectedSongIndex < filteredSongs.Count)
            {
                var selectedSong = filteredSongs[selectedSongIndex];
                var selectedChart = selectedSong.Charts != null && selectedSong.Charts.Count > 0 && selectedChartIndex < selectedSong.Charts.Count 
                    ? selectedSong.Charts[selectedChartIndex] 
                    : null;
                
                if (selectedChart != null)
                {
                    if (GUILayout.Button("🎮 Start Game", GUILayout.Height(30)))
                    {
                        StartGameWithSelectedChart(selectedSong, selectedChart);
                    }
                    
                    if (GUILayout.Button("🎵 Load Chart Only", GUILayout.Height(30)))
                    {
                        LoadChartOnly(selectedSong, selectedChart);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No chart selected.", MessageType.Warning);
                }
            }
            
            // Quick difficulty buttons
            if (filteredSongs.Count > 0 && selectedSongIndex < filteredSongs.Count)
            {
                var selectedSong = filteredSongs[selectedSongIndex];
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Quick Difficulty Selection:", EditorStyles.miniLabel);
                
                if (selectedSong.Charts != null && selectedSong.Charts.Count > 0)
                {
                    // Create a grid layout for difficulty buttons
                    int buttonsPerRow = 4; // Adjust based on available space
                    int currentRow = 0;
                    
                    for (int i = 0; i < selectedSong.Charts.Count; i++)
                    {
                        if (i % buttonsPerRow == 0)
                        {
                            if (currentRow > 0) EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            currentRow++;
                        }
                        
                        var chart = selectedSong.Charts[i];
                        string buttonText = $"{chart.Version}\n({chart.Difficulty})";
                        
                        if (GUILayout.Button(buttonText, GUILayout.Height(40)))
                        {
                            StartGameWithSelectedChart(selectedSong, chart);
                        }
                    }
                    
                    // Close the last row if we have an incomplete one
                    if (selectedSong.Charts.Count > 0)
                    {
                        EditorGUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No charts available for this song.", MessageType.Warning);
                }
            }
        }
        
        private void DrawAdvancedOptions()
        {
            EditorGUILayout.Space();
            
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
            if (showAdvancedOptions)
            {
                EditorGUILayout.BeginVertical("box");
                
                autoStartOnSelection = EditorGUILayout.Toggle("Auto-start on Selection", autoStartOnSelection);
                refreshInterval = EditorGUILayout.Slider("Refresh Interval (seconds)", refreshInterval, 1f, 30f);
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Scan for New Songs"))
                {
                    ScanForNewSongs();
                }
                
                if (GUILayout.Button("Clear Database"))
                {
                    ClearDatabase();
                }
                
                if (GUILayout.Button("Export Song List"))
                {
                    ExportSongList();
                }
                
                EditorGUILayout.EndVertical();
            }
        }
        
        private void StartGameWithSelectedChart(SongEntry song, ChartData chart)
        {
            if (Application.isPlaying)
            {
                var gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    Debug.Log($"Starting game with: {song.Title} - {chart.Version} ({chart.Difficulty})");
                    gameManager.StartGame(song, chart);
                    
                    if (autoStartOnSelection)
                    {
                        // Focus on the game view
                        EditorApplication.ExecuteMenuItem("Window/General/Game");
                    }
                }
                else
                {
                    Debug.LogError("GameManager not found in scene!");
                }
            }
            else
            {
                Debug.LogWarning("Cannot start game - scene is not playing!");
            }
        }
        
        private void LoadChartOnly(SongEntry song, ChartData chart)
        {
            if (Application.isPlaying)
            {
                var gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    Debug.Log($"Loading chart only: {song.Title} - {chart.Version} ({chart.Difficulty})");
                    gameManager.LoadChartOnly(chart);
                }
                else
                {
                    Debug.LogError("GameManager not found in scene!");
                }
            }
            else
            {
                Debug.LogWarning("Cannot load chart - scene is not playing!");
            }
        }
        
        private void ScanForNewSongs()
        {
            var chartDatabase = FindFirstObjectByType<ChartDatabase>();
            if (chartDatabase != null)
            {
                chartDatabase.ScanForSongs();
                RefreshSongDatabase();
                Debug.Log("Song scan completed!");
            }
            else
            {
                Debug.LogError("ChartDatabase not found in scene!");
            }
        }
        
        private void ClearDatabase()
        {
            if (EditorUtility.DisplayDialog("Clear Database", 
                "Are you sure you want to clear all songs from the database?", 
                "Yes", "No"))
            {
                if (songDatabase != null)
                {
                    songDatabase.ClearDatabase();
                    EditorUtility.SetDirty(songDatabase);
                    AssetDatabase.SaveAssets();
                    RefreshSongDatabase();
                    Debug.Log("Database cleared!");
                }
            }
        }
        
        private void ExportSongList()
        {
            if (songDatabase == null || songDatabase.Songs == null) return;
            
            string exportPath = EditorUtility.SaveFilePanel("Export Song List", "", "song_list", "txt");
            if (!string.IsNullOrEmpty(exportPath))
            {
                try
                {
                    var lines = new List<string>();
                    lines.Add("AshTaiko Song List Export");
                    lines.Add($"Generated: {System.DateTime.Now}");
                    lines.Add($"Total Songs: {songDatabase.Songs.Count}");
                    lines.Add("");
                    
                    foreach (var song in songDatabase.Songs)
                    {
                        lines.Add($"Song: {song.Title} - {song.Artist}");
                        lines.Add($"Creator: {song.Creator}");
                        lines.Add($"Audio: {song.AudioFilename}");
                        lines.Add($"Charts: {song.Charts?.Count ?? 0}");
                        
                        if (song.Charts != null)
                        {
                            foreach (var chart in song.Charts)
                            {
                                lines.Add($"  - {chart.Version} ({chart.Difficulty}): {chart.HitObjects?.Count ?? 0} notes");
                            }
                        }
                        lines.Add("");
                    }
                    
                    System.IO.File.WriteAllLines(exportPath, lines);
                    Debug.Log($"Song list exported to: {exportPath}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to export song list: {e.Message}");
                }
            }
        }
        
        private void CreateSongDatabase()
        {
            // Create Resources directory if it doesn't exist
            string resourcesPath = Path.Combine(Application.dataPath, "_AshTaiko", "Resources");
            if (!System.IO.Directory.Exists(resourcesPath))
            {
                System.IO.Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }
            
            // Check if database already exists
            string assetPath = "Assets/_AshTaiko/Resources/SongDatabase.asset";
            if (System.IO.File.Exists(assetPath))
            {
                Debug.LogWarning("SongDatabase already exists at: " + assetPath);
                songDatabase = AssetDatabase.LoadAssetAtPath<SongDatabase>(assetPath);
                return;
            }
            
            // Create the SongDatabase asset
            songDatabase = ScriptableObject.CreateInstance<SongDatabase>();
            
            AssetDatabase.CreateAsset(songDatabase, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Created SongDatabase at: {assetPath}");
            
            // Select the created asset
            Selection.activeObject = songDatabase;
        }

        private void LoadSelectionState()
        {
            selectedSongIndex = EditorPrefs.GetInt(SELECTED_SONG_INDEX_KEY, 0);
            selectedChartIndex = EditorPrefs.GetInt(SELECTED_CHART_INDEX_KEY, 0);
            searchQuery = EditorPrefs.GetString(SEARCH_QUERY_KEY, "");
            showAllDifficulties = EditorPrefs.GetBool(SHOW_ALL_DIFFICULTIES_KEY, true);
            
            // Load the saved difficulty filter
            Difficulty savedDifficulty = (Difficulty)EditorPrefs.GetInt(DIFFICULTY_FILTER_KEY, (int)Difficulty.Normal);
            
            // Validate that the saved difficulty is still available (will be checked after database loads)
            selectedDifficultyFilter = savedDifficulty;
        }
        
        private void SyncWithGameManager()
        {
            if (!Application.isPlaying) return;
            
            var gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null) return;
            
            var currentSong = gameManager.GetCurrentSong();
            var currentChart = gameManager.GetCurrentChart();
            
            if (currentSong != null && currentChart != null)
            {
                // Try to find this song in our filtered list
                for (int i = 0; i < filteredSongs.Count; i++)
                {
                    if (filteredSongs[i].UniqueId == currentSong.UniqueId)
                    {
                        selectedSongIndex = i;
                        
                        // Try to find the chart in this song
                        if (filteredSongs[i].Charts != null)
                        {
                            for (int j = 0; j < filteredSongs[i].Charts.Count; j++)
                            {
                                if (filteredSongs[i].Charts[j].Version == currentChart.Version)
                                {
                                    selectedChartIndex = j;
                                    break;
                                }
                            }
                        }
                        
                        Debug.Log($"Editor selection synced with GameManager: {currentSong.Title} - {currentChart.Version}");
                        break;
                    }
                }
            }
        }

        private void SaveSelectionState()
        {
            EditorPrefs.SetInt(SELECTED_SONG_INDEX_KEY, selectedSongIndex);
            EditorPrefs.SetInt(SELECTED_CHART_INDEX_KEY, selectedChartIndex);
            EditorPrefs.SetString(SEARCH_QUERY_KEY, searchQuery);
            EditorPrefs.SetBool(SHOW_ALL_DIFFICULTIES_KEY, showAllDifficulties);
            EditorPrefs.SetInt(DIFFICULTY_FILTER_KEY, (int)selectedDifficultyFilter);
        }

        private void UpdateGameManagerSelection()
        {
            if (Application.isPlaying)
            {
                var gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    if (selectedSongIndex < filteredSongs.Count)
                    {
                        var selectedSong = filteredSongs[selectedSongIndex];
                        var selectedChart = selectedSong.Charts != null && selectedSong.Charts.Count > 0 && selectedChartIndex < selectedSong.Charts.Count 
                            ? selectedSong.Charts[selectedChartIndex] 
                            : null;
                        
                        if (selectedChart != null)
                        {
                            gameManager.SetSelectedChart(selectedChart);
                        }
                    }
                }
            }
        }

        private void ClearCurrentSelection()
        {
            selectedSongIndex = 0;
            selectedChartIndex = 0;
            GUI.changed = true;
            SaveSelectionState();
            UpdateGameManagerSelection();
            Debug.Log("Editor selection cleared.");
        }

        // Method to get difficulty statistics for display
        private string GetDifficultyStatistics()
        {
            if (songDatabase == null || songDatabase.Songs == null) return "No database loaded";
            
            var difficultyStats = new Dictionary<Difficulty, int>();
            
            // Count songs for each difficulty
            foreach (var song in songDatabase.Songs)
            {
                if (song.Charts != null)
                {
                    foreach (var chart in song.Charts)
                    {
                        if (!difficultyStats.ContainsKey(chart.Difficulty))
                            difficultyStats[chart.Difficulty] = 0;
                        difficultyStats[chart.Difficulty]++;
                    }
                }
            }
            
            // Format the statistics
            var stats = difficultyStats
                .OrderBy(kvp => GetDifficultyOrder(kvp.Key))
                .Select(kvp => $"{GetDifficultyDisplayName(kvp.Key)}: {kvp.Value}")
                .ToArray();
            
            return string.Join(", ", stats);
        }
        
        // Context menu method to manually merge songs
        [ContextMenu("Refresh Difficulty List")]
        private void RefreshDifficultyListFromContextMenu()
        {
            UpdateAvailableDifficulties();
            Debug.Log($"Difficulty list refreshed manually. Available: {string.Join(", ", availableDifficultyNames)}");
        }
    }
}
