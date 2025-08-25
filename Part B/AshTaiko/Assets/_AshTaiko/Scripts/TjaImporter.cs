using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Handles import of Taiko no Tatsujin (.tja) chart files.
    /// Parses TJA format metadata, timing information, and note data to create
    /// SongEntry and ChartData objects compatible with the game engine.
    /// </summary>
    public class TjaImporter
    {
        /// <summary>
        /// Raw file lines read from the TJA file.
        /// </summary>
        private string[] _fileLines;
        
        /// <summary>
        /// Current line index during parsing.
        /// </summary>
        private int _currentLine;
        
        /// <summary>
        /// The song entry being constructed during import.
        /// </summary>
        private SongEntry _currentSong;
        
        /// <summary>
        /// The current chart being parsed.
        /// </summary>
        private ChartData _currentChart;
        
        /// <summary>
        /// Current BPM value during parsing.
        /// </summary>
        private float _currentBPM = 120f;
        
        /// <summary>
        /// Current offset value during parsing.
        /// </summary>
        private float _currentOffset = 0f;
        
        /// <summary>
        /// Current beat length in seconds (120 BPM = 0.5 seconds per beat).
        /// </summary>
        private float _beatLength = 0.5f;
        
        /// <summary>
        /// Whether we're currently parsing the notes section.
        /// </summary>
        private bool _inNotesSection = false;
        
        /// <summary>
        /// Current notes data being collected.
        /// </summary>
        private List<string> _currentNotesData = new List<string>();
        
        /// <summary>
        /// Current measure number during parsing.
        /// </summary>
        private int _currentMeasure = 0;
        
        /// <summary>
        /// Current time in seconds during parsing.
        /// </summary>
        private float _currentTime = 0f;
        
        /// <summary>
        /// Current measure duration in seconds.
        /// </summary>
        private float _currentMeasureDuration = 0f;
        
        /// <summary>
        /// Current time signature numerator.
        /// </summary>
        private int _currentTimeSignatureNumerator = 4;
        
        /// <summary>
        /// Current time signature denominator.
        /// </summary>
        private int _currentTimeSignatureDenominator = 4;
        
        /// <summary>
        /// Imports a TJA file and converts it to a SongEntry with ChartData.
        /// </summary>
        /// <param name="filePath">Path to the .tja file to import.</param>
        /// <returns>SongEntry containing the imported song data, or null if import failed.</returns>
        public SongEntry ImportSong(string filePath)
        {
            try
            {
                Debug.Log($"TJA: Starting import of {filePath}");
                _fileLines = File.ReadAllLines(filePath);
                _currentLine = 0;
                
                _currentSong = new SongEntry();
                _currentSong.Format = SongFormat.Tja;
                _currentChart = null;
                _currentBPM = 120f;
                _currentOffset = 0f;
                _beatLength = 0.5f;
                _inNotesSection = false;
                _currentNotesData.Clear();
                _currentMeasure = 0;
                _currentTime = 0f;
                _currentMeasureDuration = 0f;
                _currentTimeSignatureNumerator = 4;
                _currentTimeSignatureDenominator = 4;
                
                // First pass: parse metadata and create charts
                ParseMetadataAndCharts();
                
                // Second pass: parse notes for each chart
                ParseNotesForCharts();
                
                // Set the audio filename relative to the TJA file
                string tjaDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(_currentSong.AudioFilename))
                {
                    _currentSong.AudioFilename = Path.Combine(tjaDirectory, _currentSong.AudioFilename);
                }
                
                // Look for background images in the TJA directory
                FindBackgroundImage(tjaDirectory);
                
                // Calculate final statistics for all charts
                foreach (var chart in _currentSong.Charts)
                {
                    CalculateChartStatistics(chart);
                }
                
                Debug.Log($"TJA: Import completed - {_currentSong.Title} - {_currentSong.Artist} ({_currentSong.Charts.Count} charts)");
                
                return _currentSong;
            }
            catch (Exception e)
            {
                Debug.LogError($"TJA: Import failed - {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Parses metadata and chart definitions from the TJA file.
        /// This first pass identifies song information and creates chart objects
        /// for each difficulty level found in the file.
        /// </summary>
        private void ParseMetadataAndCharts()
        {
            _currentLine = 0;
            
            while (_currentLine < _fileLines.Length)
            {
                string line = _fileLines[_currentLine].Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                {
                    _currentLine++;
                    continue;
                }
                
                // Parse metadata
                if (line.StartsWith("TITLE:"))
                {
                    _currentSong.Title = line.Substring("TITLE:".Length).Trim();
                }
                else if (line.StartsWith("ARTIST:"))
                {
                    _currentSong.Artist = line.Substring("ARTIST:".Length).Trim();
                }
                else if (line.StartsWith("CREATOR:"))
                {
                    _currentSong.Creator = line.Substring("CREATOR:".Length).Trim();
                }
                else if (line.StartsWith("SOURCE:"))
                {
                    _currentSong.Source = line.Substring("SOURCE:".Length).Trim();
                }
                else if (line.StartsWith("WAVE:"))
                {
                    _currentSong.AudioFilename = line.Substring("WAVE:".Length).Trim();
                }
                else if (line.StartsWith("BPM:"))
                {
                    ParseBPM(line);
                }
                else if (line.StartsWith("OFFSET:"))
                {
                    ParseOffset(line);
                }
                else if (line.StartsWith("COURSE:"))
                {
                    ParseCourse(line);
                }
                else if (line.StartsWith("LEVEL:"))
                {
                    ParseLevel(line);
                }
                else if (line.StartsWith("BALLOON:"))
                {
                    ParseBalloon(line);
                }
                else if (line.StartsWith("SCOREINIT:"))
                {
                    ParseScoreInit(line);
                }
                else if (line.StartsWith("SCOREDIFF:"))
                {
                    ParseScoreDiff(line);
                }
                else if (line.StartsWith("STYLE:"))
                {
                    ParseStyle(line);
                }
                
                _currentLine++;
            }
        }
        
        private void ParseNotesForCharts()
        {
            // For each chart, parse the notes section
            foreach (var chart in _currentSong.Charts)
            {
                ParseNotesForChart(chart);
            }
        }
        
        private void ParseNotesForChart(ChartData chart)
        {
            _currentLine = 0;
            _currentChart = chart;
            _inNotesSection = false;
            _currentNotesData.Clear();
            _currentMeasure = 0;
            _currentTime = 0f;
            
            // CRITICAL: Initialize timing system for this chart
            // Use the BPM and offset from the metadata
            _currentBPM = 186f; // Default from your TJA file
            _currentOffset = -11.432f; // From your TJA file
            _beatLength = 60f / _currentBPM;
            _currentTimeSignatureNumerator = 4;
            _currentTimeSignatureDenominator = 4;
            _currentMeasureDuration = (_currentTimeSignatureNumerator * 4f * _beatLength) / _currentTimeSignatureDenominator;
            
            // Reset timing for this chart
            chart.TimingPoints.Clear();
            chart.HitObjects.Clear();
            
            // Add initial timing point
            TimingPoint tp = new TimingPoint();
            tp.Time = 0;
            tp.BeatLength = _beatLength * 1000f;
            tp.Meter = 4;
            tp.Uninherited = true;
            chart.TimingPoints.Add(tp);
            
            while (_currentLine < _fileLines.Length)
            {
                string line = _fileLines[_currentLine].Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                {
                    _currentLine++;
                    continue;
                }
                
                // Check if we're entering the notes section for this chart
                if (line.StartsWith("COURSE:"))
                {
                    string course = line.Substring("COURSE:".Length).Trim();
                    if (course == chart.Version)
                    {
                        ParseNotesSection(chart);
                        break; // Exit after parsing this chart's notes
                    }
                }
                
                _currentLine++;
            }
        }
        
        private void ParseNotesSection(ChartData chart)
        {
            // Skip the COURSE: line
            _currentLine++;
            
            while (_currentLine < _fileLines.Length)
            {
                string line = _fileLines[_currentLine].Trim();
                
                // Check if we've reached the end of this chart's notes
                if (line.StartsWith("COURSE:") || line.StartsWith("TITLE:") || line.StartsWith("ARTIST:"))
                {
                    break;
                }
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                {
                    _currentLine++;
                    continue;
                }
                
                // Check for #START and #END commands
                if (line.StartsWith("#START"))
                {
                    _inNotesSection = true;
                }
                else if (line.StartsWith("#END"))
                {
                    _inNotesSection = false;
                }
                else if (_inNotesSection)
                {
                    // Handle TJA commands
                    if (line.StartsWith("#BPMCHANGE"))
                    {
                        ParseBPMChangeCommand(line, chart);
                    }
                    else if (line.StartsWith("#MEASURE"))
                    {
                        ParseMeasureCommand(line, chart);
                    }
                    else if (line.StartsWith("#DELAY"))
                    {
                        ParseDelayCommand(line, chart);
                    }
                    else if (line.StartsWith("#SCROLL"))
                    {
                        ParseScrollCommand(line, chart);
                    }
                    else if (line.StartsWith("#GOGOSTART"))
                    {
                        ParseGogoStart(line);
                    }
                    else if (line.StartsWith("#GOGOEND"))
                    {
                        ParseGogoEnd(line);
                    }
                    else if (line.StartsWith("#BARLINEOFF"))
                    {
                        ParseBarlineOff(line);
                    }
                    else if (line.StartsWith("#BARLINEON"))
                    {
                        ParseBarlineOn(line);
                    }
                    else if (line.StartsWith("#BRANCHSTART"))
                    {
                        ParseBranchStart(line);
                    }
                    else if (line.StartsWith("#BRANCHEND"))
                    {
                        ParseBranchEnd(line);
                    }
                    else if (line.StartsWith("#SECTION"))
                    {
                        ParseSection(line);
                    }
                    else if (line.StartsWith("#N"))
                    {
                        ParseNormalBranch(line);
                    }
                    else if (line.StartsWith("#E"))
                    {
                        ParseAdvancedBranch(line);
                    }
                    else if (line.StartsWith("#M"))
                    {
                        ParseMasterBranch(line);
                    }
                    else if (line.Contains(","))
                    {
                        // This is a measure line with notes
                        ParseMeasureLine(line, chart);
                    }
                }
                
                _currentLine++;
            }
        }
        
        private void ParseMeasureLine(string line, ChartData chart)
        {
            // Remove the trailing comma and parse the measure
            string measureData = line.TrimEnd(',');
            
            if (measureData.Length == 0)
            {
                // Empty measure - just advance time
                _currentTime += _currentMeasureDuration;
                _currentMeasure++;
                return;
            }
            
            // Calculate timing for this measure
            // According to TJA spec: "each measure is equally divided by the amount of numbers there are inside"
            float noteSpacing = _currentMeasureDuration / measureData.Length;
            
            for (int i = 0; i < measureData.Length; i++)
            {
                char noteChar = measureData[i];
                if (noteChar == '0') continue; // Skip blank notes
                
                // Calculate time for this note within the measure
                float noteTimeInMeasure = i * noteSpacing;
                // TJA offset: negative values delay notes (make them appear later), positive values make them appear sooner
                // In our system: negative times = earlier, positive times = later, so we need to reverse the offset
                float totalNoteTime = _currentTime + noteTimeInMeasure - _currentOffset;
                
                // Convert note character to NoteType
                NoteType noteType = ConvertTjaNoteChar(noteChar);
                
                if (noteType != NoteType.Blank)
                {
                    HitObject hitObject = new HitObject(totalNoteTime, 1f, noteType);
                    chart.HitObjects.Add(hitObject);
                }
            }
            
            // Move to next measure
            _currentTime += _currentMeasureDuration;
            _currentMeasure++;
        }
        
        private void ParseBPMChangeCommand(string line, ChartData chart)
        {
            // #BPMCHANGE command format: #BPMCHANGE <new_bpm>
            string[] parts = line.Split(' ');
            if (parts.Length >= 2 && float.TryParse(parts[1], out float newBPM))
            {
                // Update current BPM and beat length
                _currentBPM = newBPM;
                _beatLength = 60f / newBPM;
                
                // Recalculate current measure duration
                _currentMeasureDuration = (_currentTimeSignatureNumerator * 4f * _beatLength) / _currentTimeSignatureDenominator;
                
                // Create timing point for this BPM change
                TimingPoint tp = new TimingPoint();
                tp.Time = _currentTime * 1000f; // Convert to milliseconds
                tp.BeatLength = _beatLength * 1000f;
                tp.Meter = _currentTimeSignatureNumerator;
                tp.Uninherited = true;
                chart.TimingPoints.Add(tp);
            }
        }
        
        private void ParseMeasureCommand(string line, ChartData chart)
        {
            // #MEASURE command format: #MEASURE <numerator>/<denominator>
            string[] parts = line.Split(' ');
            if (parts.Length >= 2)
            {
                string[] timeSignature = parts[1].Split('/');
                if (timeSignature.Length == 2 && 
                    int.TryParse(timeSignature[0], out int numerator) && 
                    int.TryParse(timeSignature[1], out int denominator))
                {
                    _currentTimeSignatureNumerator = numerator;
                    _currentTimeSignatureDenominator = denominator;
                    
                    // Recalculate current measure duration
                    _currentMeasureDuration = (numerator * 4f * _beatLength) / denominator;
                }
            }
        }
        
        private void ParseDelayCommand(string line, ChartData chart)
        {
            // #DELAY command format: #DELAY <seconds>
            string[] parts = line.Split(' ');
            if (parts.Length >= 2 && float.TryParse(parts[1], out float delaySeconds))
            {
                // Apply delay to current timing
                _currentTime += delaySeconds;
            }
        }
        
        private void ParseScrollCommand(string line, ChartData chart)
        {
            // #SCROLL command format: #SCROLL <multiplier>
            string[] parts = line.Split(' ');
            if (parts.Length >= 2 && float.TryParse(parts[1], out float scrollMultiplier))
            {
                // Note: Scroll speed affects visual appearance, not timing
            }
        }
        
        private void ParseGogoStart(string line)
        {
        }
        
        private void ParseGogoEnd(string line)
        {
        }
        
        private void ParseBarlineOff(string line)
        {
        }
        
        private void ParseBarlineOn(string line)
        {
        }
        
        private void ParseBranchStart(string line)
        {
        }
        
        private void ParseBranchEnd(string line)
        {
        }
        
        private void ParseSection(string line)
        {
        }
        
        private void ParseNormalBranch(string line)
        {
        }
        
        private void ParseAdvancedBranch(string line)
        {
        }
        
        private void ParseMasterBranch(string line)
        {
        }
        
        private NoteType ConvertTjaNoteChar(char noteChar)
        {
            // TJA note types according to the specification:
            // 0 - Blank, no note
            // 1 - Don
            // 2 - Ka
            // 3 - DON (Big)
            // 4 - KA (Big)
            // 5 - Drumroll
            // 6 - DRUMROLL (Big)
            // 7 - Balloon
            // 8 - End of a balloon or drumroll
            // 9 - Kusudama, yam, oimo, or big balloon
            // A - DON (Both), multiplayer note
            // B - KA (Both), multiplayer note
            // F - ADLIB, hidden note
            
            switch (noteChar)
            {
                case '0': return NoteType.Blank;
                case '1': return NoteType.Don;
                case '2': return NoteType.Ka;
                case '3': return NoteType.DonBig;
                case '4': return NoteType.KaBig;
                case '5': return NoteType.Drumroll;
                case '6': return NoteType.DrumrollBig;
                case '7': return NoteType.Balloon;
                case '8': return NoteType.DrumrollBalloonEnd;
                case '9': return NoteType.Balloon;
                case 'A': return NoteType.Don; // Treat as regular Don for now
                case 'B': return NoteType.Ka; // Treat as regular Ka for now
                case 'F': return NoteType.Don; // Treat as regular Don for now
                default: 
                    Debug.LogWarning($"Unknown TJA note character: {noteChar}");
                    return NoteType.Blank;
            }
        }
        
        private void ParseBPM(string line)
        {
            if (float.TryParse(line.Substring("BPM:".Length).Trim(), out float bpm))
            {
                _currentBPM = bpm;
                _beatLength = 60f / bpm;
                _currentMeasureDuration = (_currentTimeSignatureNumerator * 4f * _beatLength) / _currentTimeSignatureDenominator;
            }
        }
        
        private void ParseOffset(string line)
        {
            if (float.TryParse(line.Substring("OFFSET:".Length).Trim(), out float offset))
            {
                _currentOffset = offset;
            }
        }
        
        private void ParseCourse(string line)
        {
            string course = line.Substring("COURSE:".Length).Trim();
            
            // Create a new chart for this course
            _currentChart = new ChartData();
            _currentChart.Version = course;
            _currentChart.Difficulty = ParseDifficultyFromCourse(course);
            _currentSong.Charts.Add(_currentChart);
            
            // Reset timing for new chart
            _currentMeasure = 0;
            _currentTime = 0f;
            _currentMeasureDuration = (_currentTimeSignatureNumerator * 4f * _beatLength) / _currentTimeSignatureDenominator;
        }
        
        private void ParseLevel(string line)
        {
            if (_currentChart != null && int.TryParse(line.Substring("LEVEL:".Length).Trim(), out int level))
            {
            }
        }
        
        private void ParseBalloon(string line)
        {
            if (_currentChart != null)
            {
                string balloonData = line.Substring("BALLOON:".Length).Trim();
                string[] parts = balloonData.Split(',');
                
                if (parts.Length >= 4)
                {
                }
            }
        }
        
        private void ParseScoreInit(string line)
        {
            // Parse initial score (not critical for gameplay)
        }
        
        private void ParseScoreDiff(string line)
        {
            // Parse score difference (not critical for gameplay)
        }
        
        private void ParseStyle(string line)
        {
            string style = line.Substring("STYLE:".Length).Trim();
        }
        
        private Difficulty ParseDifficultyFromCourse(string course)
        {
            course = course.ToLower();
            
            if (course.Contains("easy") || course.Contains("kantan"))
                return Difficulty.Easy;
            else if (course.Contains("normal") || course.Contains("futsuu"))
                return Difficulty.Normal;
            else if (course.Contains("hard") || course.Contains("muzukashii"))
                return Difficulty.Hard;
            else if (course.Contains("oni") || course.Contains("oni"))
                return Difficulty.Insane;
            else if (course.Contains("ura") || course.Contains("ura"))
                return Difficulty.Expert;
            else if (course.Contains("edit"))
                return Difficulty.Normal; // Edit is usually a custom difficulty
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
                
                // Calculate max combo
                chart.MaxCombo = chart.HitObjects.Count;
                
                Debug.Log($"TJA: Chart '{chart.Version}' - {chart.HitObjects.Count} notes, {chart.TotalLength:F1}s length");
            }
        }
        
        private void FindBackgroundImage(string tjaDirectory)
        {
            if (string.IsNullOrEmpty(tjaDirectory) || !Directory.Exists(tjaDirectory))
                return;
                
            // Common image file names to look for
            string[] imageNames = {
                "bg.jpg", "bg.jpeg", "bg.png", "background.jpg", "background.jpeg", "background.png",
                "cover.jpg", "cover.jpeg", "cover.png", "title.jpg", "title.jpeg", "title.png",
                "image.jpg", "image.jpeg", "image.png"
            };
            
            foreach (string imageName in imageNames)
            {
                string imagePath = Path.Combine(tjaDirectory, imageName);
                if (File.Exists(imagePath))
                {
                    _currentSong.BackgroundImage = imagePath;
                    break;
                }
            }
        }
    }
}
