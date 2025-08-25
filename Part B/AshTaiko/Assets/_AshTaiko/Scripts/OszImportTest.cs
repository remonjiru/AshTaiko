using UnityEngine;
using System.IO;

namespace AshTaiko
{
    /// <summary>
    /// Test script for validating .osz file import functionality.
    /// This can be attached to a GameObject in the scene for testing purposes.
    /// </summary>
    public class OszImportTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private string testOszPath = "";
        [SerializeField] private bool testOnStart = false;
        
        private OszImporter _oszImporter;
        
        private void Start()
        {
            _oszImporter = new OszImporter();
            
            if (testOnStart && !string.IsNullOrEmpty(testOszPath))
            {
                TestOszImport();
            }
        }
        
        /// <summary>
        /// Tests the .osz import functionality with the specified file path.
        /// </summary>
        [ContextMenu("Test OSZ Import")]
        public void TestOszImport()
        {
            if (string.IsNullOrEmpty(testOszPath))
            {
                Debug.LogWarning("No test .osz path specified. Please set the testOszPath field.");
                return;
            }
            
            if (!File.Exists(testOszPath))
            {
                Debug.LogError($"Test .osz file not found: {testOszPath}");
                return;
            }
            
            Debug.Log($"=== TESTING OSZ IMPORT ===");
            Debug.Log($"File: {testOszPath}");
            Debug.Log($"File size: {new FileInfo(testOszPath).Length / 1024f:F1} KB");
            
            // Test validation
            bool isValid = _oszImporter.IsValidOszFile(testOszPath);
            Debug.Log($"Is valid .osz file: {isValid}");
            
            if (isValid)
            {
                // Test import
                var songs = _oszImporter.ImportOszFile(testOszPath);
                Debug.Log($"Import result: {songs.Count} songs imported");
                
                foreach (var song in songs)
                {
                    if (song != null)
                    {
                        Debug.Log($"Song: {song.Title} - {song.Artist}");
                        Debug.Log($"  Charts: {song.Charts.Count}");
                        Debug.Log($"  Audio: {song.AudioFilename ?? "None"}");
                        Debug.Log($"  Background: {song.BackgroundImage ?? "None"}");
                        
                        foreach (var chart in song.Charts)
                        {
                            Debug.Log($"    Chart '{chart.Version}': {chart.HitObjects.Count} notes, {chart.TimingPoints.Count} timing points");
                        }
                    }
                }
            }
            
            Debug.Log("=== END OSZ IMPORT TEST ===");
        }
        
        /// <summary>
        /// Tests .osz file validation without performing import.
        /// </summary>
        [ContextMenu("Test OSZ Validation")]
        public void TestOszValidation()
        {
            if (string.IsNullOrEmpty(testOszPath))
            {
                Debug.LogWarning("No test .osz path specified. Please set the testOszPath field.");
                return;
            }
            
            bool isValid = _oszImporter.IsValidOszFile(testOszPath);
            Debug.Log($"OSZ validation result for '{testOszPath}': {isValid}");
        }
        
        /// <summary>
        /// Scans the Songs directory for .osz files and reports their status.
        /// </summary>
        [ContextMenu("Scan Songs Directory for OSZ Files")]
        public void ScanForOszFiles()
        {
            string songsPath = Path.Combine(Application.dataPath, "Songs");
            
            if (!Directory.Exists(songsPath))
            {
                Debug.LogWarning("Songs directory does not exist");
                return;
            }
            
            string[] oszFiles = Directory.GetFiles(songsPath, "*.osz", SearchOption.AllDirectories);
            Debug.Log($"Found {oszFiles.Length} .osz files in Songs directory:");
            
            foreach (string oszFile in oszFiles)
            {
                bool isValid = _oszImporter.IsValidOszFile(oszFile);
                long fileSize = new FileInfo(oszFile).Length;
                
                Debug.Log($"  {Path.GetFileName(oszFile)}: {(isValid ? "Valid" : "Invalid")}, {fileSize / 1024f:F1} KB");
            }
        }
        
        /// <summary>
        /// Sets the test path to a file selected through the file dialog.
        /// </summary>
        [ContextMenu("Select Test OSZ File")]
        public void SelectTestOszFile()
        {
            #if UNITY_EDITOR
            string selectedPath = UnityEditor.EditorUtility.OpenFilePanel("Select .osz file", "", "osz");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                testOszPath = selectedPath;
                Debug.Log($"Selected test .osz file: {testOszPath}");
                UnityEditor.EditorUtility.SetDirty(this);
            }
            #else
            Debug.LogWarning("File selection is only available in the Unity Editor");
            #endif
        }
    }
}
