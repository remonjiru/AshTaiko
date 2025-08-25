using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

namespace AshTaiko
{
    /// <summary>
    /// Manages the central chart database for the taiko rhythm game.
    /// Handles importing songs from various formats (osu!, TJA, .osz), merging duplicate songs,
    /// and providing access to the song collection. Uses a singleton pattern for global access.
    /// </summary>
    public class ChartDatabase : MonoBehaviour
    {
        [SerializeField] 
        private SongDatabase database;
        
        [SerializeField] 
        private string songsDirectory = "Songs";
        
        /// <summary>
        /// Singleton instance providing global access to the chart database.
        /// </summary>
        public static ChartDatabase Instance { get; private set; }
        
        /// <summary>
        /// Sets up the singleton instance and loads the database.
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadDatabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Loads the existing database from Resources or creates a new one.
        /// </summary>
        private void LoadDatabase()
        {
            // Try to load existing database from Resources
            database = Resources.Load<SongDatabase>("SongDatabase");
            
            if (database == null)
            {
                // Create new database if none exists
                database = ScriptableObject.CreateInstance<SongDatabase>();
                Debug.Log("Created new SongDatabase instance");
            }
            else
            {
                Debug.Log("Loaded existing SongDatabase from Resources");
            }
        }
        
        /// <summary>
        /// Scans the Songs directory for chart files and imports them.
        /// Supports .osz, .osu, and .tja file formats.
        /// </summary>
        public void ScanForSongs()
        {
            if (database == null)
            {
                Debug.LogError("Database is null, cannot scan for songs");
                return;
            }
            
            string fullPath = Path.Combine(Application.dataPath, songsDirectory);
            
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                return;
            }
            
            // Scan for .osz files first (compressed osu! beatmaps)
            string[] oszFiles = Directory.GetFiles(fullPath, "*.osz", SearchOption.AllDirectories);
            foreach (string oszFile in oszFiles)
            {
                ImportOszFile(oszFile);
            }
            
            // Scan for individual .osu files
            string[] osuFiles = Directory.GetFiles(fullPath, "*.osu", SearchOption.AllDirectories);
            foreach (string osuFile in osuFiles)
            {
                ImportOsuFile(osuFile);
            }
            
            // Scan for TJA files
            string[] tjaFiles = Directory.GetFiles(fullPath, "*.tja", SearchOption.AllDirectories);
            foreach (string tjaFile in tjaFiles)
            {
                ImportTjaFile(tjaFile);
            }
            
            SaveDatabase();
        }
        
