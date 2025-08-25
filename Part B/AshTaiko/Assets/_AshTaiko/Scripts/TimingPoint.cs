using System;

namespace AshTaiko
{
    /// <summary>
    /// Represents a timing point that defines BPM changes, time signatures, and audio effects
    /// at specific moments in a chart. Timing points are essential for maintaining rhythm
    /// synchronization between the chart data and audio playback.
    /// </summary>
    [Serializable]
    public class TimingPoint
    {
        /// <summary>
        /// Time in seconds when this timing point takes effect.
        /// </summary>
        public float Time;
        
        /// <summary>
        /// Beat length in milliseconds, used to calculate BPM.
        /// </summary>
        public float BeatLength;
        
        /// <summary>
        /// Time signature numerator (e.g., 4 for 4/4 time).
        /// </summary>
        public int Meter;
        
        /// <summary>
        /// Sample set identifier for hit sound effects.
        /// </summary>
        public int SampleSet;
        
        /// <summary>
        /// Sample index for custom hit sound effects.
        /// </summary>
        public int SampleIndex;
        
        /// <summary>
        /// Volume level for hit sound effects (0-100).
        /// </summary>
        public int Volume;
        
        /// <summary>
        /// Whether this timing point is inherited from the previous one.
        /// </summary>
        public bool Uninherited;
        
        /// <summary>
        /// Bit flags for various audio effects and modifiers.
        /// </summary>
        public int Effects;
        
        /// <summary>
        /// Calculated BPM based on the beat length.
        /// </summary>
        public float BPM => BeatLength > 0 ? 60000f / BeatLength : 0f;
    }
}

