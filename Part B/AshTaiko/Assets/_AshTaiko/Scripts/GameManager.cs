using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace AshTaiko
{
    // if theres any bit of code in this right now that needs a refactor, it's gotta be this one
    /// <summary>
    /// Class responsible for the game state, timing synchronisation, note spawning, hit detection, scoring, and gameplay loop.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        /*
        Singleton pattern is used here to ensure global access to the game state and methods from anywhere in the codebase.
        Only one instance exists per scene and is assigned in the Awake() method.
        Singleton pattern is a horrible violation of the Single Responsibility Principle
        Dependency injection would be better as a future improvement to avoid the coupling that singleton patterns cause
        */
        /// <summary>
        /// The instance of the GameManager in the scene.
        /// </summary>
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

        // Chart system integration
        private SongEntry _currentSong;
        private ChartData _currentChart;
        float preemptTime = 2.0f; // time (in seconds) before hitTime to spawn

        [SerializeField]
        private float travelDistance = 15f; // distance from spawn to hit bar

        private float _songTime;

        // Timing system state
        private enum TimingState
        {
            Uninitialized,    // No audio loaded yet
            Loading,          // Audio is loading
            Delay,            // Audio loaded, waiting for 3s delay
            Playing           // Audio is playing normally
        }

        private TimingState _timingState = TimingState.Uninitialized;
        private double _delayStartDspTime = 0.0; // When the 3s delay actually started

        // Flag to track if a song was manually selected (prevents auto-start from overriding)
        private bool _songManuallySelected = false;

        public float SongTime
        {
            get
            {
                return _songTime;
            }
        }

        public float GetSynchronizedSongTime()
        {
            // Don't try to get audio time if we're not in the right state
            if (_timingState == TimingState.Uninitialized || _timingState == TimingState.Loading)
            {
                return _songTime; // Return current song time until system is ready
            }

            // Get the actual audio playback time from SongManager
            if (_songManager != null)
            {
                float audioTime = _songManager.GetCurrentSongTime();

                if (_timingState == TimingState.Delay)
                {
                    // We're in the 3-second delay period
                    // Calculate time based on when the delay actually started
                    double currentDspTime = AudioSettings.dspTime;
                    double timeSinceDelayStart = currentDspTime - _delayStartDspTime;

                    // Return negative time that counts up: -3.0s -> -2.0s -> -1.0s -> 0.0s
                    float synchronizedTime = -3f + (float)timeSinceDelayStart;

                    // Debug timing during delay (less frequent to avoid spam)
                    if (Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"Delay timing: DSP={currentDspTime:F3}s, delayStart={_delayStartDspTime:F3}s, timeSinceDelay={timeSinceDelayStart:F3}s, syncTime={synchronizedTime:F3}s");
                    }

                    return synchronizedTime;
                }
                else if (_timingState == TimingState.Playing)
                {
                    // Audio is playing normally - use continuous timing from delay period
                    // Calculate time since the delay started and add the 3-second offset
                    double currentDspTime = AudioSettings.dspTime;
                    double timeSinceDelayStart = currentDspTime - _delayStartDspTime;

                    // This gives us continuous timing: -3.0s -> -2.0s -> -1.0s -> 0.0s -> 1.0s -> 2.0s...
                    float synchronizedTime = -3f + (float)timeSinceDelayStart;

                    // Debug timing during playing (less frequent to avoid spam)
                    if (Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"Playing timing: DSP={currentDspTime:F3}s, delayStart={_delayStartDspTime:F3}s, timeSinceDelay={timeSinceDelayStart:F3}s, syncTime={synchronizedTime:F3}s, audioTime={audioTime:F3}s");
                    }

                    return synchronizedTime;
                }
            }

            return _songTime;
        }

        // Method to get the current song time for UI display (always positive)
        public float GetDisplaySongTime()
        {
            float synchronizedTime = GetSynchronizedSongTime();
            // Convert negative time (during delay) to positive display time
            return Mathf.Max(0f, synchronizedTime);
        }

        // Debug method to show current timing status
        public void LogTimingStatus()
        {
            if (_currentChart != null)
            {
                Debug.Log($"=== TIMING STATUS ===");
                Debug.Log($"Chart: {_currentChart.Version}");
                Debug.Log($"Total notes: {_currentChart.HitObjects.Count}");
                Debug.Log($"Next note index: {_nextNoteIndex}");
                Debug.Log($"Current song time: {_songTime:F3}s");
                Debug.Log($"Audio source time: {_songManager?.GetCurrentSongTime():F3}s");

                if (_nextNoteIndex < _notes.Count)
                {
                    float nextNoteTime = _notes[_nextNoteIndex].Time;
                    float timeUntil = nextNoteTime - _songTime;
                    Debug.Log($"Next note at: {nextNoteTime}s, time until: {timeUntil:F3}s");
                }
                Debug.Log($"====================");
            }
            else
            {
                Debug.Log("No chart loaded!");
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

        // New event for song/chart changes
        public event UnityAction<SongEntry, ChartData> OnSongChanged;

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
            _drum.OnHit += RegisterHit;

            // Auto-start is now disabled by default - use SongSelectionEditor instead
            // StartCoroutine(AutoStartWithFirstChart());

            Debug.Log("🎵 GameManager initialized - use SongSelectionEditor to start games!");
            Debug.Log("📋 To start a game:");
            Debug.Log("   1. Go to: AshTaiko > Song Selection Editor");
            Debug.Log("   2. Select a song and difficulty");
            Debug.Log("   3. Click '🎮 Start Game'");
            Debug.Log("🔧 Context menu options available on GameManager for testing");
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

            // Use synchronized time from audio source for more accurate timing
            float synchronizedTime = GetSynchronizedSongTime();
            _songTime = synchronizedTime;

            // Check if we should transition from delay to playing state
            if (_timingState == TimingState.Delay && _songTime >= 0f)
            {
                _timingState = TimingState.Playing;
                Debug.Log($"🎵 TRANSITIONING TO PLAYING STATE at songTime={_songTime:F3}s");
                Debug.Log($"🎵 Delay start DSP: {_delayStartDspTime:F3}s, Current DSP: {AudioSettings.dspTime:F3}s");
                Debug.Log($"🎵 Time since delay start: {AudioSettings.dspTime - _delayStartDspTime:F3}s");
            }

            // Clean up destroyed notes from the active notes list periodically
            if (Time.frameCount % 15 == 0) // Every 15 frames instead of 30
            {
                CleanupDestroyedNotes();
            }

            // Debug timing every frame for troubleshooting
            if (_nextNoteIndex < _notes.Count && _songTime >= -2f) // Only log when close to audio start
            {
                Debug.Log($"Frame timing: songTime={_songTime:F3}s, audioTime={_songManager?.GetCurrentSongTime():F3}s, nextNote={_notes[_nextNoteIndex].Time}s, state={_timingState}");
            }

            // Additional timing debug when in playing state
            if (_timingState == TimingState.Playing && _songTime > 0f && _songTime < 10f) // First 10 seconds of playing
            {
                if (Time.frameCount % 60 == 0) // Every 60 frames to avoid spam
                {
                    float audioTime = _songManager?.GetCurrentSongTime() ?? 0f;
                    double currentDspTime = AudioSettings.dspTime;
                    double timeSinceDelayStart = currentDspTime - _delayStartDspTime;
                    float calculatedTime = -3f + (float)timeSinceDelayStart;

                    Debug.Log($"🎵 Playing timing check: songTime={_songTime:F3}s, audioTime={audioTime:F3}s, calculated={calculatedTime:F3}s, diff={_songTime - calculatedTime:F3}s");
                }
            }

            // Calculate effective preempt time once (base + 3s delay)
            float effectivePreemptTime = preemptTime + 3f;

            // Debug timing info
            if (_nextNoteIndex < _notes.Count)
            {
                float nextNoteTime = _notes[_nextNoteIndex].Time;
                float timeUntilNote = nextNoteTime - _songTime;
                if (timeUntilNote <= effectivePreemptTime + 1f) // Log when close to spawning
                {
                    Debug.Log($"Timing: songTime={_songTime:F3}s, nextNote={nextNoteTime:F3}s, timeUntil={timeUntilNote:F3}s, preempt={preemptTime:F3}s, effectivePreempt={effectivePreemptTime:F3}s");

                    // Special debug for notes at time 0 during delay period
                    if (nextNoteTime == 0f && _songTime < 0f)
                    {
                        Debug.Log($"*** TIME 0 NOTE DURING DELAY ***");
                        Debug.Log($"  songTime: {_songTime:F3}s (delay period)");
                        Debug.Log($"  nextNoteTime: {nextNoteTime:F3}s");
                        Debug.Log($"  timeUntilNote: {timeUntilNote:F3}s");
                        Debug.Log($"  effectivePreemptTime: {effectivePreemptTime:F3}s");
                        Debug.Log($"  Should spawn: {timeUntilNote <= effectivePreemptTime}");
                    }
                }
            }

            // Note spawning logic - only spawn notes when timing system is ready
            if (_timingState == TimingState.Delay || _timingState == TimingState.Playing)
            {
                while (_nextNoteIndex < _notes.Count)
                {
                    float nextNoteTime = _notes[_nextNoteIndex].Time;
                    float timeUntilNote = nextNoteTime - _songTime;

                    // Check if this note should spawn now
                    if (timeUntilNote <= effectivePreemptTime)
                    {
                        Debug.Log($"Spawning note at {nextNoteTime}s (songTime: {_songTime:F3}s, timeUntil: {timeUntilNote:F3}s, effectivePreempt: {effectivePreemptTime:F3}s)");
                        SpawnNote(_notes[_nextNoteIndex]);
                        _nextNoteIndex++;
                    }
                    else
                    {
                        // Note is too far in the future, wait for next frame
                        break;
                    }
                }
            }
            else
            {
                // Timing system not ready yet, don't spawn notes
                if (Time.frameCount % 60 == 0) // Log every 60 frames to avoid spam
                {
                    Debug.Log($"Timing system not ready (state: {_timingState}), waiting to spawn notes...");
                }
            }

            // [-- MISS HANDLING --]
            // If the next judgable note has passed its hit window and wasn't hit mark as missed
            while (_nextJudgableNoteIndex < _activeNotes.Count)
            {
                Note note = _activeNotes[_nextJudgableNoteIndex];

                // Check if note is valid before processing
                if (!IsNoteValid(note))
                {
                    // Remove invalid note from list and continue
                    _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                    continue;
                }

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

                    // Check if note still exists before accessing transform
                    if (note != null && note.gameObject != null)
                    {
                        PlayMissEffect(note.transform.position); // Play miss visual/sound effect
                    }
                    else
                    {
                        // Use a fallback position if note is destroyed
                        PlayMissEffect(_judgementCircle.position);
                    }

                    // Destroy the missed note and remove it from the list
                    if (note.gameObject != null)
                    {
                        Destroy(note.gameObject);
                    }
                    _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                    // Don't increment index since we removed an element
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

        private void CleanupDestroyedNotes()
        {
            // Remove all null references and destroyed/disabled notes from the active notes list
            int originalCount = _activeNotes.Count;

            // Remove notes that are null, destroyed, or have disabled components
            _activeNotes.RemoveAll(note =>
                note == null ||
                note.gameObject == null ||
                !note.enabled ||
                !note.gameObject.activeInHierarchy
            );

            // Adjust the next judgable note index if it's out of bounds
            if (_nextJudgableNoteIndex >= _activeNotes.Count)
            {
                _nextJudgableNoteIndex = Mathf.Max(0, _activeNotes.Count - 1);
            }

            // Log cleanup if significant changes occurred
            if (originalCount != _activeNotes.Count)
            {
                Debug.Log($"Cleaned up notes: {originalCount} -> {_activeNotes.Count}, next index: {_nextJudgableNoteIndex}");
            }
        }

        // Additional validation method to check note integrity
        private bool IsNoteValid(Note note)
        {
            return note != null &&
                   note.gameObject != null &&
                   note.enabled &&
                   note.gameObject.activeInHierarchy;
        }

        // Public method to force cleanup of all invalid notes (useful for debugging)
        [ContextMenu("Force Cleanup All Notes")]
        public void ForceCleanupAllNotes()
        {
            int originalCount = _activeNotes.Count;
            Debug.Log($"Force cleanup: Starting with {originalCount} notes");

            // Remove all invalid notes
            _activeNotes.RemoveAll(note => !IsNoteValid(note));

            // Adjust indices
            if (_nextJudgableNoteIndex >= _activeNotes.Count)
            {
                _nextJudgableNoteIndex = Mathf.Max(0, _activeNotes.Count - 1);
            }

            Debug.Log($"Force cleanup: {originalCount} -> {_activeNotes.Count} notes, next index: {_nextJudgableNoteIndex}");
        }

        // Method to check note system health
        [ContextMenu("Check Note System Health")]
        public void CheckNoteSystemHealth()
        {
            Debug.Log($"=== NOTE SYSTEM HEALTH CHECK ===");
            Debug.Log($"Total active notes: {_activeNotes.Count}");
            Debug.Log($"Next judgable note index: {_nextJudgableNoteIndex}");
            Debug.Log($"Timing state: {_timingState}");
            Debug.Log($"Current song time: {_songTime:F3}s");

            int validNotes = 0;
            int hitNotes = 0;
            int disabledNotes = 0;
            int destroyedNotes = 0;

            for (int i = 0; i < _activeNotes.Count; i++)
            {
                Note note = _activeNotes[i];
                if (note == null)
                {
                    destroyedNotes++;
                }
                else if (note.gameObject == null)
                {
                    destroyedNotes++;
                }
                else if (!note.enabled)
                {
                    disabledNotes++;
                }
                else if (note.isHit)
                {
                    hitNotes++;
                }
                else
                {
                    validNotes++;
                }
            }

            Debug.Log($"Notes summary: {validNotes} valid, {hitNotes} hit, {disabledNotes} disabled, {destroyedNotes} destroyed");

            if (destroyedNotes > 0)
            {
                Debug.LogWarning($"Found {destroyedNotes} destroyed notes in list - this indicates cleanup issues!");
            }

            if (disabledNotes > 0)
            {
                Debug.LogWarning($"Found {disabledNotes} disabled notes - this may cause hit detection issues!");
            }

            Debug.Log($"================================");
        }

        private void SpawnNote(HitObject noteData)
        {
            GameObject prefabToSpawn = notePrefab;
            var obj = Instantiate(prefabToSpawn);
            var note = obj.GetComponent<Note>();

            // Use original preempt time for note travel speed (2.0s), but notes spawn early due to effectivePreemptTime
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

        private void InitializeGameState()
        {
            _score = 0;
            _combo = 0;
            OnScoreChange?.Invoke(_score);
            OnComboChange?.Invoke(_combo);
            // Note: Audio is now handled by the chart system, not here
        }


        public float GetSmoothedSongTime()
        {
            // Use synchronized time from audio source for more accurate timing
            return GetSynchronizedSongTime();
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

                // Check if note is valid before processing
                if (!IsNoteValid(note))
                {
                    // Remove invalid note from list and continue
                    _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                    continue;
                }

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
                            // Remove from active notes list
                            _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                            // Don't increment index since we removed an element
                        }
                        else
                        {
                            // Destroy normal notes
                            Destroy(note.gameObject);
                            // Remove from active notes list
                            _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                            // Don't increment index since we removed an element
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
                        // Remove from active notes list
                        _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                        // Don't increment index since we removed an element
                    }
                    else
                    {
                        // Destroy normal notes
                        Destroy(note.gameObject);
                        // Remove from active notes list
                        _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                        // Don't increment index since we removed an element
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
            if (_songManager == null || !_songManager.IsAudioReady())
            {
                return 0f;
            }

            float length = _songManager.GetSongLength();
            if (length <= 0f)
                return 0f;

            return _songManager.GetSongProgress() / length;
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
                    AddGauge(2.5f); // Increased from 0.25f for more responsive gauge
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
                    AddGauge(1.25f); // Increased from 0.125f for more responsive gauge
                    break;

                case Judgement.Miss:
                    hitBads++;
                    debugJudgementIndicator.text = "Bad";
                    _combo = 0;
                    OnComboChange?.Invoke(_combo);
                    PlayMissEffect(note.transform.position);
                    AddGauge(-2.5f); // Increased from -0.25f for more responsive gauge
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
            float oldGauge = _gauge;
            _gauge += amount;
            _gauge = Mathf.Clamp(_gauge, 0, _maxGauge);

            Debug.Log($"Gauge: {oldGauge:F2} + {amount:F2} = {_gauge:F2} ({(_gauge / _maxGauge * 100):F1}%)");
        }

        private float GetAccuracy()
        {
            // uses the osu taiko formula
            float nominator = hitGoods + hitOkays * 0.5f;
            float sum = hitGoods + hitOkays + hitBads;
            return nominator / sum * 100;
        }


        // private float GetStressedAccuracy()
        // {
        //     // uses the osu taiko formula
        //     hitGoods = 2147483647;
        //     hitOkays = 2147483647;

        //     float nominator = hitGoods + hitOkays * 0.5f;
        //     float sum = hitGoods + hitOkays + hitBads;
        //     return nominator / sum * 100;
        // }

        public string GetSongTimestamp()
        {
            if (_songManager == null || !_songManager.IsAudioReady())
            {
                return "00:00 / 00:00";
            }
            return _songManager.GetSongPositionString() + " / " + _songManager.GetSongLengthString();
        }

        public float GetGaugePercentage()
        {
            return _gauge / _maxGauge;
        }

        public bool IsSongProgressReady()
        {
            return _songManager != null && _songManager.IsAudioReady();
        }

        // Chart system methods
        public void StartGame(SongEntry song, ChartData chart)
        {
            Debug.Log($"=== MANUAL SONG SELECTION ===");
            Debug.Log($"Song: {song.Title} - {song.Artist}");
            Debug.Log($"Chart: {chart.Version} ({chart.Difficulty})");
            Debug.Log($"Notes: {chart.HitObjects.Count}");
            Debug.Log($"Audio: {song.AudioFilename ?? "None"}");
            Debug.Log($"================================");

            _currentSong = song;
            _currentChart = chart;

            // Mark that this song was manually selected (prevents auto-start from overriding)
            _songManuallySelected = true;
            Debug.Log("Manual selection flag set - auto-start will be disabled");

            // Trigger song change event for UI updates
            OnSongChanged?.Invoke(song, chart);

            // Load the chart data
            LoadChart(chart);

            // Automatically load and play the audio from the chart data
            if (!string.IsNullOrEmpty(song.AudioFilename))
            {
                Debug.Log($"Auto-loading audio from chart: {song.AudioFilename}");
                Debug.Log($"Audio file exists: {System.IO.File.Exists(song.AudioFilename)}");
                Debug.Log($"Audio file directory: {System.IO.Path.GetDirectoryName(song.AudioFilename)}");
                StartCoroutine(LoadAndPlayAudio(song.AudioFilename));
            }
            else
            {
                Debug.LogWarning("No audio file specified in chart data");
            }

            // Reset game state
            ResetGameState();

            Debug.Log($"Started game with chart: {chart.Version}");
            Debug.Log($"Chart has {chart.HitObjects.Count} hit objects");
            Debug.Log($"First note at: {(chart.HitObjects.Count > 0 ? chart.HitObjects[0].Time : 0)}s");
            Debug.Log($"Last note at: {(chart.HitObjects.Count > 0 ? chart.HitObjects[chart.HitObjects.Count - 1].Time : 0)}s");
        }

        // Method for testing - load chart without audio
        public void LoadChartOnly(ChartData chart)
        {
            _currentChart = chart;

            // Mark that this chart was manually selected (prevents auto-start from overriding)
            _songManuallySelected = true;

            // Find the song that contains this chart and trigger the event
            if (_currentSong != null)
            {
                OnSongChanged?.Invoke(_currentSong, chart);
            }

            LoadChart(chart);
            ResetGameState();

            Debug.Log($"Loaded chart only: {chart.Version}");
            Debug.Log($"Chart has {chart.HitObjects.Count} hit objects");
            Debug.Log($"First note at: {(chart.HitObjects.Count > 0 ? chart.HitObjects[0].Time : 0)}s");
            Debug.Log($"Last note at: {(chart.HitObjects.Count > 0 ? chart.HitObjects[chart.HitObjects.Count - 1].Time : 0)}s");
        }

        // Method to load audio from a song entry
        public void LoadAudioFromSong(SongEntry song)
        {
            if (!string.IsNullOrEmpty(song.AudioFilename))
            {
                Debug.Log($"Loading audio from song: {song.AudioFilename}");
                StartCoroutine(LoadAndPlayAudio(song.AudioFilename));
            }
            else
            {
                Debug.LogWarning("Song has no audio file specified");
            }
        }

        // Method to get current audio status
        public string GetAudioStatus()
        {
            if (_songManager != null)
            {
                AudioClip currentClip = _songManager.GetCurrentAudioClip();
                if (currentClip != null)
                {
                    return $"Audio: {currentClip.name} ({currentClip.length:F1}s) - Playing: {_songManager.IsPlaying()}";
                }
                else
                {
                    return "No audio loaded";
                }
            }
            return "SongManager not available";
        }

        // Method to get current timing status for debugging
        [ContextMenu("Check Timing Status")]
        public void CheckTimingStatus()
        {
            Debug.Log($"=== TIMING STATUS ===");
            Debug.Log($"Chart: {_currentChart?.Version ?? "None"}");
            Debug.Log($"Total notes: {_currentChart?.HitObjects.Count ?? 0}");
            Debug.Log($"Next note index: {_nextNoteIndex}");
            Debug.Log($"Current song time: {_songTime:F3}s");
            Debug.Log($"Audio source time: {_songManager?.GetCurrentSongTime():F3}s");

            if (_nextNoteIndex < _notes.Count)
            {
                float nextNoteTime = _notes[_nextNoteIndex].Time;
                float timeUntil = nextNoteTime - _songTime;
                Debug.Log($"Next note at: {nextNoteTime}s, time until: {timeUntil:F3}s");
            }
            Debug.Log($"====================");
        }

        // Method to get timing status as string (for UI display)
        public string GetTimingStatus()
        {
            float synchronizedTime = GetSynchronizedSongTime();
            float displayTime = GetDisplaySongTime();
            float audioTime = _songManager?.GetCurrentSongTime() ?? 0f;

            return $"Timing Status:\n" +
                   $"Synchronized Time: {synchronizedTime:F3}s\n" +
                   $"Display Time: {displayTime:F3}s\n" +
                   $"Audio Source Time: {audioTime:F3}s\n" +
                   $"Song Start DSP: {songStartTime:F3}s\n" +
                   $"Current DSP: {AudioSettings.dspTime:F3}s\n" +
                   $"Next Note Index: {_nextNoteIndex}/{_notes.Count}";
        }

        // Method to check auto-start status and provide guidance
        [ContextMenu("Check Auto-Start Status")]
        public void CheckAutoStartStatus()
        {
            Debug.Log($"=== AUTO-START STATUS ===");
            Debug.Log($"Auto-start enabled: {!_songManuallySelected}");
            Debug.Log($"Current song: {_currentSong?.Title ?? "None"}");
            Debug.Log($"Current chart: {_currentChart?.Version ?? "None"}");
            Debug.Log($"");
            Debug.Log($"To start a game:");
            Debug.Log($"1. Open SongSelectionEditor (AshTaiko > Song Selection Editor)");
            Debug.Log($"2. Select a song and difficulty");
            Debug.Log($"3. Click '🎮 Start Game'");
            Debug.Log($"");
            Debug.Log($"Context menu options:");
            Debug.Log($"- 'Enable Auto-Start': Re-enable automatic game start");
            Debug.Log($"- 'Disable Auto-Start': Disable automatic game start");
            Debug.Log($"- 'Trigger Auto-Start': Manually start auto-start now");
            Debug.Log($"================================");
        }

        // Method to get detailed audio path information
        public string GetAudioPathInfo()
        {
            if (_currentSong != null)
            {
                string audioPath = _currentSong.AudioFilename;
                if (!string.IsNullOrEmpty(audioPath))
                {
                    bool exists = System.IO.File.Exists(audioPath);
                    string directory = System.IO.Path.GetDirectoryName(audioPath);
                    string filename = System.IO.Path.GetFileName(audioPath);

                    return $"Song: {_currentSong.Title}\n" +
                           $"Audio Path: {audioPath}\n" +
                           $"File Exists: {exists}\n" +
                           $"Directory: {directory}\n" +
                           $"Filename: {filename}";
                }
                else
                {
                    return "No audio file specified in song";
                }
            }
            return "No current song loaded";
        }

        // Method to manually load audio from a file path (for testing)
        [ContextMenu("Load Audio From Path")]
        public void LoadAudioFromPath()
        {
            // This can be used for testing - you can modify the path here
            string testPath = "C:/Path/To/Your/Audio.mp3";
            if (System.IO.File.Exists(testPath))
            {
                StartCoroutine(LoadAndPlayAudio(testPath));
            }
            else
            {
                Debug.LogWarning($"Test audio path not found: {testPath}");
            }
        }

        // Method to test audio loading from current song (for debugging)
        [ContextMenu("Test Load Current Song Audio")]
        public void TestLoadCurrentSongAudio()
        {
            if (_currentSong != null && !string.IsNullOrEmpty(_currentSong.AudioFilename))
            {
                Debug.Log($"Testing audio load for current song: {_currentSong.Title}");
                Debug.Log($"Audio path: {_currentSong.AudioFilename}");
                StartCoroutine(LoadAndPlayAudio(_currentSong.AudioFilename));
            }
            else
            {
                Debug.LogWarning("No current song or audio file available");
            }
        }

        // Method to test audio loading with a specific path (for debugging)
        [ContextMenu("Test Load Audio From Specific Path")]
        public void TestLoadAudioFromSpecificPath()
        {
            // You can modify this path to test with a known audio file
            string testPath = @"X:\Projects\AshTaiko\Part B\AshTaiko\Assets\_AshTaiko\Assets\01. St. Chroma.mp3";
            Debug.Log($"Testing audio load from specific path: {testPath}");
            StartCoroutine(LoadAndPlayAudio(testPath));
        }

        // Method to test the new chart system with the first available song
        [ContextMenu("Test Chart System With First Song")]
        public void TestChartSystemWithFirstSong()
        {
            var database = ChartDatabase.Instance?.GetDatabase();
            if (database != null && database.Songs.Count > 0)
            {
                var firstSong = database.Songs[0];
                var firstChart = firstSong.Charts?.FirstOrDefault();

                if (firstChart != null)
                {
                    Debug.Log($"Testing chart system with: {firstSong.Title} - {firstChart.Version}");
                    StartGame(firstSong, firstChart);
                }
                else
                {
                    Debug.LogWarning($"No charts found for song: {firstSong.Title}");
                }
            }
            else
            {
                Debug.LogWarning("No songs in database or ChartDatabase not found");
            }
        }

        // Auto-start coroutine that starts the game with the first available chart
        private System.Collections.IEnumerator AutoStartWithFirstChart()
        {
            // Wait a few seconds for everything to initialize
            yield return new WaitForSeconds(2f);

            // Check if a song was manually selected - if so, don't auto-start
            if (_songManuallySelected)
            {
                Debug.Log("Song was manually selected - skipping auto-start");
                yield break;
            }

            Debug.Log("Auto-starting game with first available chart...");

            // Try to get the chart database
            var database = ChartDatabase.Instance?.GetDatabase();
            if (database != null && database.Songs.Count > 0)
            {
                var firstSong = database.Songs[0];
                Debug.Log($"Found song: {firstSong.Title} - {firstSong.Artist}");
                Debug.Log($"Available difficulties: {string.Join(", ", firstSong.Charts.Select(c => $"{c.Version} (Level {c.Difficulty})"))}");

                // Try to find a good difficulty to start with
                ChartData selectedChart = null;

                // Priority order: Normal > Hard > Easy > Oni > Edit
                var priorityOrder = new[] { Difficulty.Normal, Difficulty.Hard, Difficulty.Easy, Difficulty.Insane, Difficulty.Expert };

                foreach (var difficulty in priorityOrder)
                {
                    var chart = firstSong.Charts.FirstOrDefault(c => c.Difficulty == difficulty);
                    if (chart != null)
                    {
                        selectedChart = chart;
                        Debug.Log($"Selected {difficulty} difficulty: {chart.Version}");
                        break;
                    }
                }

                // Fallback to first available chart if no preferred difficulty found
                if (selectedChart == null && firstSong.Charts.Count > 0)
                {
                    selectedChart = firstSong.Charts[0];
                    Debug.Log($"No preferred difficulty found, using first available: {selectedChart.Version} ({selectedChart.Difficulty})");
                }

                if (selectedChart != null)
                {
                    Debug.Log($"Auto-starting with: {firstSong.Title} - {selectedChart.Version} ({selectedChart.Difficulty})");
                    Debug.Log($"Chart has {selectedChart.HitObjects.Count} notes, {selectedChart.TimingPoints.Count} timing points");
                    StartGame(firstSong, selectedChart);
                }
                else
                {
                    Debug.LogWarning($"No charts found for song: {firstSong.Title}");
                }
            }
            else
            {
                Debug.LogWarning("No songs in database or ChartDatabase not found - cannot auto-start");
            }
        }

        // Method to manually trigger auto-start (for testing)
        [ContextMenu("Trigger Auto-Start")]
        public void TriggerAutoStart()
        {
            Debug.Log("Manually triggering auto-start...");
            StartCoroutine(AutoStartWithFirstChart());
        }

        // Method to enable/disable auto-start behavior
        [ContextMenu("Enable Auto-Start")]
        public void EnableAutoStart()
        {
            Debug.Log("Auto-start enabled - will start automatically on next scene load");
            _songManuallySelected = false;
        }

        [ContextMenu("Disable Auto-Start")]
        public void DisableAutoStart()
        {
            Debug.Log("Auto-start disabled - use SongSelectionEditor to start games");
            _songManuallySelected = true;
        }

        // Method to check if timing system is ready
        [ContextMenu("Check Timing System Ready")]
        public void CheckTimingSystemReady()
        {
            bool audioLoaded = _songManager?.GetCurrentAudioClip() != null;
            bool timingReady = _timingState == TimingState.Delay || _timingState == TimingState.Playing;
            bool audioPlaying = _songManager?.IsPlaying() ?? false;

            Debug.Log($"=== TIMING SYSTEM READY CHECK ===");
            Debug.Log($"Audio loaded: {audioLoaded}");
            Debug.Log($"Timing state: {_timingState}");
            Debug.Log($"Timing ready: {timingReady}");
            Debug.Log($"Audio playing: {audioPlaying}");
            Debug.Log($"Delay start DSP: {_delayStartDspTime:F3}s");
            Debug.Log($"Current song time: {_songTime:F3}s");
            Debug.Log($"Audio source time: {_songManager?.GetCurrentSongTime():F3}s");

            if (audioLoaded && timingReady)
            {
                Debug.Log($"✅ Timing system is ready - notes should spawn and move correctly");
            }
            else
            {
                Debug.Log($"❌ Timing system not ready - audio: {audioLoaded}, timing state: {_timingState}");
            }
            Debug.Log($"================================");
        }

        // Method to detect timing drift during gameplay
        [ContextMenu("Detect Timing Drift")]
        public void DetectTimingDrift()
        {
            if (_timingState != TimingState.Playing)
            {
                Debug.Log("⚠️ Timing drift detection only works during Playing state");
                return;
            }

            Debug.Log($"=== TIMING DRIFT DETECTION ===");

            // Get current timing from different sources
            double currentDspTime = AudioSettings.dspTime;
            double timeSinceDelayStart = currentDspTime - _delayStartDspTime;
            float calculatedTime = -3f + (float)timeSinceDelayStart;
            float audioTime = _songManager?.GetCurrentSongTime() ?? 0f;

            Debug.Log($"Current song time: {_songTime:F3}s");
            Debug.Log($"Calculated from DSP: {calculatedTime:F3}s");
            Debug.Log($"Audio source time: {audioTime:F3}s");
            Debug.Log($"");

            // Calculate differences
            float dspDifference = Mathf.Abs(_songTime - calculatedTime);
            float audioDifference = Mathf.Abs(_songTime - audioTime);

            Debug.Log($"Difference from DSP timing: {dspDifference:F3}s");
            Debug.Log($"Difference from audio timing: {audioDifference:F3}s");
            Debug.Log($"");

            // Check for significant drift
            if (dspDifference > 0.1f)
            {
                Debug.LogWarning($"🚨 SIGNIFICANT DSP TIMING DRIFT DETECTED: {dspDifference:F3}s");
                Debug.LogWarning($"This could cause notes to become unhittable!");
            }

            if (audioDifference > 0.1f)
            {
                Debug.LogWarning($"🚨 SIGNIFICANT AUDIO TIMING DRIFT DETECTED: {audioDifference:F3}s");
                Debug.LogWarning($"This could cause notes to become unhittable!");
            }

            if (dspDifference <= 0.1f && audioDifference <= 0.1f)
            {
                Debug.Log("✅ Timing appears to be synchronized correctly");
            }

            Debug.Log($"================================");
        }

        // Method to check timing synchronization
        [ContextMenu("Check Timing Synchronization")]
        public void CheckTimingSynchronization()
        {
            Debug.Log($"=== TIMING SYNCHRONIZATION CHECK ===");
            Debug.Log($"Current timing state: {_timingState}");
            Debug.Log($"Current song time: {_songTime:F3}s");
            Debug.Log($"Delay start DSP: {_delayStartDspTime:F3}s");
            Debug.Log($"Current DSP time: {AudioSettings.dspTime:F3}s");

            if (_songManager != null)
            {
                float audioTime = _songManager.GetCurrentSongTime();
                Debug.Log($"Audio source time: {audioTime:F3}s");
                Debug.Log($"Time difference: {_songTime - audioTime:F3}s");
            }

            if (_timingState == TimingState.Delay || _timingState == TimingState.Playing)
            {
                double currentDspTime = AudioSettings.dspTime;
                double timeSinceDelayStart = currentDspTime - _delayStartDspTime;
                float calculatedTime = -3f + (float)timeSinceDelayStart;
                Debug.Log($"Calculated time from DSP: {calculatedTime:F3}s");
                Debug.Log($"Time difference from calculated: {_songTime - calculatedTime:F3}s");
            }

            Debug.Log($"================================");
        }

        // Method to reset timing system if there are issues
        [ContextMenu("Reset Timing System")]
        public void ResetTimingSystem()
        {
            Debug.Log($"🔄 Resetting timing system...");
            Debug.Log($"Previous state: {_timingState}, Previous song time: {_songTime:F3}s");

            // Reset timing state
            _timingState = TimingState.Uninitialized;
            _songTime = 0f;
            _delayStartDspTime = 0.0;

            // If we have a current chart and song, try to reinitialize
            if (_currentChart != null && _currentSong != null)
            {
                Debug.Log($"🔄 Reinitializing timing for current chart: {_currentChart.Version}");

                // Reload the chart to reset timing
                LoadChart(_currentChart);

                // If we have audio, restart the timing system
                if (_songManager?.GetCurrentAudioClip() != null)
                {
                    Debug.Log($"🔄 Restarting audio timing system");
                    StartCoroutine(LoadAndPlayAudio(_currentSong.AudioFilename));
                }
                else
                {
                    Debug.Log($"🔄 No audio available, timing system reset to uninitialized");
                }
            }
            else
            {
                Debug.Log($"🔄 No current chart/song, timing system reset to uninitialized");
            }

            Debug.Log($"🔄 Timing system reset complete. New state: {_timingState}");
        }

        // Method to check note synchronization
        [ContextMenu("Check Note Synchronization")]
        public void CheckNoteSynchronization()
        {
            Debug.Log($"=== NOTE SYNCHRONIZATION CHECK ===");
            Debug.Log($"Current song time: {_songTime:F3}s");
            Debug.Log($"Next note index: {_nextNoteIndex}/{_notes.Count}");
            Debug.Log($"Next judgable note index: {_nextJudgableNoteIndex}/{_activeNotes.Count}");
            Debug.Log($"Timing state: {_timingState}");

            if (_nextNoteIndex < _notes.Count)
            {
                float nextNoteTime = _notes[_nextNoteIndex].Time;
                float timeUntilNote = nextNoteTime - _songTime;
                Debug.Log($"Next note at: {nextNoteTime}s, time until: {timeUntilNote:F3}s");
                Debug.Log($"Should spawn: {timeUntilNote <= (preemptTime + 3f)}");
            }

            if (_nextJudgableNoteIndex < _activeNotes.Count)
            {
                Note nextJudgableNote = _activeNotes[_nextJudgableNoteIndex];
                if (nextJudgableNote != null)
                {
                    float timeUntilHit = nextJudgableNote.hitTime - _songTime;
                    Debug.Log($"Next judgable note at: {nextJudgableNote.hitTime:F3}s, time until: {timeUntilHit:F3}s");
                    Debug.Log($"Note type: {nextJudgableNote.noteType}, Is hit: {nextJudgableNote.isHit}");
                    Debug.Log($"Note position: {nextJudgableNote.transform.position}");
                    Debug.Log($"Judgement circle: {_judgementCircle.position}");
                    Debug.Log($"Distance to hit: {Vector3.Distance(nextJudgableNote.transform.position, _judgementCircle.position):F2}");
                }
                else
                {
                    Debug.LogWarning("Next judgable note is null!");
                }
            }

            Debug.Log($"================================");
        }

        // Method to reset manual selection flag (re-enables auto-start behavior)
        [ContextMenu("Reset Manual Selection Flag")]
        public void ResetManualSelectionFlag()
        {
            _songManuallySelected = false;
            Debug.Log("Manual selection flag reset - auto-start will work on next scene load");
        }

        // Method to check current manual selection status
        [ContextMenu("Check Manual Selection Status")]
        public void CheckManualSelectionStatus()
        {
            Debug.Log($"=== MANUAL SELECTION STATUS ===");
            Debug.Log($"Song manually selected: {_songManuallySelected}");
            Debug.Log($"Current song: {_currentSong?.Title ?? "None"}");
            Debug.Log($"Current chart: {_currentChart?.Version ?? "None"}");
            Debug.Log($"Auto-start will: {(_songManuallySelected ? "NOT run (manual selection active)" : "run (no manual selection)")}");
            Debug.Log($"================================");
        }

        // Method to force auto-start even when manual selection is active
        [ContextMenu("Force Auto-Start")]
        public void ForceAutoStart()
        {
            Debug.Log("Force auto-start requested - temporarily resetting manual selection flag");
            _songManuallySelected = false;
            StartCoroutine(AutoStartWithFirstChart());
        }

        // Method to explain the new timing system
        [ContextMenu("Explain Timing System")]
        public void ExplainTimingSystem()
        {
            Debug.Log($"=== TIMING SYSTEM EXPLANATION ===");
            Debug.Log($"Base Preempt Time: {preemptTime}s (notes travel this many seconds from spawn to hit)");
            Debug.Log($"Audio Delay: 3s (audio starts 3 seconds after scheduling)");
            Debug.Log($"Effective Preempt Time: {preemptTime + 3f}s (notes spawn early enough to be visible during delay)");
            Debug.Log($"");
            Debug.Log($"Timing Flow:");
            Debug.Log($"1. Scene loads, auto-start begins");
            Debug.Log($"2. Chart loads, audio loading starts (state: Loading)");
            Debug.Log($"3. Audio loaded, 3s delay begins (state: Delay)");
            Debug.Log($"4. Notes start spawning when songTime <= noteTime - {preemptTime + 3f}s");
            Debug.Log($"5. Notes travel at constant speed over {preemptTime}s (not {preemptTime + 3f}s)");
            Debug.Log($"6. Audio starts, state changes to Playing");
            Debug.Log($"7. Notes continue spawning and traveling normally");
            Debug.Log($"8. Notes reach hit bar exactly when they should be hit");
            Debug.Log($"");
            Debug.Log($"Negative Time System:");
            Debug.Log($"- During delay: songTime counts up from -3.0s to 0.0s");
            Debug.Log($"- Notes at time 0: spawn when songTime = -3.0s (3s early)");
            Debug.Log($"- Notes at time 2: spawn when songTime = -1.0s (3s early)");
            Debug.Log($"- After delay: songTime goes from 0.0s onwards normally");
            Debug.Log($"");
            Debug.Log($"Time Calculation During Delay:");
            Debug.Log($"- songStartTime = DSP time when audio was scheduled + 3s");
            Debug.Log($"- timeSinceDelayStart = current DSP time - songStartTime");
            Debug.Log($"- synchronizedTime = -3f + timeSinceDelayStart");
            Debug.Log($"- This creates a countdown: -3.0s → -2.0s → -1.0s → 0.0s");
            Debug.Log($"");
            Debug.Log($"Key Point: Notes spawn early (5s before hit) but travel fast (2s travel time)");
            Debug.Log($"This gives players 3s to see notes coming before audio starts!");
            Debug.Log($"========================");
        }

        // Method to start with a specific song by index (for testing)
        [ContextMenu("Start With Song Index 0")]
        public void StartWithSongIndex0()
        {
            StartWithSongByIndex(0);
        }

        [ContextMenu("Start With Song Index 1")]
        public void StartWithSongIndex1()
        {
            StartWithSongByIndex(1);
        }

        // Method to start with a song by index
        public void StartWithSongByIndex(int songIndex)
        {
            var database = ChartDatabase.Instance?.GetDatabase();
            if (database != null && songIndex < database.Songs.Count)
            {
                var song = database.Songs[songIndex];
                var chart = song.Charts?.FirstOrDefault();

                if (chart != null)
                {
                    Debug.Log($"Starting with song index {songIndex}: {song.Title} - {chart.Version}");
                    StartGame(song, chart);
                }
                else
                {
                    Debug.LogWarning($"No charts found for song at index {songIndex}: {song.Title}");
                }
            }
            else
            {
                Debug.LogWarning($"Song index {songIndex} not found or ChartDatabase not available");
            }
        }

        // Method to switch to a different difficulty of the current song
        [ContextMenu("Switch to Normal Difficulty")]
        public void SwitchToNormalDifficulty()
        {
            SwitchToDifficulty(Difficulty.Normal);
        }

        [ContextMenu("Switch to Hard Difficulty")]
        public void SwitchToHardDifficulty()
        {
            SwitchToDifficulty(Difficulty.Hard);
        }

        [ContextMenu("Switch to Easy Difficulty")]
        public void SwitchToEasyDifficulty()
        {
            SwitchToDifficulty(Difficulty.Easy);
        }

        [ContextMenu("Switch to Oni Difficulty")]
        public void SwitchToOniDifficulty()
        {
            SwitchToDifficulty(Difficulty.Insane);
        }

        [ContextMenu("Switch to Edit Difficulty")]
        public void SwitchToEditDifficulty()
        {
            SwitchToDifficulty(Difficulty.Normal); // Edit maps to Normal
        }

        private void SwitchToDifficulty(Difficulty difficulty)
        {
            if (_currentSong == null)
            {
                Debug.LogWarning("No current song loaded - cannot switch difficulty");
                return;
            }

            var chart = _currentSong.Charts.FirstOrDefault(c => c.Difficulty == difficulty);
            if (chart != null)
            {
                Debug.Log($"Switching to {difficulty} difficulty: {chart.Version}");
                Debug.Log($"Chart has {chart.HitObjects.Count} notes, {chart.TimingPoints.Count} timing points");
                StartGame(_currentSong, chart);
            }
            else
            {
                Debug.LogWarning($"Difficulty {difficulty} not found for current song: {_currentSong.Title}");
                Debug.Log($"Available difficulties: {string.Join(", ", _currentSong.Charts.Select(c => $"{c.Version} ({c.Difficulty})"))}");
            }
        }

        // Method for testing - force spawn the next note
        public void ForceSpawnNextNote()
        {
            if (_nextNoteIndex < _notes.Count)
            {
                Debug.Log($"Force spawning note {_nextNoteIndex} at {_notes[_nextNoteIndex].Time}s");
                SpawnNote(_notes[_nextNoteIndex]);
                _nextNoteIndex++;
            }
            else
            {
                Debug.Log("No more notes to spawn!");
            }
        }

        private void LoadChart(ChartData chart)
        {
            _notes.Clear();
            _notes.AddRange(chart.HitObjects);

            // Sort notes by time
            _notes.Sort((a, b) => a.Time.CompareTo(b.Time));

            _nextNoteIndex = 0;
            _nextJudgableNoteIndex = 0;

            // Clear any existing active notes to prevent null reference issues
            foreach (var note in _activeNotes)
            {
                if (note != null)
                {
                    Destroy(note.gameObject);
                }
            }
            _activeNotes.Clear();

            // Set song end time
            if (chart.HitObjects.Count > 0)
            {
                songEndTime = chart.TotalLength;
            }

            // Initialize timing for the new chart
            _songTime = -3f; // Start in delay period

            Debug.Log($"Chart loaded: {chart.HitObjects.Count} notes, first at {(chart.HitObjects.Count > 0 ? chart.HitObjects[0].Time : 0)}s");
            Debug.Log($"Timing initialized: songTime={_songTime}s, will start spawning notes at 0s");
        }

        private System.Collections.IEnumerator LoadAndPlayAudio(string audioPath)
        {
            Debug.Log($"Loading audio from: {audioPath}");
            Debug.Log($"Audio file exists: {System.IO.File.Exists(audioPath)}");
            Debug.Log($"Audio file directory: {System.IO.Path.GetDirectoryName(audioPath)}");
            Debug.Log($"Audio file name: {System.IO.Path.GetFileName(audioPath)}");

            // Set state to loading
            _timingState = TimingState.Loading;
            Debug.Log($"Timing state: {_timingState}");

            // Check if the audio file exists
            if (!System.IO.File.Exists(audioPath))
            {
                Debug.LogError($"Audio file not found: {audioPath}");
                _timingState = TimingState.Uninitialized;
                yield break;
            }

            // Load audio file using UnityWebRequest (modern approach)
            string fullPath = "file://" + audioPath;
            Debug.Log($"UnityWebRequest path: {fullPath}");

            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip(fullPath, AudioType.UNKNOWN))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    AudioClip clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        Debug.Log($"Audio loaded successfully: {clip.name}, length: {clip.length}s");

                        // IMMEDIATELY assign the audio clip to SongManager
                        _songManager.SetAudioClip(clip);

                        // Now that audio is loaded, we can start the timing system
                        Debug.Log($"Audio clip assigned to SongManager, starting timing system...");

                        // Use PlayScheduled(3) for proper audio synchronization
                        _songManager.PlayInSeconds(3);

                        // Set the delay start time to NOW (when we actually start the delay)
                        _delayStartDspTime = AudioSettings.dspTime;
                        _songTime = -3f; // Start at -3 to indicate we're in the delay period

                        // Set state to delay
                        _timingState = TimingState.Delay;

                        Debug.Log($"Audio scheduled to start at DSP time: {_delayStartDspTime + 3.0:F3}s (3 second delay)");
                        Debug.Log($"Song time initialized to: {_songTime:F3}s (delay period)");
                        Debug.Log($"Current DSP time: {AudioSettings.dspTime:F3}s");
                        Debug.Log($"Delay started at: {_delayStartDspTime:F3}s");
                        Debug.Log($"Timing state: {_timingState}");

                        // Force an immediate update of the timing system
                        Debug.Log($"Timing system initialized - notes should now spawn and move correctly");
                    }
                    else
                    {
                        Debug.LogError("Failed to get AudioClip from UnityWebRequest");
                        _timingState = TimingState.Uninitialized;
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load audio: {www.error}");
                    _timingState = TimingState.Uninitialized;
                }
            }
        }

        private void ResetGameState()
        {
            _score = 0;
            _combo = 0;
            _gauge = 0f; // Start at 0 - player must earn gauge by hitting notes
            _songTime = -3f; // Start in delay period
            _nextNoteIndex = 0;
            _nextJudgableNoteIndex = 0;

            // Clear active notes
            foreach (var note in _activeNotes)
            {
                if (note != null)
                {
                    Destroy(note.gameObject);
                }
            }
            _activeNotes.Clear();

            // Clear drumroll state
            _isInDrumroll = false;
            _currentDrumroll = null;
            _drumrollHits = 0;

            // Clear drumroll bridges
            foreach (var bridge in _activeDrumrollBridges)
            {
                if (bridge != null)
                {
                    Destroy(bridge.gameObject);
                }
            }
            _activeDrumrollBridges.Clear();

            OnScoreChange?.Invoke(_score);
            OnComboChange?.Invoke(_combo);
            OnAccuracyChange?.Invoke(GetAccuracy());
        }

        public SongEntry GetCurrentSong()
        {
            return _currentSong;
        }

        public ChartData GetCurrentChart()
        {
            return _currentChart;
        }

        // Method to check if manual selection is active
        public bool IsManualSelectionActive()
        {
            return _songManuallySelected;
        }

        // Method to get current song selection info for UI display
        public string GetCurrentSongInfo()
        {
            if (_currentSong == null || _currentChart == null)
            {
                return "No song selected";
            }

            string selectionType = _songManuallySelected ? "Manual Selection" : "Auto-Start";
            return $"{_currentSong.Title} - {_currentChart.Version} ({_currentChart.Difficulty}) [{selectionType}]";
        }

        // Method to check if the game is ready to play
        public bool IsGameReadyToPlay()
        {
            return _currentSong != null && _currentChart != null && _songManager?.GetCurrentAudioClip() != null;
        }

        // Method to get game readiness status for UI display
        public string GetGameReadinessStatus()
        {
            if (_currentSong == null)
                return "No song selected";
            if (_currentChart == null)
                return "No chart selected";
            if (_songManager?.GetCurrentAudioClip() == null)
                return "Audio not loaded";
            if (_timingState == TimingState.Uninitialized || _timingState == TimingState.Loading)
                return "Audio loading...";
            if (_timingState == TimingState.Delay)
                return "Game starting in " + (-_songTime).ToString("F1") + "s";
            if (_timingState == TimingState.Playing)
                return "Game playing";

            return "Unknown state";
        }

        // Method to set the selected chart without starting the game (for editor preview)
        public void SetSelectedChart(ChartData chart)
        {
            if (chart == null) return;

            // Find the song that contains this chart
            var database = ChartDatabase.Instance?.GetDatabase();
            if (database != null)
            {
                foreach (var song in database.Songs)
                {
                    if (song.Charts != null && song.Charts.Contains(chart))
                    {
                        _currentSong = song;
                        _currentChart = chart;
                        _songManuallySelected = true;

                        Debug.Log($"Chart selection updated: {song.Title} - {chart.Version} ({chart.Difficulty})");
                        Debug.Log($"Chart has {chart.HitObjects.Count} notes, first at {(chart.HitObjects.Count > 0 ? chart.HitObjects[0].Time : 0)}s");

                        // Trigger song change event for UI updates
                        OnSongChanged?.Invoke(song, chart);

                        // Load the chart data but don't start audio
                        LoadChart(chart);
                        return;
                    }
                }
            }

            Debug.LogWarning($"Could not find song for chart: {chart.Version}");
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
