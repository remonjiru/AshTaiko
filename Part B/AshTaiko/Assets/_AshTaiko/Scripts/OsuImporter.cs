using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Handles import of osu! beatmap (.osu) files.
    /// Parses osu! format metadata, timing points, and hit objects to create
    /// SongEntry and ChartData objects compatible with the game engine.
    /// </summary>
    public class OsuImporter
    {
        /// <summary>
        /// Raw file lines read from the osu! file.
        /// </summary>
        private string[] fileLines;
        
        /// <summary>
        /// Current line index during parsing.
        /// </summary>
        private int currentLine;
        
        /// <summary>
        /// Current section being parsed (General, Metadata, etc.).
        /// </summary>
        private string currentSection;
        
        /// <summary>
        /// Imports an osu! beatmap file and converts it to a SongEntry with ChartData.
        /// </summary>
        /// <param name="filePath">Path to the .osu file to import.</param>
        /// <returns>SongEntry containing the imported song data, or null if import failed.</returns>
        public SongEntry ImportSong(string filePath)
        {
            try
            {
                Debug.Log($"Starting import of: {filePath}");
                fileLines = File.ReadAllLines(filePath);
                currentLine = 0;
                
                SongEntry song = new SongEntry();
                song.Format = SongFormat.Osu;
                
                // Parse the file section by section
                while (currentLine < fileLines.Length)
                {
                    string line = fileLines[currentLine].Trim();
                    
                    if (line.StartsWith("[") && line.EndsWith("]"))
                    {
                        currentSection = line.Substring(1, line.Length - 2);
                        Debug.Log($"Parsing section: {currentSection}");
                        currentLine++;
                        ParseSection(song, currentSection);
                    }
                    else
                    {
                        currentLine++;
                    }
                }
                
                // Set the audio filename relative to the osu file
                string osuDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(song.AudioFilename))
                {
                    song.AudioFilename = Path.Combine(osuDirectory, song.AudioFilename);
                    Debug.Log($"Audio file: {song.AudioFilename}");
                }
                
                Debug.Log($"Import completed. Song: {song.Title}, Charts: {song.Charts.Count}");
                return song;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error importing osu! file: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Routes parsing to the appropriate section handler based on section name.
        /// </summary>
        /// <param name="song">The song entry being constructed.</param>
        /// <param name="sectionName">Name of the section to parse.</param>
        private void ParseSection(SongEntry song, string sectionName)
        {
            switch (sectionName)
            {
                case "General":
                    ParseGeneralSection(song);
                    break;
                case "Metadata":
                    ParseMetadataSection(song);
                    break;
                case "Difficulty":
                    ParseDifficultySection(song);
                    break;
                case "Events":
                    ParseEventsSection(song);
                    break;
                case "TimingPoints":
                    ParseTimingPointsSection(song);
                    break;
                case "HitObjects":
                    ParseHitObjectsSection(song);
                    break;
                default:
                    // Skip unknown sections
                    SkipSection();
                    break;
            }
        }
        
        private void ParseGeneralSection(SongEntry song)
        {
            int mode = 0; // Default to standard mode
            
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                if (line.StartsWith("[") || string.IsNullOrEmpty(line))
                    break;
                
                if (line.StartsWith("AudioFilename:"))
                {
                    song.AudioFilename = line.Substring("AudioFilename:".Length).Trim();
                }
                else if (line.StartsWith("PreviewTime:"))
                {
                    if (float.TryParse(line.Substring("PreviewTime:".Length).Trim(), out float previewTime))
                    {
                        song.PreviewTime = previewTime;
                    }
                }
                else if (line.StartsWith("Mode:"))
                {
                    if (int.TryParse(line.Substring("Mode:".Length).Trim(), out int mapMode))
                    {
                        mode = mapMode;
                        Debug.Log($"Detected map mode: {mapMode} (1=Taiko, 0=Standard)");
                    }
                }
                
                currentLine++;
            }
            
            // Store the mode for later use
            song.Tags.Add($"Mode:{mode}");
        }
        
        private void ParseMetadataSection(SongEntry song)
        {
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                if (line.StartsWith("[") || string.IsNullOrEmpty(line))
                    break;
                
                if (line.StartsWith("Title:"))
                {
                    song.Title = line.Substring("Title:".Length).Trim();
                }
                else if (line.StartsWith("TitleUnicode:"))
                {
                    song.TitleUnicode = line.Substring("TitleUnicode:".Length).Trim();
                }
                else if (line.StartsWith("Artist:"))
                {
                    song.Artist = line.Substring("Artist:".Length).Trim();
                }
                else if (line.StartsWith("ArtistUnicode:"))
                {
                    song.ArtistUnicode = line.Substring("ArtistUnicode:".Length).Trim();
                }
                else if (line.StartsWith("Creator:"))
                {
                    song.Creator = line.Substring("Creator:".Length).Trim();
                }
                else if (line.StartsWith("Source:"))
                {
                    song.Source = line.Substring("Source:".Length).Trim();
                }
                else if (line.StartsWith("Tags:"))
                {
                    string tags = line.Substring("Tags:".Length).Trim();
                    song.Tags.AddRange(tags.Split(' '));
                }
                else if (line.StartsWith("Version:"))
                {
                    // Create a new chart for this difficulty
                    ChartData chart = new ChartData();
                    chart.Version = line.Substring("Version:".Length).Trim();
                    chart.Difficulty = ParseDifficultyFromVersion(chart.Version);
                    song.Charts.Add(chart);
                }
                
                currentLine++;
            }
        }
        
        private void ParseDifficultySection(SongEntry song)
        {
            if (song.Charts.Count == 0) return;
            
            ChartData currentChart = song.Charts[song.Charts.Count - 1];
            
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                if (line.StartsWith("[") || string.IsNullOrEmpty(line))
                    break;
                
                if (line.StartsWith("HPDrainRate:"))
                {
                    if (float.TryParse(line.Substring("HPDrainRate:".Length).Trim(), out float hp))
                    {
                        currentChart.HP = hp;
                    }
                }
                else if (line.StartsWith("CircleSize:"))
                {
                    if (float.TryParse(line.Substring("CircleSize:".Length).Trim(), out float cs))
                    {
                        currentChart.CircleSize = cs;
                    }
                }
                else if (line.StartsWith("OverallDifficulty:"))
                {
                    if (float.TryParse(line.Substring("OverallDifficulty:".Length).Trim(), out float od))
                    {
                        currentChart.OverallDifficulty = od;
                    }
                }
                else if (line.StartsWith("ApproachRate:"))
                {
                    if (float.TryParse(line.Substring("ApproachRate:".Length).Trim(), out float ar))
                    {
                        currentChart.ApproachRate = ar;
                    }
                }
                else if (line.StartsWith("SliderMultiplier:"))
                {
                    if (float.TryParse(line.Substring("SliderMultiplier:".Length).Trim(), out float sm))
                    {
                        currentChart.SliderMultiplier = sm;
                    }
                }
                
                currentLine++;
            }
        }
        
        private void ParseEventsSection(SongEntry song)
        {
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                if (line.StartsWith("[") || string.IsNullOrEmpty(line))
                    break;
                
                if (line.StartsWith("0,0,\"") && IsImageFile(line))
                {
                    // Background image - extract filename from quoted path
                    int startIndex = line.IndexOf("\"") + 1;
                    int endIndex = line.LastIndexOf("\"");
                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        string imagePath = line.Substring(startIndex, endIndex - startIndex);
                        song.BackgroundImage = imagePath;
                        Debug.Log($"Found background image: {imagePath}");
                    }
                }
                
                currentLine++;
            }
        }
        
        private void ParseTimingPointsSection(SongEntry song)
        {
            if (song.Charts.Count == 0) return;
            
            ChartData currentChart = song.Charts[song.Charts.Count - 1];
            
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                if (line.StartsWith("[") || string.IsNullOrEmpty(line))
                    break;
                
                if (!line.StartsWith("//") && !string.IsNullOrEmpty(line))
                {
                    TimingPoint timingPoint = ParseTimingPoint(line);
                    if (timingPoint != null)
                    {
                        currentChart.TimingPoints.Add(timingPoint);
                        Debug.Log($"Added timing point: time={timingPoint.Time}ms, BPM={timingPoint.BPM}");
                    }
                }
                
                currentLine++;
            }
        }
        
        private void ParseHitObjectsSection(SongEntry song)
        {
            if (song.Charts.Count == 0) return;
            
            ChartData currentChart = song.Charts[song.Charts.Count - 1];
            Debug.Log($"Parsing hit objects for chart: {currentChart.Version}");
            
            int hitObjectCount = 0;
            int totalLines = 0;
            int skippedLines = 0;
            
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                if (line.StartsWith("[") || string.IsNullOrEmpty(line))
                    break;
                
                totalLines++;
                
                if (!line.StartsWith("//") && !string.IsNullOrEmpty(line))
                {
                    Debug.Log($"Parsing hit object line: {line}");
                    Debug.Log($"Line parts: {string.Join(" | ", line.Split(','))}");
                    HitObject hitObject = ParseHitObject(line, song);
                    if (hitObject != null)
                    {
                        currentChart.HitObjects.Add(hitObject);
                        hitObjectCount++;
                        Debug.Log($"Successfully added hit object: {hitObject.Type} at {hitObject.Time}s");
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to parse hit object from line: {line}");
                        skippedLines++;
                    }
                }
                else
                {
                    skippedLines++;
                }
                
                currentLine++;
            }
            
            // Calculate chart statistics
            CalculateChartStatistics(currentChart);
        }
        
        private TimingPoint ParseTimingPoint(string line)
        {
            try
            {
                string[] parts = line.Split(',');
                if (parts.Length >= 8)
                {
                    TimingPoint tp = new TimingPoint();
                    
                    if (float.TryParse(parts[0], out float time))
                        tp.Time = time;
                    
                    if (float.TryParse(parts[1], out float beatLength))
                        tp.BeatLength = beatLength;
                    
                    if (int.TryParse(parts[2], out int meter))
                        tp.Meter = meter;
                    
                    if (int.TryParse(parts[3], out int sampleSet))
                        tp.SampleSet = sampleSet;
                    
                    if (int.TryParse(parts[4], out int sampleIndex))
                        tp.SampleIndex = sampleIndex;
                    
                    if (int.TryParse(parts[5], out int volume))
                        tp.Volume = volume;
                    
                    if (int.TryParse(parts[6], out int uninherited))
                        tp.Uninherited = uninherited == 1;
                    
                    if (int.TryParse(parts[7], out int effects))
                        tp.Effects = effects;
                    
                    return tp;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to parse timing point: {line}, Error: {e.Message}");
            }
            
            return null;
        }
        
        private HitObject ParseHitObject(string line, SongEntry song)
        {
            try
            {
                string[] parts = line.Split(',');
                Debug.Log($"Hit object parts count: {parts.Length}");
                
                if (parts.Length >= 4)
                {
                    // For Taiko mode, we only care about certain hit object types
                    int type = int.Parse(parts[3]);
                    Debug.Log($"Hit object type: {type} (binary: {Convert.ToString(type, 2)})");
                    
                    // Check if it's a circle (type 1) or slider (type 2)
                    if ((type & 1) == 1 || (type & 2) == 2)
                    {
                        float time = float.Parse(parts[2]);
                        NoteType noteType = DetermineNoteType(type, parts, song);
                        
                        // Convert from milliseconds to seconds
                        float timeInSeconds = time / 1000f;
                        
                        Debug.Log($"Creating hit object: time={time}ms -> {timeInSeconds}s, type={noteType}");
                        
                        HitObject hitObject = new HitObject(timeInSeconds, 1f, noteType);
                        return hitObject;
                    }
                    else
                    {
                        Debug.Log($"Skipping hit object type {type} (binary: {Convert.ToString(type, 2)}) - not circle or slider");
                        // Log what this type might be
                        if ((type & 4) == 4) Debug.Log("  - This appears to be a spinner");
                        if ((type & 8) == 8) Debug.Log("  - This appears to be a hold note");
                        if ((type & 16) == 16) Debug.Log("  - This appears to be a mania note");
                    }
                }
                else
                {
                    Debug.LogWarning($"Hit object line doesn't have enough parts: {parts.Length} < 4");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to parse hit object: {line}, Error: {e.Message}");
            }
            
            return null;
        }
        
        private NoteType DetermineNoteType(int type, string[] parts, SongEntry song)
        {
            // In osu! Taiko mode:
            // type & 1 = circle (don/ka)
            // type & 2 = slider (drumroll)
            // Note types are determined by HITSOUNDS, not position!
            //
            // osu! hit object format for circles:
            // x,y,time,type,hitsound,additions,extras
            // hitsound: 0=normal, 2=whistle, 8=clap
            // additions: 0=none, 4=finish (large note)
            //
            // For Taiko:
            // - Normal hitsound = Don (center/red)
            // - Whistle/Clap hitsound = Ka (rim/blue)
            // - Finish addition = Large variant
            
            if ((type & 2) == 2)
            {
                // Slider - drumroll
                Debug.Log($"Found drumroll note at x={parts[0]}");
                
                // Check if it's a large drumroll (finish hitsound)
                int additions = 0;
                if (parts.Length >= 6)
                {
                    if (int.TryParse(parts[5], out int parsedAdditions))
                        additions = parsedAdditions;
                }
                
                bool isLarge = (additions & 4) == 4; // Finish = 4
                if (isLarge)
                {
                    Debug.Log($"Large drumroll detected");
                    return NoteType.DrumrollBig;
                }
                
                return NoteType.Drumroll;
            }
            else if ((type & 1) == 1)
            {
                // Circle - determine don/ka based on HITSOUNDS
                // Default values in case parts are missing
                int hitsound = 0;
                int additions = 0;
                
                // Parse hitsound information if available
                if (parts.Length >= 5)
                {
                    if (int.TryParse(parts[4], out int parsedHitsound))
                        hitsound = parsedHitsound;
                }
                
                if (parts.Length >= 6)
                {
                    if (int.TryParse(parts[5], out int parsedAdditions))
                        additions = parsedAdditions;
                }
                
                Debug.Log($"Circle note - hitsound: {hitsound}, additions: {additions}");
                
                // Check if it's a large note (finish hitsound)
                bool isLarge = (additions & 4) == 4; // Finish = 4
                
                // Determine base note type from normal hitsound
                NoteType baseType;
                if ((hitsound & 2) == 2 || (hitsound & 8) == 8) // Whistle = 2, Clap = 8
                {
                    // Whistle or Clap hitsound = Ka (rim/blue)
                    baseType = NoteType.Ka;
                    Debug.Log($"Whistle/Clap hitsound -> Ka");
                }
                else
                {
                    // Normal hitsound = Don (center/red)
                    baseType = NoteType.Don;
                    Debug.Log($"Normal hitsound -> Don");
                }
                
                // Convert to large variant if finish hitsound is present
                if (isLarge)
                {
                    Debug.Log($"Finish hitsound detected -> Large {baseType}");
                    switch (baseType)
                    {
                        case NoteType.Don:
                            return NoteType.DonBig;
                        case NoteType.Ka:
                            return NoteType.KaBig;
                        default:
                            return baseType;
                    }
                }
                else
                {
                    return baseType;
                }
            }
            
            Debug.Log($"Default fallback -> Don");
            return NoteType.Don; // Default fallback
        }
        
        private Difficulty ParseDifficultyFromVersion(string version)
        {
            version = version.ToLower();
            
            if (version.Contains("easy"))
                return Difficulty.Easy;
            else if (version.Contains("normal"))
                return Difficulty.Normal;
            else if (version.Contains("hard"))
                return Difficulty.Hard;
            else if (version.Contains("insane"))
                return Difficulty.Insane;
            else if (version.Contains("expert"))
                return Difficulty.Expert;
            else if (version.Contains("master"))
                return Difficulty.Master;
            else
                return Difficulty.Normal; // Default fallback
        }
        
        private void CalculateChartStatistics(ChartData chart)
        {
            if (chart.HitObjects.Count > 0)
            {
                // Calculate total length
                float lastNoteTime = 0;
                foreach (var hitObject in chart.HitObjects)
                {
                    if (hitObject.Time > lastNoteTime)
                    {
                        lastNoteTime = hitObject.Time;
                    }
                }
                chart.TotalLength = lastNoteTime;
                
                // Calculate max combo (simplified - just count notes)
                chart.MaxCombo = chart.HitObjects.Count;
            }
        }
        
        private void SkipSection()
        {
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                if (line.StartsWith("[") || string.IsNullOrEmpty(line))
                    break;
                currentLine++;
            }
        }
        
        /*
            IsImageFile checks if a line contains a reference to an image file.
            This supports common image formats used in osu! beatmaps.
        */
        private bool IsImageFile(string line)
        {
            string lowerLine = line.ToLower();
            return lowerLine.Contains(".jpg") || 
                   lowerLine.Contains(".jpeg") || 
                   lowerLine.Contains(".png") || 
                   lowerLine.Contains(".bmp") || 
                   lowerLine.Contains(".gif");
        }
    }
}
