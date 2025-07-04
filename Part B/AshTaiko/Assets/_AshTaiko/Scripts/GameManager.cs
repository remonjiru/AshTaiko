using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace AshTaiko
{
    // if theres any bit of code in this right now that needs a refactor, it's gotta be this one
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        public GameObject notePrefab;

        /// <summary>
        /// Reference to the Song Manager instance.
        /// </summary>
        [SerializeField]
        private SongManager _songManager;

        [SerializeField]
        private Drum _drum;

        [SerializeField]
        private Transform _judgementCircle;

        [SerializeField]
        private GameObject _hitEffect;

        [SerializeField]
        private List<HitObject> _notes = new List<HitObject>();
        private int _nextNoteIndex = 0;
        float preemptTime = 2.0f; // time (in seconds) before hitTime to spawn

        [SerializeField]
        private float travelDistance = 15f; // distance from spawn to hit bar

        private float _songTime;

        public float SongTime
        {
            get
            {
                return _songTime;
            }
        }

        public float _smoothedDsp;

        private double _lastDsp;

        public float songStartTime = 0;
        public float songEndTime = 0;

        private double _lastDspTime;
        private double _currentDspTime;
        private float _frameStartTime;

        private List<Note> _activeNotes = new List<Note>();

        private int _nextJudgableNoteIndex = 0; // next note to be judged

        public event UnityAction<int> OnScoreChange;
        public event UnityAction<int> OnComboChange;
        public event UnityAction<Note> OnNoteHit;

        private int _score = 0;
        private int _combo = 0;


        private void Awake()
        {
            Instance = this;

        }

        private void Start()
        {

            // AudioConfiguration config = AudioSettings.GetConfiguration();
            // config.dspBufferSize = 256;
            // AudioSettings.Reset(config);

            songStartTime = (float)AudioSettings.dspTime;
            StartGame();
            _drum.OnHit += RegisterHit;
        }

        private void OnDisable()
        {
            _drum.OnHit -= RegisterHit;
        }

        private void Update()
        {
            // At the start of the frame store the last and current dspTime
            _lastDspTime = _currentDspTime;
            _currentDspTime = AudioSettings.dspTime;
            _frameStartTime = Time.time;

            if (AudioSettings.dspTime != _lastDsp)
            {
                _smoothedDsp = (float)AudioSettings.dspTime;
            }
            else
            {
                _smoothedDsp += Time.unscaledDeltaTime;
            }
            _lastDsp = AudioSettings.dspTime;
            _smoothedDsp = (float)AudioSettings.dspTime;

            _songTime = _smoothedDsp - songStartTime;

            while (_nextNoteIndex < _notes.Count && _notes[_nextNoteIndex].Time - _songTime <= preemptTime)
            {
                SpawnNote(_notes[_nextNoteIndex]);
                _nextNoteIndex++;
            }

            // [-- MISS HANDLING --]
            // If the next judgable note has passed its hit window and wasn't hit mark as missed
            while (_nextJudgableNoteIndex < _activeNotes.Count)
            {
                Note note = _activeNotes[_nextJudgableNoteIndex];
                if (note.isHit) // Already judged
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }
                float currentTime = GetSmoothedSongTime();
                float missWindow = 0.15f;
                if (currentTime > note.hitTime + missWindow)
                {
                    note.isHit = true;
                    // [-- Miss Effect --]
                    // TODO: PLEASE PUT THIS INTO A DIFFERENT FUNCTION I DONT KNOW WHY IVE PUT TWO HERE
                    _combo = 0;
                    OnComboChange?.Invoke(_combo);
                    PlayMissEffect(note.transform.position); // Play miss visual/sound effect
                    Destroy(note.gameObject); // boom
                    _nextJudgableNoteIndex++;
                }
                else
                {
                    // next note isn't missable, ignore for now
                    break;
                }
            }
        }

        private void SpawnNote(HitObject noteData)
        {
            var obj = Instantiate(notePrefab);
            var note = obj.GetComponent<Note>();
            note.Initialize(noteData.Time, preemptTime, travelDistance, noteData.ScrollSpeed, noteData);
            note.judgementCirclePosition = _judgementCircle.position;
            _activeNotes.Add(note);
        }

        private void StartGame()
        {
            _score = 0;
            _combo = 0;
            OnScoreChange?.Invoke(_score);
            OnComboChange?.Invoke(_combo);
            _songManager.PlayInSeconds(3);
        }

        public float GetSmoothedSongTime()
        {
            // Check how long into current frame
            float t = Mathf.Clamp01((Time.time - _frameStartTime) / Time.deltaTime);

            // Interpolate between last and current dspTime
            double interpolatedDsp = Mathf.Lerp((float)_lastDspTime, (float)_currentDspTime, t);

            // Return song time relative to start
            return (float)(interpolatedDsp - songStartTime);
        }

        private void RegisterHit(HitType hitType)
        {
            float currentTime = GetSmoothedSongTime();
            float hitWindow = 0.15f; // Example hit window (tweak as needed)

            // Only check the next judgable note
            while (_nextJudgableNoteIndex < _activeNotes.Count)
            {
                Note note = _activeNotes[_nextJudgableNoteIndex];
                if (note.isHit)
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }
                // If the note is too far in the future, ignore input
                if (currentTime < note.hitTime - hitWindow)
                {
                    break;
                }

                // If the note is within the hit window
                if (Mathf.Abs(currentTime - note.hitTime) <= hitWindow)
                {
                    // Only allow correct input type to hit the note
                    if (IsHitTypeMatchingNoteType(hitType, note.hitObject.Type))
                    {
                        Debug.Log("Hit!");
                        note.isHit = true;
                        // --- Hit Effect ---
                        _combo++;
                        OnComboChange?.Invoke(_combo);
                        OnNoteHit?.Invoke(note);
                        PlayHitEffect(note.transform.position); // Play visual/sound effect
                        Destroy(note.gameObject); // Remove the note from the scene
                        // TODO: Add scoring, combo, etc.
                        _nextJudgableNoteIndex++;
                    }
                    // if input type dont match
                    // dont advance index
                    break;
                }
                else
                {
                    // If the note is too late, mark as missed and move to next
                    note.isHit = true;
                    // [ -- Miss Effect -- ]
                    _combo = 0;
                    OnComboChange?.Invoke(_combo);
                    PlayMissEffect(note.transform.position); // Play miss visual/sound effect
                    Destroy(note.gameObject); // Remove the note from the scene
                    // TODO: Handle miss feedback (e.g., break combo)
                    _nextJudgableNoteIndex++;
                    // Continue to next note in case player input is for a later note
                }
            }
        }

        // Helper method to check if a HitType matches a NoteType
        private bool IsHitTypeMatchingNoteType(HitType hitType, NoteType noteType)
        {
            switch (noteType)
            {
                case NoteType.Don:
                case NoteType.DonBig:
                    return hitType == HitType.Don;
                case NoteType.Ka:
                case NoteType.KaBig:
                    return hitType == HitType.Ka;
                default:
                    return false;
            }
        }

        private void PlayHitEffect(Vector3 position)
        {
            // TODO: Instantiate hit effect prefab or play a sound at the given position
            Instantiate(_hitEffect, _judgementCircle.position, Quaternion.identity);
            Debug.Log("Play hit effect at: " + position);
        }

        private void PlayMissEffect(Vector3 position)
        {
            // TODO: Instantiate miss effect prefab or play a sound at the given position
            Debug.Log("Play miss effect at: " + position);
        }

        public float GetSongProgressPercentage()
        {
            return (_songManager.GetSongProgress() / _songManager.GetSongLength()) * 100;
        }
    }

    [Serializable]
    public class HitObject
    {
        public HitObject(float _time, float _scrollSpeed, NoteType _type)
        {
            Time = _time;
            ScrollSpeed = _scrollSpeed;
            Type = _type;
        }
        public float Time; // in seconds
        public float ScrollSpeed = 1f; // multiplier relative to base speed
        public NoteType Type;   // "don", "ka", "drumroll", etc. 
    }

}
