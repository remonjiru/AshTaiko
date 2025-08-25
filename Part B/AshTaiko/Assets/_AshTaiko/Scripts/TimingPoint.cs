using System;

namespace AshTaiko
{
    [Serializable]
    public class TimingPoint
    {
        public float Time; // Time in milliseconds
        public float BeatLength; // Beat length in milliseconds
        public int Meter; // Time signature numerator
        public int SampleSet;
        public int SampleIndex;
        public int Volume;
        public bool Uninherited; // Whether this timing point is inherited
        public int Effects;
        
        public float BPM => BeatLength > 0 ? 60000f / BeatLength : 0f;
    }
}

