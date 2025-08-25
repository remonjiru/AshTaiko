using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshTaiko
{
    [Serializable]
    public class SongEntry
    {
        public string UniqueId; // Unique identifier for the song
        public string Title;
        public string TitleUnicode;
        public string Artist;
        public string ArtistUnicode;
        public string Creator;
        public string Source;
        public List<string> Tags = new List<string>();
        public string AudioFilename;
        public string BackgroundImage;
        public float PreviewTime;
        public SongFormat Format;
        public List<ChartData> Charts = new List<ChartData>();
        
        public SongEntry()
        {
            UniqueId = Guid.NewGuid().ToString();
            Charts = new List<ChartData>();
            Tags = new List<string>();
        }
        
        public ChartData GetChart(Difficulty difficulty)
        {
            if (Charts == null)
            {
                Debug.LogWarning("Charts list is null, returning null");
                return null;
            }
            
            return Charts.FirstOrDefault(c => c.Difficulty == difficulty);
        }
        
        public ChartData GetChart(string version)
        {
            if (Charts == null)
            {
                Debug.LogWarning("Charts list is null, returning null");
                return null;
            }
            
            return Charts.FirstOrDefault(c => c.Version == version);
        }
        
        /*
            GetBestAvailableImage returns the best available image path for this song.
            Priority: BackgroundImage > null
            This provides a consistent way to access song visual representation.
        */
        public string GetBestAvailableImage()
        {
            if (!string.IsNullOrEmpty(BackgroundImage))
                return BackgroundImage;
            
            return null;
        }
        
        /*
            GetBestAvailableImagePath returns the full absolute path to the best available image.
            This method handles both relative and absolute paths, converting relative paths
            to absolute paths based on the song's location.
        */
        public string GetBestAvailableImagePath()
        {
            if (string.IsNullOrEmpty(BackgroundImage))
                return null;
            
            // If the path is already absolute, return it as-is
            if (System.IO.Path.IsPathRooted(BackgroundImage))
                return BackgroundImage;
            
            // For relative paths, we need to construct the full path
            // Since we don't have the song's directory stored, we'll need to search for it
            return FindImageInSongDirectories(BackgroundImage);
        }
        
        /*
            FindImageInSongDirectories searches for an image file in common song directories.
            This handles the case where we have a relative path but need to find the actual file.
        */
        private string FindImageInSongDirectories(string imageFileName)
        {
            // Common song directories to search
            string[] searchDirectories = {
                System.IO.Path.Combine(Application.dataPath, "Songs"),
                System.IO.Path.Combine(Application.dataPath, "..", "Songs"),
                System.IO.Path.Combine(Application.dataPath, "..", "..", "Songs")
            };
            
            foreach (string directory in searchDirectories)
            {
                if (System.IO.Directory.Exists(directory))
                {
                    // Search recursively in the directory
                    string[] foundFiles = System.IO.Directory.GetFiles(directory, imageFileName, System.IO.SearchOption.AllDirectories);
                    if (foundFiles.Length > 0)
                    {
                        Debug.Log($"Found image '{imageFileName}' at: {foundFiles[0]}");
                        return foundFiles[0]; // Return the first match
                    }
                }
            }
            
            // If not found, try searching with different case variations (for cross-platform compatibility)
            string[] caseVariations = {
                imageFileName.ToLower(),
                imageFileName.ToUpper(),
                System.IO.Path.GetFileNameWithoutExtension(imageFileName).ToLower() + System.IO.Path.GetExtension(imageFileName).ToLower(),
                System.IO.Path.GetFileNameWithoutExtension(imageFileName).ToUpper() + System.IO.Path.GetExtension(imageFileName).ToUpper()
            };
            
            foreach (string directory in searchDirectories)
            {
                if (System.IO.Directory.Exists(directory))
                {
                    foreach (string variation in caseVariations)
                    {
                        string[] foundFiles = System.IO.Directory.GetFiles(directory, variation, System.IO.SearchOption.AllDirectories);
                        if (foundFiles.Length > 0)
                        {
                            Debug.Log($"Found image '{variation}' (case variation of '{imageFileName}') at: {foundFiles[0]}");
                            return foundFiles[0];
                        }
                    }
                }
            }
            
            Debug.LogWarning($"Could not find image '{imageFileName}' in any song directories");
            // If not found, return the original path (will cause a warning but won't crash)
            return BackgroundImage;
        }
        
        /*
            HasImage checks if this song has any image available.
        */
        public bool HasImage()
        {
            return !string.IsNullOrEmpty(GetBestAvailableImage());
        }
        
        /*
            DebugImagePath provides detailed information about the image path for debugging.
            This helps developers understand how image paths are being resolved.
        */
        public void DebugImagePath()
        {
            Debug.Log($"=== IMAGE PATH DEBUG INFO ===");
            Debug.Log($"Song: {Title} - {Artist}");
            Debug.Log($"Raw BackgroundImage: {BackgroundImage}");
            Debug.Log($"IsPathRooted: {System.IO.Path.IsPathRooted(BackgroundImage)}");
            
            if (!string.IsNullOrEmpty(BackgroundImage))
            {
                string fullPath = GetBestAvailableImagePath();
                Debug.Log($"Resolved Full Path: {fullPath}");
                Debug.Log($"File Exists: {System.IO.File.Exists(fullPath)}");
                
                if (!System.IO.File.Exists(fullPath))
                {
                    Debug.LogWarning($"Image file not found at resolved path!");
                }
            }
            Debug.Log($"================================");
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Debug Image Path")]
        private void DebugImagePathContextMenu()
        {
            DebugImagePath();
        }
        #endif
    }
}

