using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AshTaiko
{
    [Serializable]
    public class SongDatabase : ScriptableObject
    {
        [SerializeField] public List<SongEntry> songs = new List<SongEntry>();
        
        public IReadOnlyList<SongEntry> Songs => songs.AsReadOnly();
        
        public List<SongEntry> GetSongsList()
        {
            return songs;
        }
        
        public void AddSong(SongEntry song)
        {
            if (songs == null)
            {
                songs = new List<SongEntry>();
            }
            
            if (!songs.Any(s => s.UniqueId == song.UniqueId))
            {
                songs.Add(song);
            }
        }
        
        public void RemoveSong(string uniqueId)
        {
            if (songs == null)
            {
                Debug.LogWarning("Songs list is null, cannot remove song");
                return;
            }
            
            songs.RemoveAll(s => s.UniqueId == uniqueId);
        }
        
        public void ClearDatabase()
        {
            if (songs != null)
            {
                int songCount = songs.Count;
                songs.Clear();
                Debug.Log($"Database cleared - {songCount} songs removed");
            }
            else
            {
                Debug.LogWarning("Songs list is null, cannot clear database");
            }
        }
        
        public int GetSongCount()
        {
            return songs != null ? songs.Count : 0;
        }
        
        public SongEntry GetSong(string uniqueId)
        {
            if (songs == null)
            {
                Debug.LogWarning("Songs list is null, returning null");
                return null;
            }
            
            return songs.FirstOrDefault(s => s.UniqueId == uniqueId);
        }
        
        public List<SongEntry> SearchSongs(string query)
        {
            if (songs == null)
            {
                Debug.LogWarning("Songs list is null, returning empty list");
                return new List<SongEntry>();
            }
            
            query = query.ToLower();
            return songs.Where(s => 
                s.Title.ToLower().Contains(query) || 
                s.Artist.ToLower().Contains(query) || 
                s.Creator.ToLower().Contains(query) ||
                s.Tags.Any(tag => tag.ToLower().Contains(query))
            ).ToList();
        }
        
        public List<SongEntry> GetSongsByDifficulty(Difficulty difficulty)
        {
            if (songs == null)
            {
                Debug.LogWarning("Songs list is null, returning empty list");
                return new List<SongEntry>();
            }
            
            return songs.Where(s => s.Charts.Any(c => c.Difficulty == difficulty)).ToList();
        }
        
        // Method to find a song by title and artist (case-insensitive)
        public SongEntry FindSongByTitleAndArtist(string title, string artist)
        {
            if (songs == null)
            {
                Debug.LogWarning("Songs list is null, returning null");
                return null;
            }
            
            return songs.FirstOrDefault(s => 
                string.Equals(s.Title, title, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(s.Artist, artist, StringComparison.OrdinalIgnoreCase)
            );
        }
        
        // Method to get all songs with their chart counts for debugging
        public void LogSongDatabaseStatus()
        {
            if (songs == null)
            {
                Debug.Log("SongDatabase: No songs loaded");
                return;
            }
            
            Debug.Log($"=== SONG DATABASE STATUS ===");
            Debug.Log($"Total songs: {songs.Count}");
            
            foreach (var song in songs)
            {
                Debug.Log($"Song: {song.Title} - {song.Artist}");
                Debug.Log($"  Charts: {song.Charts?.Count ?? 0}");
                if (song.Charts != null)
                {
                    foreach (var chart in song.Charts)
                    {
                        Debug.Log($"    - {chart.Version} ({chart.Difficulty}): {chart.HitObjects?.Count ?? 0} notes");
                    }
                }
                Debug.Log($"  Audio: {song.AudioFilename ?? "None"}");
                Debug.Log($"  Background: {song.BackgroundImage ?? "None"}");
                Debug.Log($"");
            }
            Debug.Log($"================================");
        }
    }
}
