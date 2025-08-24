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
        
        public void PlayImmediately()
        {
            _audioSource.Play();
            _startDspTime = AudioSettings.dspTime;
            _songTime = 0;
        }

        public float GetSongLength()
        {
            if (_audioSource.clip == null)
                return 0f;
            return _audioSource.clip.length;
        }
        
        public float GetSongProgress()
        {
            if (_audioSource.clip == null)
                return 0f;
            return _audioSource.time;
        }

        public string GetSongLengthString()
        {
            if (_audioSource.clip == null)
                return "00:00";
            
            var ts = TimeSpan.FromSeconds(_audioSource.clip.length);
            return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }

        public string GetSongPositionString()
        {
            if (_audioSource.clip == null)
                return "00:00";
            
            var ts = TimeSpan.FromSeconds(_audioSource.time);
            return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }
        
        public void SetAudioClip(AudioClip clip)
        {
            _audioSource.clip = clip;
        }
        
        public AudioClip GetCurrentAudioClip()
        {
            return _audioSource.clip;
        }
        
        public bool IsPlaying()
        {
            return _audioSource.isPlaying;
        }
        
        public float GetCurrentSongTime()
        {
            if (_audioSource.clip == null)
                return 0f;
            return _audioSource.time;
        }
        
        public bool IsAudioReady()
        {
            return _audioSource != null && _audioSource.clip != null;
        }
    }
}
