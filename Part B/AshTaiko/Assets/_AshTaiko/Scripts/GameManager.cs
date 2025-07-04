using System;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace AshTaiko
{
    // if theres any bit of code in this right now that needs a refactor, it's gotta be this one
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [Header("Note Prefabs")]
        public GameObject notePrefab;
        public GameObject drumrollBridgePrefab;

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

        // --- Drumroll State Management ---
        private bool _isInDrumroll = false;
        private HitObject _currentDrumroll = null;
        private float _drumrollStartTime = 0f;
        private float _drumrollEndTime = 0f; // Now set by DrumrollEnd note
        private int _drumrollHits = 0;
        private float _drumrollHitWindow = 0.15f; // Window for drumroll hits

        // Track active drumroll bridges
        private List<DrumrollBridge> _activeDrumrollBridges = new List<DrumrollBridge>();

        public event UnityAction<int> OnScoreChange;
        public event UnityAction<int> OnComboChange;
        public event UnityAction<float> OnAccuracyChange;

        public event UnityAction<Note> OnNoteHit;

        private int _score = 0;
        private int _combo = 0;

        private float _gauge;
        private float _maxGauge = 100;

        // --- Judgement Timing Constants (in seconds) ---
        private const float MISS_WINDOW = 0.125f;  // 125ms
        private const float OKAY_WINDOW = 0.108f;  // 108ms
        private const float GOOD_WINDOW = 0.042f;  // 42ms


        [SerializeField]
        private GameObject debugDrumrollIndicator;

        [SerializeField]
        private TextMeshProUGUI debugJudgementIndicator;

        private int hitGoods;
        private int hitOkays;
        private int hitBads;

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

        public Transform GetJudgementCircle()
        {
            return _judgementCircle;
        }

        private void Update()
        {
            debugDrumrollIndicator.SetActive(_isInDrumroll);
            // At the start of the frame, store the last and current dspTime
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

                // Skip DrumrollBalloonEnd notes - they don't need to be judged
                if (note.noteType == NoteType.DrumrollBalloonEnd)
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
                    //Destroy(note.gameObject); // Remove the note from the scene
                    // TODO: Handle miss feedback (e.g., break combo)
                    _nextJudgableNoteIndex++;
                }
                else
                {
                    // next note isn't missable, ignore for now
                    break;
                }
            }

            // --- Drumroll End Detection ---
            if (_isInDrumroll)
            {
                float currentTime = GetSmoothedSongTime();
                if (currentTime > _drumrollEndTime)
                {
                    EndDrumroll();
                }
            }
        }

        private void SpawnNote(HitObject noteData)
        {
            GameObject prefabToSpawn = notePrefab;
            var obj = Instantiate(prefabToSpawn);
            var note = obj.GetComponent<Note>();
            note.Initialize(noteData.Time, preemptTime, travelDistance, noteData.ScrollSpeed, noteData);
            note.judgementCirclePosition = _judgementCircle.position;
            _activeNotes.Add(note);

            // --- Drumroll bridge logic ---
            if (noteData.Type == NoteType.Drumroll || noteData.Type == NoteType.DrumrollBig)
            {
                // Spawn bridge and assign head
                var bridgeObj = Instantiate(drumrollBridgePrefab);
                var bridge = bridgeObj.GetComponent<DrumrollBridge>();
                bridge.headNote = note;
                _activeDrumrollBridges.Add(bridge);
            }
            else if (noteData.Type == NoteType.DrumrollBalloonEnd)
            {
                // Find the closest unlinked bridge and assign end
                DrumrollBridge closestBridge = null;
                float minTimeDiff = float.MaxValue;
                foreach (var bridge in _activeDrumrollBridges)
                {
                    if (bridge.endNote == null && bridge.headNote != null)
                    {
                        float diff = Mathf.Abs(note.hitTime - bridge.headNote.hitTime);
                        if (diff < minTimeDiff)
                        {
                            minTimeDiff = diff;
                            closestBridge = bridge;
                        }
                    }
                }
                if (closestBridge != null)
                {
                    closestBridge.headNote.drumrollEndNote = note;
                    closestBridge.endNote = note;
                }
            }
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
            float hitWindow = MISS_WINDOW; // Use miss window as the maximum hit window

            // --- Handle Drumroll Hits (if in drumroll mode) ---
            if (_isInDrumroll)
            {
                HandleDrumrollHit(hitType, currentTime);
                // Don't return here - continue to process regular notes
            }

            // Only check the next judgable note
            while (_nextJudgableNoteIndex < _activeNotes.Count)
            {
                Note note = _activeNotes[_nextJudgableNoteIndex];
                if (note.isHit)
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }

                // Skip DrumrollBalloonEnd notes - they don't need to be judged by input
                if (note.noteType == NoteType.DrumrollBalloonEnd)
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
                        // Determine judgement based on timing
                        Judgement judgement = GetJudgement(Mathf.Abs(currentTime - note.hitTime));
                        Debug.Log($"Hit! Judgement: {judgement}");

                        note.isHit = true;

                        // --- Check if this starts a drumroll ---
                        if (note.hitObject.Type == NoteType.Drumroll)
                        {
                            StartDrumroll(note.hitObject, currentTime);
                        }

                        // --- Apply judgement effects ---
                        ApplyJudgementEffects(judgement, note);

                        // --- Special handling for drumroll head notes ---
                        if (note.noteType == NoteType.Drumroll || note.noteType == NoteType.DrumrollBig)
                        {
                            // Don't destroy drumroll head notes - they stay until the end note is hit
                            _nextJudgableNoteIndex++;
                        }
                        else if (note.noteType == NoteType.DrumrollBalloonEnd)
                        {
                            // Destroy the drumroll head note and bridge when the end note is hit
                            DestroyDrumrollHeadAndBridge(note);
                            Destroy(note.gameObject);
                            _nextJudgableNoteIndex++;
                        }
                        else
                        {
                            // Destroy normal notes
                            Destroy(note.gameObject);
                            _nextJudgableNoteIndex++;
                        }
                    }
                    // if input type dont match
                    // dont advance index
                    break;
                }
                else
                {
                    // If the note is too late, mark as missed and move to next
                    note.isHit = true;
                    // --- Miss Effect ---
                    ApplyJudgementEffects(Judgement.Miss, note);

                    // --- Special handling for drumroll head notes ---
                    if (note.noteType == NoteType.Drumroll || note.noteType == NoteType.DrumrollBig)
                    {
                        // Don't destroy drumroll head notes - they stay until the end note is hit
                        _nextJudgableNoteIndex++;
                    }
                    else if (note.noteType == NoteType.DrumrollBalloonEnd)
                    {
                        // Destroy the drumroll head note and bridge when the end note is missed
                        DestroyDrumrollHeadAndBridge(note);
                        Destroy(note.gameObject);
                        _nextJudgableNoteIndex++;
                    }
                    else
                    {
                        // Destroy normal notes
                        Destroy(note.gameObject);
                        _nextJudgableNoteIndex++;
                    }
                }
            }
        }

        // --- Drumroll Methods ---
        private void StartDrumroll(HitObject drumrollStart, float currentTime)
        {
            _isInDrumroll = true;
            _currentDrumroll = drumrollStart;
            _drumrollStartTime = currentTime;
            _drumrollHits = 0;
            _drumrollEndTime = -1f;
            // Find the next DrumrollBalloonEnd note in _notes
            for (int i = _nextJudgableNoteIndex + 1; i < _notes.Count; i++)
            {
                if (_notes[i].Type == NoteType.DrumrollBalloonEnd)
                {
                    _drumrollEndTime = _notes[i].Time;
                    break;
                }
            }
            if (_drumrollEndTime < 0f)
            {
                Debug.LogWarning("No DrumrollBalloonEnd found after drumroll start!");
                // Fallback: set a default duration (e.g., 1s)
                _drumrollEndTime = _drumrollStartTime + 1f;
            }
            Debug.Log($"Drumroll started! Ends at: {_drumrollEndTime}");
        }

        private void HandleDrumrollHit(HitType hitType, float currentTime)
        {
            // Check if drumroll has ended
            if (currentTime > _drumrollEndTime)
            {
                EndDrumroll();
                return;
            }
            // Count the hit if it's the correct type
            if (IsHitTypeMatchingNoteType(hitType, _currentDrumroll.Type))
            {
                _drumrollHits++;
                _combo++;
                OnComboChange?.Invoke(_combo);
                // Award points for each drumroll hit (smaller than regular hits)
                _score += 10; // Adjust scoring as needed
                OnScoreChange?.Invoke(_score);
                PlayHitEffect(_judgementCircle.position);
                Debug.Log("Drumroll hit! Total hits: " + _drumrollHits);
            }
        }

        private void EndDrumroll()
        {
            Debug.Log("Drumroll ended! Total hits: " + _drumrollHits);
            // Award bonus points based on drumroll performance
            int bonusPoints = _drumrollHits * 5; // Adjust bonus calculation
            _score += bonusPoints;
            OnScoreChange?.Invoke(_score);
            // Reset drumroll state
            _isInDrumroll = false;
            _currentDrumroll = null;
            _drumrollStartTime = 0f;
            _drumrollEndTime = 0f;
            _drumrollHits = 0;
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
                case NoteType.Drumroll:
                case NoteType.DrumrollBig:
                    // During drumrolls, both Don and Ka inputs are valid
                    return hitType == HitType.Don || hitType == HitType.Ka;
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
            return _songManager.GetSongProgress() / _songManager.GetSongLength();
        }

        private void DestroyDrumrollHeadAndBridge(Note endNote)
        {
            // Find the bridge that connects to this end note
            DrumrollBridge bridgeToRemove = null;
            foreach (var bridge in _activeDrumrollBridges)
            {
                if (bridge.endNote == endNote)
                {
                    // Destroy the head note
                    if (bridge.headNote != null)
                    {
                        Destroy(bridge.headNote.gameObject);
                    }
                    // Destroy the bridge
                    Destroy(bridge.gameObject);
                    bridgeToRemove = bridge;
                    break;
                }
            }
            if (bridgeToRemove != null)
            {
                _activeDrumrollBridges.Remove(bridgeToRemove);
            }
        }

        // --- Judgement Methods ---
        private Judgement GetJudgement(float timeDifference)
        {
            if (timeDifference <= GOOD_WINDOW)
                return Judgement.Good;
            else if (timeDifference <= OKAY_WINDOW)
                return Judgement.Okay;
            else if (timeDifference <= MISS_WINDOW)
                return Judgement.Miss;
            else
                return Judgement.Miss; // Fallback
        }

        private void ApplyJudgementEffects(Judgement judgement, Note note)
        {
            switch (judgement)
            {
                case Judgement.Good:
                    hitGoods++;
                    debugJudgementIndicator.text = "Good!";
                    _combo++;
                    _score += 100; // Base score for Good
                    OnComboChange?.Invoke(_combo);
                    OnNoteHit?.Invoke(note);
                    PlayHitEffect(note.transform.position);
                    Debug.Log("Good!");
                    AddGauge(0.25f);
                    break;

                case Judgement.Okay:
                    debugJudgementIndicator.text = "Okay!";
                    hitOkays++;
                    _combo++;
                    _score += 50; // Lower score for Okay
                    OnComboChange?.Invoke(_combo);
                    OnNoteHit?.Invoke(note);
                    PlayHitEffect(note.transform.position);
                    Debug.Log("Okay!");
                    AddGauge(0.125f);
                    break;

                case Judgement.Miss:
                    hitBads++;
                    debugJudgementIndicator.text = "Bad";
                    _combo = 0;
                    OnComboChange?.Invoke(_combo);
                    PlayMissEffect(note.transform.position);
                    AddGauge(-0.25f);
                    Debug.Log("Miss!");
                    break;
            }

            OnScoreChange?.Invoke(_score);
            OnAccuracyChange?.Invoke(GetAccuracy());
        }

        public enum Judgement
        {
            Miss,
            Okay,
            Good
        }

        private void AddGauge(float amount)
        {
            _gauge += amount;
            _gauge = Mathf.Clamp(_gauge, 0, _maxGauge);
        }

        private float GetAccuracy()
        {
            // uses the osu taiko formula
            float nominator = hitGoods + hitOkays * 0.5f;
            float sum = hitGoods + hitOkays + hitBads;
            return nominator / sum * 100;
        }

        public string GetSongTimestamp()
        {
            return _songManager.GetSongPositionString() + " / " + _songManager.GetSongLengthString();
        }

        public float GetGaugePercentage()
        {
            return _gauge / _maxGauge;
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
        // Drumrolls are now defined by start and end notes, so no Duration here
    }

}
