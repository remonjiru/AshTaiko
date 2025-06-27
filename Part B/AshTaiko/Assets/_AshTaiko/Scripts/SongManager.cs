using System;
using UnityEngine;

namespace AshTaiko
{
    public class SongManager : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        private double _songTime;
        private double _startDspTime;

        public void PlayInSeconds(double seconds)
        {
            _audioSource.PlayScheduled(AudioSettings.dspTime + seconds);

            _startDspTime = AudioSettings.dspTime + seconds;
            _songTime = AudioSettings.dspTime - _startDspTime;
        }

        public float GetSongLength()
        {
            return _audioSource.clip.length;
        }
        public float GetSongProgress()
        {
            return _audioSource.time;
        }

        public string GetSongLengthString()
        {
            var ts = TimeSpan.FromSeconds(_audioSource.clip.length);
            return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }


        public string GetSongPositionString()
        {
            var ts = TimeSpan.FromSeconds(_audioSource.time);
            return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }
    }
}
