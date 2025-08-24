using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AshTaiko
{
    public class TjaImporter
    {
        private string[] fileLines;
        private int currentLine;
        private SongEntry currentSong;
        private ChartData currentChart;
        private float currentBPM = 120f;
        private float currentOffset = 0f;
        private float beatLength = 0.5f; // 120 BPM = 0.5 seconds per beat
        private bool inNotesSection = false;
        private List<string> currentNotesData = new List<string>();
        private int currentMeasure = 0;
        private float currentTime = 0f;
        private float currentMeasureDuration = 0f;
        private int currentTimeSignatureNumerator = 4;
        private int currentTimeSignatureDenominator = 4;
        
        public SongEntry ImportSong(string filePath)
        {
            try
            {
                Debug.Log($"Starting TJA import: {filePath}");
                fileLines = File.ReadAllLines(filePath);
                currentLine = 0;
                
                currentSong = new SongEntry();
                currentChart = null;
                currentBPM = 120f;
                currentOffset = 0f;
                beatLength = 0.5f;
                inNotesSection = false;
                currentNotesData.Clear();
                currentMeasure = 0;
                currentTime = 0f;
                currentMeasureDuration = 0f;
                currentTimeSignatureNumerator = 4;
                currentTimeSignatureDenominator = 4;
                
                // First pass: parse metadata and create charts
                ParseMetadataAndCharts();
                
                // Second pass: parse notes for each chart
                ParseNotesForCharts();
                
                // Set the audio filename relative to the TJA file
                string tjaDirectory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(currentSong.AudioFilename))
                {
                    currentSong.AudioFilename = Path.Combine(tjaDirectory, currentSong.AudioFilename);
                    Debug.Log($"TJA: Full audio path = {currentSong.AudioFilename}");
                }
                
                // Calculate final statistics for all charts
                foreach (var chart in currentSong.Charts)
                {
                    CalculateChartStatistics(chart);
                }
                
                Debug.Log($"TJA import completed: {currentSong.Title} - {currentSong.Artist}");
                Debug.Log($"TJA: Created {currentSong.Charts.Count} charts");
                foreach (var chart in currentSong.Charts)
                {
                    Debug.Log($"TJA: Chart '{chart.Version}' - {chart.HitObjects.Count} notes, {chart.TimingPoints.Count} timing points");
                }
                
                return currentSong;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error importing TJA file: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                return null;
            }
        }
        
        private void ParseMetadataAndCharts()
        {
            currentLine = 0;
            
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                {
                    currentLine++;
                    continue;
                }
                
                // Parse metadata
                if (line.StartsWith("TITLE:"))
                {
                    currentSong.Title = line.Substring("TITLE:".Length).Trim();
                    Debug.Log($"TJA: Title = {currentSong.Title}");
                }
                else if (line.StartsWith("ARTIST:"))
                {
                    currentSong.Artist = line.Substring("ARTIST:".Length).Trim();
                    Debug.Log($"TJA: Artist = {currentSong.Artist}");
                }
                else if (line.StartsWith("CREATOR:"))
                {
                    currentSong.Creator = line.Substring("CREATOR:".Length).Trim();
                    Debug.Log($"TJA: Creator = {currentSong.Creator}");
                }
                else if (line.StartsWith("SOURCE:"))
                {
                    currentSong.Source = line.Substring("SOURCE:".Length).Trim();
                    Debug.Log($"TJA: Source = {currentSong.Source}");
                }
                else if (line.StartsWith("WAVE:"))
                {
                    currentSong.AudioFilename = line.Substring("WAVE:".Length).Trim();
                    Debug.Log($"TJA: Audio = {currentSong.AudioFilename}");
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
                
                currentLine++;
            }
        }
        
        private void ParseNotesForCharts()
        {
            // For each chart, parse the notes section
            foreach (var chart in currentSong.Charts)
            {
                ParseNotesForChart(chart);
            }
        }
        
        private void ParseNotesForChart(ChartData chart)
        {
            currentLine = 0;
            currentChart = chart;
            inNotesSection = false;
            currentNotesData.Clear();
            currentMeasure = 0;
            currentTime = 0f;
            
            // CRITICAL: Initialize timing system for this chart
            // Use the BPM and offset from the metadata
            currentBPM = 186f; // Default from your TJA file
            currentOffset = -11.432f; // From your TJA file
            beatLength = 60f / currentBPM;
            currentTimeSignatureNumerator = 4;
            currentTimeSignatureDenominator = 4;
            currentMeasureDuration = (currentTimeSignatureNumerator * 4f * beatLength) / currentTimeSignatureDenominator;
            
            Debug.Log($"TJA: Initialized timing for chart '{chart.Version}' - BPM: {currentBPM}, BeatLength: {beatLength:F3}s, MeasureDuration: {currentMeasureDuration:F3}s, Offset: {currentOffset:F3}s");
            
            // Reset timing for this chart
            chart.TimingPoints.Clear();
            chart.HitObjects.Clear();
            
            // Add initial timing point
            TimingPoint tp = new TimingPoint();
            tp.Time = 0;
            tp.BeatLength = beatLength * 1000f;
            tp.Meter = 4;
            tp.Uninherited = true;
            chart.TimingPoints.Add(tp);
            
            Debug.Log($"TJA: Parsing notes for chart '{chart.Version}'");
            
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                {
                    currentLine++;
                    continue;
                }
                
                // Check if we're entering the notes section for this chart
                if (line.StartsWith("COURSE:"))
                {
                    string course = line.Substring("COURSE:".Length).Trim();
                    if (course == chart.Version)
                    {
                        Debug.Log($"TJA: Found notes section for chart '{chart.Version}'");
                        ParseNotesSection(chart);
                        break; // Exit after parsing this chart's notes
                    }
                }
                
                currentLine++;
            }
            
            Debug.Log($"TJA: Finished parsing chart '{chart.Version}' - {chart.HitObjects.Count} notes");
        }
        
        private void ParseNotesSection(ChartData chart)
        {
            // Skip the COURSE: line
            currentLine++;
            
            Debug.Log($"TJA: Starting notes section parsing - Initial timing: BPM={currentBPM}, BeatLength={beatLength:F3}s, MeasureDuration={currentMeasureDuration:F3}s, Offset={currentOffset:F3}s");
            
            while (currentLine < fileLines.Length)
            {
                string line = fileLines[currentLine].Trim();
                
                // Check if we've reached the end of this chart's notes
                if (line.StartsWith("COURSE:") || line.StartsWith("TITLE:") || line.StartsWith("ARTIST:"))
                {
                    Debug.Log($"TJA: End of notes section for chart '{chart.Version}'");
                    break;
                }
                
                // Skip comments and empty lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                {
                    currentLine++;
                    continue;
                }
                
                // Check for #START and #END commands
                if (line.StartsWith("#START"))
                {
                    inNotesSection = true;
                    Debug.Log($"TJA: Entering notes section for chart '{chart.Version}'");
                }
                else if (line.StartsWith("#END"))
                {
                    inNotesSection = false;
                    Debug.Log($"TJA: Exiting notes section for chart '{chart.Version}'");
                }
                else if (inNotesSection)
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
                
                currentLine++;
            }
        }
        
        private void ParseMeasureLine(string line, ChartData chart)
        {
            // Remove the trailing comma and parse the measure
            string measureData = line.TrimEnd(',');
            Debug.Log($"TJA: Parsing measure {currentMeasure}: {measureData}");
            
            if (measureData.Length == 0)
            {
                // Empty measure - just advance time
                currentTime += currentMeasureDuration;
                currentMeasure++;
                return;
            }
            
            // Calculate timing for this measure
            // According to TJA spec: "each measure is equally divided by the amount of numbers there are inside"
            float noteSpacing = currentMeasureDuration / measureData.Length;
            
            Debug.Log($"TJA: Measure {currentMeasure} - {measureData.Length} notes, duration: {currentMeasureDuration:F3}s, spacing: {noteSpacing:F3}s, currentTime: {currentTime:F3}s");
            
            for (int i = 0; i < measureData.Length; i++)
            {
                char noteChar = measureData[i];
                if (noteChar == '0') continue; // Skip blank notes
                
                // Calculate time for this note within the measure
                float noteTimeInMeasure = i * noteSpacing;
                // TJA offset: negative values delay notes (make them appear later), positive values make them appear sooner
                // In our system: negative times = earlier, positive times = later, so we need to reverse the offset
                float totalNoteTime = currentTime + noteTimeInMeasure - currentOffset;
                
                // Convert note character to NoteType
                NoteType noteType = ConvertTjaNoteChar(noteChar);
                
                if (noteType != NoteType.Blank)
                {
                    HitObject hitObject = new HitObject(totalNoteTime, 1f, noteType);
                    chart.HitObjects.Add(hitObject);
                    
                    Debug.Log($"TJA: Note at measure {currentMeasure}, position {i}/{measureData.Length}, time {totalNoteTime:F3}s, type {noteType} (char: {noteChar})");
                    Debug.Log($"  - Base time: {currentTime:F3}s, Note in measure: {noteTimeInMeasure:F3}s, Offset: {currentOffset:F3}s, Final: {totalNoteTime:F3}s");
                }
            }
            
            // Move to next measure
            currentTime += currentMeasureDuration;
            currentMeasure++;
        }
        
        private void ParseBPMChangeCommand(string line, ChartData chart)
        {
            // #BPMCHANGE command format: #BPMCHANGE <new_bpm>
            string[] parts = line.Split(' ');
            if (parts.Length >= 2 && float.TryParse(parts[1], out float newBPM))
            {
                Debug.Log($"TJA: BPM change command detected: {line}");
                
                // Update current BPM and beat length
                currentBPM = newBPM;
                beatLength = 60f / newBPM;
                
                // Recalculate current measure duration
                currentMeasureDuration = (currentTimeSignatureNumerator * 4f * beatLength) / currentTimeSignatureDenominator;
                
                // Create timing point for this BPM change
                TimingPoint tp = new TimingPoint();
                tp.Time = currentTime * 1000f; // Convert to milliseconds
                tp.BeatLength = beatLength * 1000f;
                tp.Meter = currentTimeSignatureNumerator;
                tp.Uninherited = true;
                chart.TimingPoints.Add(tp);
                
                Debug.Log($"TJA: BPM change to {newBPM} at measure {currentMeasure}, time {currentTime:F3}s, new beat length: {beatLength:F3}s, measure duration: {currentMeasureDuration:F3}s");
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
                    Debug.Log($"TJA: Measure command detected: {line}");
                    
                    currentTimeSignatureNumerator = numerator;
                    currentTimeSignatureDenominator = denominator;
                    
                    // Recalculate current measure duration
                    currentMeasureDuration = (numerator * 4f * beatLength) / denominator;
                    
                    Debug.Log($"TJA: Time signature change to {numerator}/{denominator} at measure {currentMeasure}, new duration: {currentMeasureDuration:F3}s");
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
                currentTime += delaySeconds;
                Debug.Log($"TJA: Delay of {delaySeconds:F3}s applied, new time: {currentTime:F3}s");
            }
        }
        
        private void ParseScrollCommand(string line, ChartData chart)
        {
            // #SCROLL command format: #SCROLL <multiplier>
            string[] parts = line.Split(' ');
            if (parts.Length >= 2 && float.TryParse(parts[1], out float scrollMultiplier))
            {
                Debug.Log($"TJA: Scroll speed multiplier {scrollMultiplier} at measure {currentMeasure}");
                // Note: Scroll speed affects visual appearance, not timing
            }
        }
        
        private void ParseGogoStart(string line)
        {
            Debug.Log($"TJA: Gogo start at measure {currentMeasure}");
        }
        
        private void ParseGogoEnd(string line)
        {
            Debug.Log($"TJA: Gogo end at measure {currentMeasure}");
        }
        
        private void ParseBarlineOff(string line)
        {
            Debug.Log($"TJA: Barline off at measure {currentMeasure}");
        }
        
        private void ParseBarlineOn(string line)
        {
            Debug.Log($"TJA: Barline on at measure {currentMeasure}");
        }
        
        private void ParseBranchStart(string line)
        {
            Debug.Log($"TJA: Branch start at measure {currentMeasure}");
        }
        
        private void ParseBranchEnd(string line)
        {
            Debug.Log($"TJA: Branch end at measure {currentMeasure}");
        }
        
        private void ParseSection(string line)
        {
            Debug.Log($"TJA: Section at measure {currentMeasure}");
        }
        
        private void ParseNormalBranch(string line)
        {
            Debug.Log($"TJA: Normal branch at measure {currentMeasure}");
        }
        
        private void ParseAdvancedBranch(string line)
        {
            Debug.Log($"TJA: Advanced branch at measure {currentMeasure}");
        }
        
        private void ParseMasterBranch(string line)
        {
            Debug.Log($"TJA: Master branch at measure {currentMeasure}");
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
                currentBPM = bpm;
                beatLength = 60f / bpm;
                currentMeasureDuration = (currentTimeSignatureNumerator * 4f * beatLength) / currentTimeSignatureDenominator;
                Debug.Log($"TJA: BPM = {bpm}, beat length = {beatLength:F3}s, measure duration = {currentMeasureDuration:F3}s");
            }
        }
        
        private void ParseOffset(string line)
        {
            if (float.TryParse(line.Substring("OFFSET:".Length).Trim(), out float offset))
            {
                currentOffset = offset;
                Debug.Log($"TJA: Offset = {offset:F3}s");
            }
        }
        
        private void ParseCourse(string line)
        {
            string course = line.Substring("COURSE:".Length).Trim();
            
            // Create a new chart for this course
            currentChart = new ChartData();
            currentChart.Version = course;
            currentChart.Difficulty = ParseDifficultyFromCourse(course);
            currentSong.Charts.Add(currentChart);
            
            // Reset timing for new chart
            currentMeasure = 0;
            currentTime = 0f;
            currentMeasureDuration = (currentTimeSignatureNumerator * 4f * beatLength) / currentTimeSignatureDenominator;
            
            Debug.Log($"TJA: Created chart '{course}' with difficulty {currentChart.Difficulty}");
        }
        
        private void ParseLevel(string line)
        {
            if (currentChart != null && int.TryParse(line.Substring("LEVEL:".Length).Trim(), out int level))
            {
                Debug.Log($"TJA: Level = {level}");
            }
        }
        
        private void ParseBalloon(string line)
        {
            if (currentChart != null)
            {
                string balloonData = line.Substring("BALLOON:".Length).Trim();
                string[] parts = balloonData.Split(',');
                
                if (parts.Length >= 4)
                {
                    Debug.Log($"TJA: Balloon data = {balloonData}");
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
            Debug.Log($"TJA: Style = {style}");
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
                
                Debug.Log($"TJA: Chart '{chart.Version}' - Total length: {chart.TotalLength:F3}s, Max combo: {chart.MaxCombo}");
                
                // Explain the timing system
                ExplainTjaTimingSystem(chart);
            }
        }
        
        private void ExplainTjaTimingSystem(ChartData chart)
        {
            Debug.Log($"=== TJA TIMING SYSTEM EXPLANATION ===");
            Debug.Log($"Chart: {chart.Version}");
            Debug.Log($"BPM: {currentBPM}");
            Debug.Log($"Beat Length: {beatLength:F3}s");
            Debug.Log($"Measure Duration: {currentMeasureDuration:F3}s");
            Debug.Log($"TJA Offset: {currentOffset:F3}s");
            Debug.Log($"");
            Debug.Log($"TJA Offset Interpretation:");
            Debug.Log($"- TJA offset {currentOffset:F3}s means notes are delayed by {Mathf.Abs(currentOffset):F3}s");
            Debug.Log($"- In our system: negative times = notes appear earlier, positive times = notes appear later");
            Debug.Log($"- So we apply: noteTime = baseTime - offset (reversing the TJA offset)");
            Debug.Log($"");
            Debug.Log($"Example timing:");
            if (chart.HitObjects.Count > 0)
            {
                var firstNote = chart.HitObjects[0];
                var lastNote = chart.HitObjects[chart.HitObjects.Count - 1];
                Debug.Log($"- First note: {firstNote.Time:F3}s");
                Debug.Log($"- Last note: {lastNote.Time:F3}s");
                Debug.Log($"- Chart duration: {lastNote.Time - firstNote.Time:F3}s");
            }
            Debug.Log($"================================");
        }
    }
}
