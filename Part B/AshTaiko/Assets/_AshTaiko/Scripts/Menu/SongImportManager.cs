using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace AshTaiko.Menu
{
    /// <summary>
    /// Manages song importing from OSZ and TJA files through a simple UI interface.
    /// Provides file selection dialogs and handles the import process for Windows.
    /// </summary>
    public class SongImportManager : MonoBehaviour
    {
        [Header("Import UI")]
        [SerializeField] 
        private Button _importButton;
        [SerializeField] 
        private GameObject _importMenuPanel;
        [SerializeField] 
        private Button _oszImportButton;
        [SerializeField] 
        private Button _tjaImportButton;
        [SerializeField] 
        private Button _closeImportMenuButton;
        
        [Header("Import Settings")]
        [SerializeField] 
        private string _songsDirectory = "Songs";
        
        private ChartDatabase _chartDatabase;
        
        private void Start()
        {
            SetupEventHandlers();
            HideImportMenu();
            
            // Get reference to ChartDatabase
            _chartDatabase = ChartDatabase.Instance;
            if (_chartDatabase == null)
            {
                Debug.LogError("SongImportManager: ChartDatabase not found!");
            }
        }
        
        private void SetupEventHandlers()
        {
            if (_importButton != null)
                _importButton.onClick.AddListener(ShowImportMenu);
                
            if (_oszImportButton != null)
                _oszImportButton.onClick.AddListener(ImportOszFile);
                
            if (_tjaImportButton != null)
                _tjaImportButton.onClick.AddListener(ImportTjaFile);
                
            if (_closeImportMenuButton != null)
                _closeImportMenuButton.onClick.AddListener(HideImportMenu);
        }
        
        private void ShowImportMenu()
        {
            if (_importMenuPanel != null)
            {
                _importMenuPanel.SetActive(true);
            }
        }
        
        private void HideImportMenu()
        {
            if (_importMenuPanel != null)
            {
                _importMenuPanel.SetActive(false);
            }
        }
        
        private void ImportOszFile()
        {
            string filePath = OpenFileDialog("Select OSZ file", "osz");
            if (!string.IsNullOrEmpty(filePath))
            {
                ImportOszFileInternal(filePath);
                HideImportMenu();
            }
        }
        
        private void ImportTjaFile()
        {
            string filePath = OpenFileDialog("Select TJA file", "tja");
            if (!string.IsNullOrEmpty(filePath))
            {
                ImportTjaFileInternal(filePath);
                HideImportMenu();
            }
        }
        
        private string OpenFileDialog(string title, string extension)
        {
            string initialPath = GetInitialPath();
            string filter = WindowsFileDialog.CreateFilterString($"{extension.ToUpper()} files", extension);
            
            string result = WindowsFileDialog.ShowOpenFileDialog(title, filter, initialPath, extension);
            
            return result;
        }
        
        private string GetInitialPath()
        {
            // Start from the Songs directory if it exists
            string songsPath = Path.Combine(Application.dataPath, _songsDirectory);
            if (Directory.Exists(songsPath))
            {
                return songsPath;
            }
            
            // Fallback to user's Documents folder
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
        
        private void ImportOszFileInternal(string filePath)
        {
            if (_chartDatabase == null)
            {
                Debug.LogError("Cannot import: ChartDatabase not found");
                return;
            }
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return;
            }
            
            try
            {
                Debug.Log($"Importing OSZ file: {filePath}");
                
                // Import directly using OszImporter instead of copying
                OszImporter importer = new OszImporter();
                List<SongEntry> songs = importer.ImportOszFile(filePath);
                
                if (songs != null && songs.Count > 0)
                {
                    Debug.Log($"Imported {songs.Count} songs from OSZ file");
                    
                    // Add songs to database
                    foreach (var song in songs)
                    {
                        if (song != null)
                        {
                            // Check if song already exists
                            var existingSong = _chartDatabase.GetDatabase().Songs.FirstOrDefault(s => 
                                string.Equals(s.Title, song.Title, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(s.Artist, song.Artist, StringComparison.OrdinalIgnoreCase)
                            );
                            
                            if (existingSong != null)
                            {
                                Debug.Log($"Song already exists: {song.Title} - {song.Artist}, merging charts");
                                // Merge charts logic would go here
                            }
                            else
                            {
                                _chartDatabase.GetDatabase().AddSong(song);
                                Debug.Log($"Added new song: {song.Title} - {song.Artist} ({song.Charts?.Count ?? 0} charts)");
                            }
                        }
                    }
                    
                    // Save database
                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(_chartDatabase.GetDatabase());
                    UnityEditor.AssetDatabase.SaveAssets();
                    Debug.Log("Database saved to disk");
                    #endif
                    
                    // Refresh song list if available
                    var songListManager = FindObjectOfType<SongListManager>();
                    if (songListManager != null)
                    {
                        songListManager.RefreshSongList();
                        songListManager.LoadSongDatabase();
                        Debug.Log("Song list refreshed and database reloaded");
                    }
                    else
                    {
                        Debug.LogWarning("SongListManager not found - song list may not refresh automatically");
                    }
                    
                    Debug.Log("OSZ import completed successfully");
                }
                else
                {
                    Debug.LogWarning("No songs were imported from the OSZ file");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import OSZ file: {e.Message}");
            }
        }
        
        private void ImportTjaFileInternal(string filePath)
        {
            if (_chartDatabase == null)
            {
                Debug.LogError("Cannot import: ChartDatabase not found");
                return;
            }
            
            if (!File.Exists(filePath))
            {
                Debug.LogError($"File not found: {filePath}");
                return;
            }
            
            try
            {
                Debug.Log($"Importing TJA file: {filePath}");
                
                // Import directly using TjaImporter instead of copying
                TjaImporter importer = new TjaImporter();
                SongEntry song = importer.ImportSong(filePath);
                
                if (song != null)
                {
                    Debug.Log($"Imported TJA song: {song.Title} - {song.Artist}");
                    
                    // Check if song already exists
                    var existingSong = _chartDatabase.GetDatabase().Songs.FirstOrDefault(s => 
                        string.Equals(s.Title, song.Title, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(s.Artist, song.Artist, StringComparison.OrdinalIgnoreCase)
                    );
                    
                    if (existingSong != null)
                    {
                        Debug.Log($"Song already exists: {song.Title} - {song.Artist}, merging charts");
                        // Merge charts logic would go here
                    }
                    else
                    {
                        _chartDatabase.GetDatabase().AddSong(song);
                        Debug.Log($"Added new song: {song.Title} - {song.Artist}");
                    }
                    
                    // Save database
                    #if UNITY_EDITOR
                    UnityEditor.EditorUtility.SetDirty(_chartDatabase.GetDatabase());
                    UnityEditor.AssetDatabase.SaveAssets();
                    Debug.Log("Database saved to disk");
                    #endif
                    
                    // Refresh song list if available
                    var songListManager = FindObjectOfType<SongListManager>();
                    if (songListManager != null)
                    {
                        songListManager.RefreshSongList();
                        songListManager.LoadSongDatabase();
                        Debug.Log("Song list refreshed and database reloaded");
                    }
                    else
                    {
                        Debug.LogWarning("SongListManager not found - song list may not refresh automatically");
                    }
                    
                    Debug.Log("TJA import completed successfully");
                }
                else
                {
                    Debug.LogWarning("No song was imported from the TJA file");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import TJA file: {e.Message}");
            }
        }
        
        private void OnDestroy()
        {
            if (_importButton != null)
                _importButton.onClick.RemoveListener(ShowImportMenu);
                
            if (_oszImportButton != null)
                _oszImportButton.onClick.RemoveListener(ImportOszFile);
                
            if (_tjaImportButton != null)
                _tjaImportButton.onClick.RemoveListener(ImportTjaFile);
                
            if (_closeImportMenuButton != null)
                _closeImportMenuButton.onClick.RemoveListener(HideImportMenu);
        }
    }
}
