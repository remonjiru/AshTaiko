using System;
using System.Collections.Generic;

namespace AshTaiko
{
    [Serializable]
    public class ChartData
    {
        public string Version; // Difficulty name (e.g., "Hard", "Insane")
        public Difficulty Difficulty;
        public float HP;
        public float CircleSize;
        public float OverallDifficulty;
        public float ApproachRate;
        public float SliderMultiplier;
        public List<TimingPoint> TimingPoints = new List<TimingPoint>();
        public List<HitObject> HitObjects = new List<HitObject>();
        public float TotalLength;
        public int MaxCombo;
        public float StarRating;
        
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
