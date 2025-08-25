using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Represents a song entry containing metadata, audio references, and multiple difficulty charts.
    /// This class serves as the central data structure for storing imported song information
    /// from various formats (osu!, TJA, etc.) and provides methods for accessing song assets.
    /// </summary>
    [Serializable]
    public class SongEntry
    {
        /// <summary>
        /// Unique identifier for the song, automatically generated using GUID.
        /// </summary>
        public string UniqueId;
        
        /// <summary>
        /// Primary title of the song in the default language.
        /// </summary>
        public string Title;
        
        /// <summary>
        /// Unicode version of the song title for international character support.
        /// </summary>
        public string TitleUnicode;
        
        /// <summary>
        /// Primary artist name in the default language.
        /// </summary>
        public string Artist;
        
        /// <summary>
        /// Unicode version of the artist name for international character support.
        /// </summary>
        public string ArtistUnicode;
        
        /// <summary>
        /// Name of the chart creator/mapper.
        /// </summary>
        public string Creator;
        
        /// <summary>
        /// Source material or original work the song is based on.
        /// </summary>
        public string Source;
        
        /// <summary>
        /// List of tags for categorizing and searching songs.
        /// </summary>
        public List<string> Tags = new List<string>();
        
        /// <summary>
        /// Path to the audio file for this song.
        /// </summary>
        public string AudioFilename;
        
        /// <summary>
        /// Path to the background/cover image for this song.
        /// </summary>
        public string BackgroundImage;
        
        /// <summary>
        /// Time in seconds to start preview playback from.
        /// </summary>
        public float PreviewTime;
        
        /// <summary>
        /// Original format of the imported song file.
        /// </summary>
        public SongFormat Format;
        
        /// <summary>
        /// Collection of difficulty charts available for this song.
        /// </summary>
        public List<ChartData> Charts = new List<ChartData>();
        
        /// <summary>
        /// Initializes a new SongEntry with default values and generates a unique identifier.
        /// </summary>
        public SongEntry()
        {
            UniqueId = Guid.NewGuid().ToString();
            Charts = new List<ChartData>();
            Tags = new List<string>();
        }
        
        /// <summary>
        /// Retrieves a chart by difficulty level.
        /// </summary>
        /// <param name="difficulty">The difficulty level to search for.</param>
        /// <returns>The chart with the specified difficulty, or null if not found.</returns>
        public ChartData GetChart(Difficulty difficulty)
        {
            if (Charts == null)
            {
                Debug.LogWarning("Charts list is null, returning null");
                return null;
            }
            
            return Charts.FirstOrDefault(c => c.Difficulty == difficulty);
        }
        
        /// <summary>
        /// Retrieves a chart by version name.
        /// </summary>
        /// <param name="version">The version name to search for.</param>
        /// <returns>The chart with the specified version, or null if not found.</returns>
        public ChartData GetChart(string version)
        {
            if (Charts == null)
            {
                Debug.LogWarning("Charts list is null, returning null");
                return null;
            }
            
            return Charts.FirstOrDefault(c => c.Version == version);
        }
        
        /// <summary>
        /// Returns the best available image path for this song.
        /// Priority: BackgroundImage > null
        /// This provides a consistent way to access song visual representation.
        /// </summary>
        /// <returns>The image path if available, otherwise null.</returns>
        public string GetBestAvailableImage()
        {
            if (!string.IsNullOrEmpty(BackgroundImage))
                return BackgroundImage;
            
            return null;
        }
        
        /// <summary>
        /// Returns the full absolute path to the best available image.
        /// This method handles both relative and absolute paths, converting relative paths
        /// to absolute paths based on the song's location.
        /// </summary>
        /// <returns>The full absolute path to the image, or null if no image is available.</returns>
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
        
        /// <summary>
        /// Searches for an image file in common song directories.
        /// This handles the case where we have a relative path but need to find the actual file.
        /// </summary>
        /// <param name="imageFileName">The filename of the image to search for.</param>
        /// <returns>The full path to the found image, or the original path if not found.</returns>
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
                            return foundFiles[0];
                        }
                    }
                }
            }
            
            Debug.LogWarning($"Could not find image '{imageFileName}' in any song directories");
            // If not found, return the original path (will cause a warning but won't crash)
            return BackgroundImage;
        }
        
        /// <summary>
        /// Checks if this song has any image available.
        /// </summary>
        /// <returns>True if an image is available, false otherwise.</returns>
        public bool HasImage()
        {
            return !string.IsNullOrEmpty(GetBestAvailableImage());
        }
        
        /// <summary>
        /// Provides detailed information about the image path for debugging.
        /// This helps developers understand how image paths are being resolved.
        /// </summary>
        public void DebugImagePath()
        {
            if (!string.IsNullOrEmpty(BackgroundImage))
            {
                string fullPath = GetBestAvailableImagePath();
                Debug.Log($"Image path debug: {BackgroundImage} -> {fullPath} (exists: {System.IO.File.Exists(fullPath)})");
            }
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

