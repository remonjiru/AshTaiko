using System;
using System.Collections.Generic;

namespace AshTaiko
{
    /// <summary>
    /// Represents a single difficulty chart for a song, containing timing information,
    /// note data, and difficulty parameters. This class stores all the gameplay data
    /// needed to render and play a specific difficulty level of a song.
    /// </summary>
    [Serializable]
    public class ChartData
    {
        /// <summary>
        /// Difficulty name or version identifier (e.g., "Hard", "Insane", "Expert").
        /// </summary>
        public string Version;
        
        /// <summary>
        /// Enumeration representing the difficulty level for sorting and filtering.
        /// </summary>
        public Difficulty Difficulty;
        
        /// <summary>
        /// Health points/drain rate for this difficulty (osu! format).
        /// </summary>
        public float HP;
        
        /// <summary>
        /// Size of the hit circles for this difficulty (osu! format).
        /// </summary>
        public float CircleSize;
        
        /// <summary>
        /// Overall difficulty rating for this chart (osu! format).
        /// </summary>
        public float OverallDifficulty;
        
        /// <summary>
        /// Approach rate determining how early notes appear (osu! format).
        /// </summary>
        public float ApproachRate;
        
        /// <summary>
        /// Multiplier for slider movement speed (osu! format).
        /// </summary>
        public float SliderMultiplier;
        
        /// <summary>
        /// Collection of timing points that define BPM changes and beat divisions.
        /// </summary>
        public List<TimingPoint> TimingPoints = new List<TimingPoint>();
        
        /// <summary>
        /// Collection of hit objects (notes, drumrolls, etc.) for this chart.
        /// </summary>
        public List<HitObject> HitObjects = new List<HitObject>();
        
        /// <summary>
        /// Total length of the chart in seconds.
        /// </summary>
        public float TotalLength;
        
        /// <summary>
        /// Maximum possible combo achievable in this chart.
        /// </summary>
        public int MaxCombo;
        
        /// <summary>
        /// Star rating indicating the relative difficulty of this chart.
        /// </summary>
        public float StarRating;
        
        /// <summary>
        /// Initializes a new ChartData instance with default values.
        /// </summary>
        public ChartData()
        {
            TimingPoints = new List<TimingPoint>();
            HitObjects = new List<HitObject>();
            TotalLength = 0;
            MaxCombo = 0;
            StarRating = 0;
        }
    }
}
