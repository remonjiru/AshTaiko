using System;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Manages audio playback and timing synchronization for songs.
    /// Provides high-precision timing information and controls for audio playback,
    /// including pause/resume functionality and scheduled playback.
    /// </summary>
    public class SongManager : MonoBehaviour
    {
        [SerializeField]
        private AudioSource _audioSource;

        /// <summary>
        /// Current playback time in seconds, accounting for pauses.
        /// </summary>
        private double _songTime;
        
        /// <summary>
        /// DSP time when playback started.
        /// </summary>
        private double _startDspTime;
        
        /// <summary>
        /// DSP time when pause started.
        /// </summary>
        private double _pauseStartDspTime;
        
        /// <summary>
        /// Total time spent paused during playback.
        /// </summary>
        private double _totalPausedTime = 0.0;
        
        /// <summary>
        /// Whether playback is currently paused.
        /// </summary>
        private bool _isPaused = false;

        /// <summary>
        /// Schedules audio playback to start after a specified delay.
        /// </summary>
        /// <param name="seconds">Delay in seconds before playback starts.</param>
        public void PlayInSeconds(double seconds)
        {
            if (_audioSource != null && _audioSource.clip != null)
            {
                double scheduledTime = AudioSettings.dspTime + seconds;
                _audioSource.PlayScheduled(scheduledTime);
                
                _startDspTime = scheduledTime;
                _songTime = AudioSettings.dspTime - _startDspTime;
                
                Debug.Log($"SongManager: Audio scheduled to play in {seconds:F1}s");
            }
            else
            {
                Debug.LogError("SongManager: Cannot play audio - AudioSource or AudioClip is null!");
            }
            
            // Reset pause tracking when starting playback
            _totalPausedTime = 0.0;
            _isPaused = false;
        }
        
        /// <summary>
        /// Starts audio playback immediately.
        /// </summary>
        public void PlayImmediately()
        {
            _audioSource.Play();
            _startDspTime = AudioSettings.dspTime;
            _songTime = 0;
            // Reset pause tracking when starting playback
            _totalPausedTime = 0.0;
            _isPaused = false;
        }

        /// <summary>
        /// Gets the total length of the current audio clip in seconds.
        /// </summary>
        /// <returns>Length in seconds, or 0 if no clip is loaded.</returns>
        public float GetSongLength()
        {
            if (_audioSource.clip == null)
                return 0f;
            return _audioSource.clip.length;
        }
        
        /// <summary>
        /// Gets the current playback position in seconds.
        /// </summary>
        /// <returns>Current position in seconds, or 0 if no clip is loaded.</returns>
        public float GetSongProgress()
        {
            if (_audioSource.clip == null)
                return 0f;
            return _audioSource.time;
        }

        /// <summary>
        /// Gets the total song length formatted as MM:SS string.
        /// </summary>
        /// <returns>Formatted time string, or "00:00" if no clip is loaded.</returns>
        public string GetSongLengthString()
        {
            if (_audioSource.clip == null)
                return "00:00";
            
            var ts = TimeSpan.FromSeconds(_audioSource.clip.length);
            return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }

        /// <summary>
        /// Gets the current playback position formatted as MM:SS string.
        /// </summary>
        /// <returns>Formatted time string, or "00:00" if no clip is loaded.</returns>
        public string GetSongPositionString()
        {
            if (_audioSource.clip == null)
                return "00:00";
            
            var ts = TimeSpan.FromSeconds(_audioSource.time);
            return string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);
        }
        
        /// <summary>
        /// Sets the audio clip for playback.
        /// </summary>
        /// <param name="clip">The AudioClip to set.</param>
        public void SetAudioClip(AudioClip clip)
        {
            if (_audioSource != null)
            {
                _audioSource.clip = clip;
                Debug.Log($"SongManager: AudioClip set successfully: {clip?.name ?? "null"}");
            }
            else
            {
                Debug.LogError("SongManager: Cannot set AudioClip - AudioSource is null!");
            }
            
            // Reset pause tracking when loading new audio
            _totalPausedTime = 0.0;
            _isPaused = false;
        }
        
        /// <summary>
        /// Gets the currently loaded audio clip.
        /// </summary>
        /// <returns>The current AudioClip, or null if none is loaded.</returns>
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

        /// <summary>
        /// Gets the current song time accounting for paused time.
        /// </summary>
        /// <returns>The corrected song time in seconds.</returns>
        public double GetCorrectedSongTime()
        {
            if (_audioSource == null || !_audioSource.isPlaying)
            {
                return _songTime;
            }
            
            // Calculate current time accounting for total paused time
            double currentDspTime = AudioSettings.dspTime;
            double correctedTime = currentDspTime - _startDspTime - _totalPausedTime;
            
            return correctedTime;
        }

        /// <summary>
        /// Gets the total time the audio has been paused.
        /// </summary>
        /// <returns>Total paused time in seconds.</returns>
        public double GetTotalPausedTime()
        {
            return _totalPausedTime;
        }

        /// <summary>
        /// Pauses the audio playback.
        /// </summary>
        public void PauseAudio()
        {
            if (_audioSource != null && _audioSource.isPlaying && !_isPaused)
            {
                _isPaused = true;
                _pauseStartDspTime = AudioSettings.dspTime;
                _audioSource.Pause();
            }
        }

        /// <summary>
        /// Resumes the audio playback from where it was paused.
        /// </summary>
        public void ResumeAudio()
        {
            if (_audioSource != null && !_audioSource.isPlaying && _isPaused)
            {
                _isPaused = false;
                
                // Calculate how long we were paused
                double currentDspTime = AudioSettings.dspTime;
                double pauseDuration = currentDspTime - _pauseStartDspTime;
                _totalPausedTime += pauseDuration;
                
                // Resume the audio
                _audioSource.UnPause();
            }
        }

        /// <summary>
        /// Stops the audio playback completely and resets to beginning.
        /// </summary>
        public void StopAudio()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.time = 0f;
                
                // Reset pause tracking
                _isPaused = false;
                _totalPausedTime = 0.0;
                _pauseStartDspTime = 0.0;
                
                // Reset song time
                _songTime = 0.0;
            }
        }

        /// <summary>
        /// Resets pause tracking to initial state.
        /// Useful when restarting songs or loading new audio.
        /// </summary>
        public void ResetPauseTracking()
        {
            _isPaused = false;
            _totalPausedTime = 0.0;
            _pauseStartDspTime = 0.0;
        }
    }
}
