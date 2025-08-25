using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Handles import of compressed .osz files containing osu! beatmaps.
    /// Extracts the ZIP archive and processes the contained .osu files using the existing OsuImporter.
    /// </summary>
    public class OszImporter
    {
        /// <summary>
        /// Imports a .osz file by extracting it and processing all contained .osu files.
        /// </summary>
        /// <param name="oszFilePath">Path to the .osz file to import.</param>
        /// <returns>List of imported songs, or empty list if import failed.</returns>
        public List<SongEntry> ImportOszFile(string oszFilePath)
        {
            List<SongEntry> importedSongs = new List<SongEntry>();
            
            try
            {
                Debug.Log($"Starting import of .osz file: {oszFilePath}");
                
                // Verify the file exists and is a valid .osz file
                if (!File.Exists(oszFilePath))
                {
                    Debug.LogError($"OSZ file not found: {oszFilePath}");
                    return importedSongs;
                }
                
                if (!oszFilePath.EndsWith(".osz", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogError($"File is not a .osz file: {oszFilePath}");
                    return importedSongs;
                }
                
                // Create a temporary directory for extraction
                string tempExtractPath = CreateTempExtractDirectory(oszFilePath);
                if (string.IsNullOrEmpty(tempExtractPath))
                {
                    Debug.LogError("Failed to create temporary extraction directory");
                    return importedSongs;
                }
                
                try
                {
                    // Extract the .osz file
                    ExtractOszFile(oszFilePath, tempExtractPath);
                    
                    // Find and process all .osu files in the extracted directory
                    importedSongs = ProcessExtractedOsuFiles(tempExtractPath);
                    
                    // Copy necessary files to permanent location before cleanup
                    if (importedSongs.Count > 0)
                    {
                        CopyFilesToPermanentLocation(importedSongs, tempExtractPath);
                    }
                    
                    Debug.Log($"Successfully imported {importedSongs.Count} songs from .osz file");
                }
                finally
                {
                    // Clean up temporary files
                    CleanupTempDirectory(tempExtractPath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error importing .osz file {oszFilePath}: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
            
            return importedSongs;
        }
        
        /// <summary>
        /// Creates a temporary directory for extracting .osz contents.
        /// </summary>
        /// <param name="oszFilePath">Path to the .osz file (used for naming).</param>
        /// <returns>Path to the created temporary directory, or null if creation failed.</returns>
        private string CreateTempExtractDirectory(string oszFilePath)
        {
            try
            {
                string fileName = Path.GetFileNameWithoutExtension(oszFilePath);
                string tempDir = Path.Combine(Path.GetTempPath(), "AshTaiko_OszImport", fileName + "_" + Guid.NewGuid().ToString("N"));
                
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }
                
                Debug.Log($"Created temporary extraction directory: {tempDir}");
                return tempDir;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create temporary directory: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Extracts the contents of a .osz file to the specified directory.
        /// </summary>
        /// <param name="oszFilePath">Path to the .osz file.</param>
        /// <param name="extractPath">Directory to extract contents to.</param>
        private void ExtractOszFile(string oszFilePath, string extractPath)
        {
            try
            {
                Debug.Log($"Extracting .osz file to: {extractPath}");
                
                using (ZipArchive archive = ZipFile.OpenRead(oszFilePath))
                {
                    int extractedFiles = 0;
                    int totalFiles = archive.Entries.Count;
                    
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        // Skip directory entries and hidden files
                        if (string.IsNullOrEmpty(entry.Name) || entry.Name.StartsWith("."))
                            continue;
                        
                        // Create the full path for the extracted file
                        string entryPath = Path.Combine(extractPath, entry.Name);
                        string entryDir = Path.GetDirectoryName(entryPath);
                        
                        // Ensure the directory exists
                        if (!string.IsNullOrEmpty(entryDir) && !Directory.Exists(entryDir))
                        {
                            Directory.CreateDirectory(entryDir);
                        }
                        
                        // Extract the file
                        entry.ExtractToFile(entryPath, true);
                        extractedFiles++;
                        
                        if (extractedFiles % 10 == 0 || extractedFiles == totalFiles)
                        {
                            Debug.Log($"Extracted {extractedFiles}/{totalFiles} files from .osz");
                        }
                    }
                    
                    Debug.Log($"Extraction complete: {extractedFiles} files extracted");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to extract .osz file: {e.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Processes all .osu files found in the extracted directory.
        /// Groups multiple difficulties into single song entries.
        /// </summary>
        /// <param name="extractPath">Path to the extracted directory.</param>
        /// <returns>List of successfully imported songs.</returns>
        private List<SongEntry> ProcessExtractedOsuFiles(string extractPath)
        {
            List<SongEntry> importedSongs = new List<SongEntry>();
            Dictionary<string, SongEntry> songsByKey = new Dictionary<string, SongEntry>();
            
            try
            {
                // Find all .osu files in the extracted directory
                string[] osuFiles = Directory.GetFiles(extractPath, "*.osu", SearchOption.AllDirectories);
                Debug.Log($"Found {osuFiles.Length} .osu files in extracted directory");
                
                if (osuFiles.Length == 0)
                {
                    Debug.LogWarning("No .osu files found in extracted .osz file");
                    return importedSongs;
                }
                
                // Process each .osu file using the existing OsuImporter
                OsuImporter osuImporter = new OsuImporter();
                
                foreach (string osuFile in osuFiles)
                {
                    try
                    {
                        Debug.Log($"Processing extracted .osu file: {osuFile}");
                        
                        // Import the .osu file
                        SongEntry tempSong = osuImporter.ImportSong(osuFile);
                        
                        if (tempSong != null)
                        {
                            // Update file paths to be relative to the extracted directory
                            UpdateSongFilePaths(tempSong, extractPath, osuFile);
                            
                            // Create a unique key for this song based on title and artist
                            string songKey = $"{tempSong.Title?.Trim()}_{tempSong.Artist?.Trim()}".ToLowerInvariant();
                            
                            if (songsByKey.ContainsKey(songKey))
                            {
                                // Song already exists, merge the charts
                                var existingSong = songsByKey[songKey];
                                Debug.Log($"Merging charts for existing song: {tempSong.Title} - {tempSong.Artist}");
                                
                                // Add all charts from tempSong to the existing song
                                if (tempSong.Charts != null)
                                {
                                    foreach (var chart in tempSong.Charts)
                                    {
                                        existingSong.Charts.Add(chart);
                                        Debug.Log($"  Added chart: {chart.Version} - {chart.Difficulty}");
                                    }
                                }
                                
                                // Update other metadata if missing
                                if (string.IsNullOrEmpty(existingSong.AudioFilename) && !string.IsNullOrEmpty(tempSong.AudioFilename))
                                    existingSong.AudioFilename = tempSong.AudioFilename;
                                if (string.IsNullOrEmpty(existingSong.BackgroundImage) && !string.IsNullOrEmpty(tempSong.BackgroundImage))
                                    existingSong.BackgroundImage = tempSong.BackgroundImage;
                            }
                            else
                            {
                                // New song
                                songsByKey[songKey] = tempSong;
                                Debug.Log($"Added new song: {tempSong.Title} - {tempSong.Artist} with {tempSong.Charts?.Count ?? 0} charts");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Failed to import .osu file: {osuFile}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error processing .osu file {osuFile}: {e.Message}");
                    }
                }
                
                // Convert the dictionary values to a list
                importedSongs.AddRange(songsByKey.Values);
                Debug.Log($"Final result: {importedSongs.Count} unique songs with multiple difficulties");
                
                // Log the final chart counts for each song
                foreach (var song in importedSongs)
                {
                    Debug.Log($"Song: {song.Title} - {song.Artist} has {song.Charts?.Count ?? 0} charts");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing extracted .osu files: {e.Message}");
            }
            
            return importedSongs;
        }
        
        /// <summary>
        /// Updates file paths in the song entry to be relative to the extracted directory.
        /// This ensures that audio and image files can be found after extraction.
        /// </summary>
        /// <param name="song">The song entry to update.</param>
        /// <param name="extractPath">Path to the extracted directory.</param>
        /// <param name="osuFilePath">Path to the .osu file being processed.</param>
        private void UpdateSongFilePaths(SongEntry song, string extractPath, string osuFilePath)
        {
            try
            {
                // Get the directory containing the .osu file
                string osuDirectory = Path.GetDirectoryName(osuFilePath);
                
                // Update audio filename path
                if (!string.IsNullOrEmpty(song.AudioFilename))
                {
                    // Check if the audio file exists in the osu directory
                    string audioPath = Path.Combine(osuDirectory, song.AudioFilename);
                    if (File.Exists(audioPath))
                    {
                        song.AudioFilename = audioPath;
                        Debug.Log($"Updated audio path: {song.AudioFilename}");
                    }
                    else
                    {
                        Debug.LogWarning($"Audio file not found at expected path: {audioPath}");
                    }
                }
                
                // Update background image path
                if (!string.IsNullOrEmpty(song.BackgroundImage))
                {
                    // Check if the image file exists in the osu directory
                    string imagePath = Path.Combine(osuDirectory, song.BackgroundImage);
                    if (File.Exists(imagePath))
                    {
                        song.BackgroundImage = imagePath;
                        Debug.Log($"Updated background image path: {song.BackgroundImage}");
                    }
                    else
                    {
                        Debug.LogWarning($"Background image not found at expected path: {imagePath}");
                    }
                }
                
                // Add the extraction directory as a tag for reference
                song.Tags.Add($"ExtractedFrom:{extractPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating song file paths: {e.Message}");
            }
        }
        
        /// <summary>
        /// Copies necessary files (audio, images, etc.) to a permanent location.
        /// </summary>
        /// <param name="songs">List of imported songs that need file copying.</param>
        /// <param name="extractPath">Path where files were extracted.</param>
        private void CopyFilesToPermanentLocation(List<SongEntry> songs, string extractPath)
        {
            try
            {
                // Create permanent songs directory
                string songsDir = Path.Combine(Application.dataPath, "Songs");
                if (!Directory.Exists(songsDir))
                {
                    Directory.CreateDirectory(songsDir);
                }
                
                foreach (var song in songs)
                {
                    if (song == null) continue;
                    
                    // Create song-specific directory
                    string songDir = Path.Combine(songsDir, $"{song.Title}_{song.Artist}".Replace(" ", "_").Replace("/", "_").Replace("\\", "_"));
                    if (!Directory.Exists(songDir))
                    {
                        Directory.CreateDirectory(songDir);
                    }
                    
                    // Copy audio file
                    if (!string.IsNullOrEmpty(song.AudioFilename))
                    {
                        string sourceAudioPath = song.AudioFilename;
                        string audioFileName = Path.GetFileName(sourceAudioPath);
                        string destAudioPath = Path.Combine(songDir, audioFileName);
                        
                        if (File.Exists(sourceAudioPath))
                        {
                            File.Copy(sourceAudioPath, destAudioPath, true);
                            song.AudioFilename = destAudioPath;
                            Debug.Log($"Copied audio file: {destAudioPath}");
                        }
                    }
                    
                    // Copy background image
                    if (!string.IsNullOrEmpty(song.BackgroundImage))
                    {
                        string sourceImagePath = song.BackgroundImage;
                        string imageFileName = Path.GetFileName(sourceImagePath);
                        string destImagePath = Path.Combine(songDir, imageFileName);
                        
                        if (File.Exists(sourceImagePath))
                        {
                            File.Copy(sourceImagePath, destImagePath, true);
                            song.BackgroundImage = destImagePath;
                            Debug.Log($"Copied background image: {destImagePath}");
                        }
                    }
                    
                    // Copy any other referenced files (hit sounds, etc.)
                    // This would need to be expanded based on what files are referenced in the .osu files
                    
                    // Copy .osu files for this song
                    var osuFiles = Directory.GetFiles(extractPath, "*.osu", SearchOption.AllDirectories);
                    foreach (var osuFile in osuFiles)
                    {
                        // Check if this .osu file belongs to this song by checking the content
                        if (FileContainsSongData(osuFile, song))
                        {
                            string osuFileName = Path.GetFileName(osuFile);
                            string destOsuPath = Path.Combine(songDir, osuFileName);
                            
                            File.Copy(osuFile, destOsuPath, true);
                            Debug.Log($"Copied .osu file: {destOsuPath}");
                        }
                    }
                }
                
                Debug.Log("Successfully copied all necessary files to permanent location");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error copying files to permanent location: {e.Message}");
            }
        }
        
        /// <summary>
        /// Checks if a .osu file contains data for a specific song.
        /// </summary>
        /// <param name="osuFilePath">Path to the .osu file to check.</param>
        /// <param name="song">Song to check against.</param>
        /// <returns>True if the file contains data for this song, false otherwise.</returns>
        private bool FileContainsSongData(string osuFilePath, SongEntry song)
        {
            try
            {
                string[] lines = File.ReadAllLines(osuFilePath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("Title:"))
                    {
                        string title = line.Substring(6).Trim();
                        if (string.Equals(title, song.Title, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error checking .osu file content: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Cleans up the temporary extraction directory and all its contents.
        /// </summary>
        /// <param name="tempPath">Path to the temporary directory to clean up.</param>
        private void CleanupTempDirectory(string tempPath)
        {
            try
            {
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                    Debug.Log($"Cleaned up temporary directory: {tempPath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to clean up temporary directory {tempPath}: {e.Message}");
                // Don't throw here as cleanup failure shouldn't prevent import success
            }
        }
        
        /// <summary>
        /// Validates that a file is a valid .osz file by checking its header.
        /// </summary>
        /// <param name="filePath">Path to the file to validate.</param>
        /// <returns>True if the file appears to be a valid .osz file, false otherwise.</returns>
        public bool IsValidOszFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;
                
                // Check file extension
                if (!filePath.EndsWith(".osz", StringComparison.OrdinalIgnoreCase))
                    return false;
                
                // Check if it's a valid ZIP file by trying to open it
                using (ZipArchive archive = ZipFile.OpenRead(filePath))
                {
                    // Look for at least one .osu file in the archive
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.Name.EndsWith(".osu", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch
            {
                // If we can't open it as a ZIP or any other error occurs, it's not valid
                return false;
            }
        }
    }
}