        /// <summary>
        /// Imports an individual osu! beatmap file.
        /// </summary>
        /// <param name="filePath">Path to the .osu file to import.</param>
        private void ImportOsuFile(string filePath)
        {
            try
            {
                Debug.Log($"Importing osu! file: {filePath}");
                OsuImporter importer = new OsuImporter();
                SongEntry song = importer.ImportSong(filePath);
                
                if (song != null)
                {
                    Debug.Log($"Imported: {song.Title} - {song.Artist} ({song.Charts.Count} charts)");
                    
                    // Check if this song already exists in the database
                    SongEntry existingSong = FindExistingSong(song.Title, song.Artist);
                    if (existingSong != null)
                    {
                        // Merge charts from the new song into the existing one
                        int chartsBefore = existingSong.Charts.Count;
                        MergeCharts(existingSong, song);
                        int chartsAfter = existingSong.Charts.Count;
                        Debug.Log($"Merged charts: {existingSong.Title} ({chartsBefore} -> {chartsAfter} charts)");
                    }
                    else
                    {
                        // Add as new song
                        database.AddSong(song);
                        Debug.Log($"Added new song: {song.Title} - {song.Artist}");
                    }
                }
                else
                {
                    Debug.LogError($"OsuImporter returned null for file: {filePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import osu! file {filePath}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Imports a .osz file containing compressed osu! beatmaps.
        /// </summary>
        /// <param name="oszFilePath">Path to the .osz file to import.</param>
        private void ImportOszFile(string oszFilePath)
        {
            try
            {
                Debug.Log($"Importing .osz file: {oszFilePath}");
                OszImporter importer = new OszImporter();
                List<SongEntry> songs = importer.ImportOszFile(oszFilePath);
                
                if (songs != null && songs.Count > 0)
                {
                    Debug.Log($"Imported {songs.Count} songs from .osz file");
                    
                    foreach (SongEntry song in songs)
                    {
                        if (song != null)
                        {
                            // Check if this song already exists in the database
                            SongEntry existingSong = FindExistingSong(song.Title, song.Artist);
                            if (existingSong != null)
                            {
                                // Merge charts from the new song into the existing one
                                int chartsBefore = existingSong.Charts.Count;
                                MergeCharts(existingSong, song);
                                int chartsAfter = existingSong.Charts.Count;
                                Debug.Log($"Merged charts: {existingSong.Title} ({chartsBefore} -> {chartsAfter} charts)");
                            }
                            else
                            {
                                // Add as new song
                                database.AddSong(song);
                                Debug.Log($"Added new song: {song.Title} - {song.Artist}");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"No songs imported from .osz file: {oszFilePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import .osz file {oszFilePath}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Imports a TJA chart file.
        /// </summary>
        /// <param name="filePath">Path to the .tja file to import.</param>
        private void ImportTjaFile(string filePath)
        {
            try
            {
                TjaImporter importer = new TjaImporter();
                SongEntry song = importer.ImportSong(filePath);
                
                if (song != null)
                {
                    database.AddSong(song);
                    Debug.Log($"Imported TJA: {song.Title} - {song.Artist}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import TJA file {filePath}: {e.Message}");
            }
        }
        
        /// <summary>
        /// Saves the database to disk using Unity's asset system.
        /// </summary>
        private void SaveDatabase()
        {
            if (database != null)
            {
                // Mark the database as dirty so Unity knows it needs to be saved
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(database);
                UnityEditor.AssetDatabase.SaveAssets();
                #endif
                Debug.Log("Database saved to disk");
            }
        }
        
        /// <summary>
        /// Gets a random song from the database.
        /// </summary>
        /// <returns>A random SongEntry, or null if the database is empty.</returns>
        public SongEntry GetRandomSong()
        {
            if (database == null || database.Songs.Count == 0) return null;
            
            int randomIndex = UnityEngine.Random.Range(0, database.Songs.Count);
            return database.Songs[randomIndex];
        }
        
        /// <summary>
        /// Gets a song by its index in the database.
        /// </summary>
        /// <param name="index">The index of the song to retrieve.</param>
        /// <returns>The SongEntry at the specified index, or null if the index is invalid.</returns>
        public SongEntry GetSongByIndex(int index)
        {
            if (database == null || index < 0 || index >= database.Songs.Count)
            {
                return null;
            }
            return database.Songs[index];
        }
        
        /// <summary>
        /// Gets the total number of songs in the database.
        /// </summary>
        /// <returns>The number of songs, or 0 if the database is null.</returns>
        public int GetSongCount()
        {
            if (database == null)
            {
                Debug.LogWarning("Database is null, returning 0");
                return 0;
            }
            return database.Songs.Count;
        }
        
        /// <summary>
        /// Gets the underlying SongDatabase instance.
        /// </summary>
        /// <returns>The SongDatabase instance.</returns>
        public SongDatabase GetDatabase()
        {
            return database;
        }
        
        /// <summary>
        /// Gets a read-only list of all songs in the database.
        /// </summary>
        /// <returns>A read-only list of songs, or an empty list if the database is null.</returns>
        public IReadOnlyList<SongEntry> GetSongs()
        {
            if (database == null)
            {
                Debug.LogWarning("Database is null, returning empty list");
                return new List<SongEntry>().AsReadOnly();
            }
            return database.Songs;
        }
        
        /// <summary>
        /// Finds an existing song by title and artist using case-insensitive comparison.
        /// </summary>
        /// <param name="title">The song title to search for.</param>
        /// <param name="artist">The artist name to search for.</param>
        /// <returns>The matching SongEntry, or null if no match is found.</returns>
        private SongEntry FindExistingSong(string title, string artist)
        {
            if (database == null || database.Songs == null) return null;
            
            // Try to find a song with matching title and artist
            // Use case-insensitive comparison for better matching
            return database.Songs.FirstOrDefault(s => 
                string.Equals(s.Title, title, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Artist, artist, StringComparison.OrdinalIgnoreCase)
            );
        }
        
        /// <summary>
        /// Merges charts from a new song into an existing one, avoiding duplicates.
        /// Also merges metadata like audio files, background images, and tags.
        /// </summary>
        /// <param name="existingSong">The existing song to merge into.</param>
        /// <param name="newSong">The new song to merge from.</param>
        private void MergeCharts(SongEntry existingSong, SongEntry newSong)
        {
            if (existingSong == null || newSong == null) return;
            
            // Merge charts that don't already exist
            foreach (var newChart in newSong.Charts)
            {
                // Check if this chart version already exists
                bool chartExists = existingSong.Charts.Any(existingChart => 
                    string.Equals(existingChart.Version, newChart.Version, StringComparison.OrdinalIgnoreCase)
                );
                
                if (!chartExists)
                {
                    existingSong.Charts.Add(newChart);
                    Debug.Log($"Added chart '{newChart.Version}' to existing song '{existingSong.Title}'");
                }
                else
                {
                    Debug.Log($"Chart '{newChart.Version}' already exists in song '{existingSong.Title}', skipping duplicate");
                }
            }
            
            // Merge other metadata if the existing song is missing some
            if (string.IsNullOrEmpty(existingSong.AudioFilename) && !string.IsNullOrEmpty(newSong.AudioFilename))
            {
                existingSong.AudioFilename = newSong.AudioFilename;
            }
            
            if (string.IsNullOrEmpty(existingSong.BackgroundImage) && !string.IsNullOrEmpty(newSong.BackgroundImage))
            {
                existingSong.BackgroundImage = newSong.BackgroundImage;
            }
            
            // Merge tags
            if (newSong.Tags != null)
            {
                foreach (var tag in newSong.Tags)
                {
                    if (!existingSong.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    {
                        existingSong.Tags.Add(tag);
                    }
                }
            }
        }
        
        /// <summary>
        /// Manually merges duplicate songs in the database.
        /// Useful for fixing existing databases with duplicate entries.
        /// </summary>
        public void MergeSongsManually()
        {
            if (database == null || database.Songs == null) return;
            
            Debug.Log("Starting manual song merge process...");
            int originalSongCount = database.Songs.Count;
            
            // Create a list of songs to remove (they will be merged into others)
            List<SongEntry> songsToRemove = new List<SongEntry>();
            
            // Check each song against others for potential merging
            for (int i = 0; i < database.Songs.Count; i++)
            {
                var song1 = database.Songs[i];
                if (song1 == null) continue;
                
                for (int j = i + 1; j < database.Songs.Count; j++)
                {
                    var song2 = database.Songs[j];
                    if (song2 == null) continue;
                    
                    // Check if these songs should be merged
                    if (string.Equals(song1.Title, song2.Title, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(song1.Artist, song2.Artist, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Log($"Found duplicate: {song1.Title} - {song1.Artist} ({song1.Charts?.Count ?? 0} + {song2.Charts?.Count ?? 0} charts)");
                        
                        // Merge song2 into song1
                        MergeCharts(song1, song2);
                        songsToRemove.Add(song2);
                    }
                }
            }
            
            // Remove the merged songs
            foreach (var songToRemove in songsToRemove)
            {
                database.RemoveSong(songToRemove.UniqueId);
            }
            
            int finalSongCount = database.Songs.Count;
            Debug.Log($"Manual merge complete: {originalSongCount} -> {finalSongCount} songs, merged {songsToRemove.Count} songs");
        }
        
        /// <summary>
        /// Logs the current database status for debugging purposes.
        /// </summary>
        [ContextMenu("Log Database Status")]
        private void LogDatabaseStatus()
        {
            if (database != null)
            {
                database.LogSongDatabaseStatus();
            }
            else
            {
                Debug.LogWarning("No database loaded");
            }
        }
        
        /// <summary>
        /// Triggers manual song merging from the Unity context menu.
        /// </summary>
        [ContextMenu("Merge Duplicate Songs")]
        private void MergeSongsFromContextMenu()
        {
            MergeSongsManually();
        }
    }
}
