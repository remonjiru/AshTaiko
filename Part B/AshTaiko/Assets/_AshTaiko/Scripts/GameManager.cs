using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace AshTaiko
{
    /*
     * GameManager serves as the central orchestrator for the Taiko rhythm game, managing all core gameplay systems.
     * This class implements a singleton pattern to provide global access to game state, though this creates tight coupling
     * that should be refactored to use dependency injection in future iterations.
     * 
     * The class handles multiple responsibilities including:
     * - Timing synchronization between audio playback and visual note movement
     * - Note spawning and lifecycle management
     * - Hit detection and scoring systems
     * - Game state transitions and management
     * - Integration with chart data and audio systems
     * 
     * Data Structure Design:
     * - Uses Lists for dynamic collections (notes, active notes) to allow runtime addition/removal
     * - Implements state machine pattern with TimingState enum for managing audio loading phases
     * - Stores timing data as float values in seconds for precision and Unity compatibility
     * - Uses double for DSP time calculations to maintain precision during long audio sessions
     */
    /// <summary>
    /// Central game controller managing timing, note spawning, hit detection, scoring, and gameplay loop.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton and Core References

        /*
         * Singleton Pattern Implementation:
         * The singleton pattern provides global access to game state from any component without requiring
         * explicit references to be passed around. This simplifies component communication but creates
         * tight coupling that makes testing and modularity difficult.
         * 
         * Alternative approaches for future refactoring:
         * - Dependency injection container for managing component references
         * - Event-driven architecture using ScriptableObject-based events
         * - Service locator pattern for component discovery
         */
        /// <summary>
        /// Global singleton instance providing access to game state from anywhere in the codebase.
        /// </summary>
        public static GameManager Instance { get; private set; }

        #endregion

        #region Prefab and Component References

        /*
         * Note Prefab System:
         * Note prefabs are instantiated at runtime to represent individual chart notes. Using prefabs
         * allows for efficient object pooling and consistent visual representation across all notes.
         * The drumroll bridge prefab creates visual connections between drumroll start and end notes.
         */
        [Header("Note Prefabs")]
        [SerializeField]
        private GameObject _notePrefab;
        
        [SerializeField]
        private GameObject _drumrollBridgePrefab;

        /*
         * Audio System Integration:
         * SongManager handles audio playback and provides timing information. This separation of concerns
         * allows the GameManager to focus on gameplay logic while delegating audio management to specialists.
         * The relationship is loosely coupled through interfaces and events.
         */
        /// <summary>
        /// Manages audio playback and provides synchronized timing information for gameplay.
        /// </summary>
        [SerializeField]
        private SongManager _songManager;

        /*
         * Input System:
         * Drum component handles player input detection and converts raw input into game events.
         * This separation allows for easy input system swapping (keyboard, controller, touch) without
         * affecting core gameplay logic.
         */
        [SerializeField]
        private Drum _drum;

        /*
         * Visual Reference Points:
         * Transform components provide world space positions for note spawning and hit detection.
         * Using Transform instead of Vector3 allows for runtime position updates and scene hierarchy integration.
         */
        [SerializeField]
        private Transform _judgementCircle;

        /*
         * Visual Effects:
         * Hit effects are instantiated at note hit positions to provide player feedback.
         * GameObject references allow for runtime instantiation and destruction of effect instances.
         */
        [SerializeField]
        private GameObject _hitEffect;

        #endregion

        #region Note Management System

        /*
         * The notes list stores all chart data as HitObject instances for note spawning.
         * Using List<T> allows for dynamic chart loading and runtime modification.
         * The nextNoteIndex tracks the current position in the chart for sequential spawning.
         */
        [SerializeField]
        private List<HitObject> _notes = new List<HitObject>();
        private int _nextNoteIndex = 0;

        #endregion

        #region Chart and Song Data

        /*
         * SongEntry contains metadata about the current song (title, artist, audio file).
         * ChartData contains the actual gameplay data (note timings, difficulty settings).
         * This separation allows for multiple difficulty charts per song while sharing common metadata.
         */
        private SongEntry _currentSong;
        private ChartData _currentChart;

        #endregion

        #region Timing System

        /*
         * Preempt time determines how early notes spawn before their hit time.
         * Travel distance defines the spawn-to-hit distance for consistent note movement speeds.
         * Using float values provides sufficient precision for Unity compatibility.
         */
        private float _preemptTime = 2.0f;

        [SerializeField]
        private float _travelDistance = 15f;

        /*
         * SongTime represents the current playback position in the chart, synchronized with audio playback.
         * This value drives all timing-dependent systems including note spawning, hit detection, and scoring.
         */
        private float _songTime;

        /*
         * TimingState enum implements a state machine pattern to manage audio loading and synchronization.
         * State Flow: Uninitialized -> Loading -> Delay -> Playing
         * The Delay state implements a 3-second countdown before audio starts.
         */
        private enum TimingState
        {
            Uninitialized,    // No audio loaded yet
            Loading,          // Audio is loading
            Delay,            // Audio loaded, waiting for 3s delay
            Playing           // Audio is playing normally
        }

        private TimingState _timingState = TimingState.Uninitialized;
        
        /*
         * DSP time provides high-precision timing for audio synchronization.
         * Using double precision prevents timing drift during long audio sessions.
         * The delay start time enables continuous timing through the delay period.
         */
        private double _delayStartDspTime = 0.0;

        /*
         * This flag prevents automatic song selection from overriding user choices during gameplay.
         * Boolean type provides simple state tracking with minimal memory overhead.
         */
        private bool _songManuallySelected = false;

        #endregion

        #region Core Properties

        /*
         * Core Timing Property:
         * SongTime provides read-only access to the current synchronized playback time.
         * Using a property instead of a public field ensures encapsulation and allows for
         * future validation or side effects without breaking external code.
         */
        public float SongTime
        {
            get => _songTime;
        }

        #endregion

        #region Timing Methods

        /*
         * Provides core timing synchronization between visual and audio systems.
         * Handles timing calculations during the delay period and maintains continuous timing.
         * During delay, time counts from -3.0s to 0.0s, then continues normally.
         */
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

        /*
         * Converts internal synchronized time to positive display time for UI elements.
         * Ensures UI never shows negative values during the delay period.
         */
        public float GetDisplaySongTime()
        {
            float synchronizedTime = GetSynchronizedSongTime();
            // Convert negative time (during delay) to positive display time
            return Mathf.Max(0f, synchronizedTime);
        }

        #endregion

        #region Game State Fields

        /*
         * DSP time smoothing mechanism prevents jitter and provides stable timing.
         * The smoothing algorithm interpolates between DSP time updates for consistent frame rates.
         */
        private float _smoothedDsp;
        private double _lastDsp;

        /*
         * Song duration tracking for progress calculations and UI display.
         * Using float values provides sufficient precision for song duration.
         */
        private float _songStartTime = 0;
        private float _songEndTime = 0;

        /*
         * Frame timing system tracks timing information for each frame.
         * Uses both DSP time and Unity time for maximum accuracy.
         */
        private double _lastDspTime;
        private double _currentDspTime;
        private float _frameStartTime;

        /*
         * Active notes list contains all currently spawned notes moving toward the hit bar.
         * The nextJudgableNoteIndex tracks the next note for hit detection evaluation.
         */
        private List<Note> _activeNotes = new List<Note>();
        private int _nextJudgableNoteIndex = 0;

        #endregion

        #region Drumroll System

        /*
         * Drumroll State Management:
         * Drumrolls are special note sequences that require continuous input from players.
         * This system tracks the current drumroll state, timing, and player performance
         * to provide appropriate scoring and visual feedback.
         * 
         * Data Structure Design:
         * - Boolean flags for state tracking: Simple and efficient for binary states
         * - Float values for timing: Precise timing for drumroll mechanics
         * - Integer counters for performance tracking: Efficient counting without precision loss
         * - List for bridge management: Dynamic collection for visual drumroll connections
         */
        private bool _isInDrumroll = false;
        private HitObject _currentDrumroll = null;
        private float _drumrollStartTime = 0f;
        private float _drumrollEndTime = 0f;
        private int _drumrollHits = 0;
        private float _drumrollHitWindow = 0.15f;

        private List<DrumrollBridge> _activeDrumrollBridges = new List<DrumrollBridge>();

        #endregion

        #region Event System

        /*
         * Event System Architecture:
         * UnityEvents provide a decoupled communication system between GameManager and UI components.
         * This design allows UI elements to subscribe to game events without creating direct dependencies,
         * enabling modular UI development and easy testing.
         * 
         * Event Types:
         * - Score/Combo events: Integer values for discrete gameplay metrics
         * - Accuracy events: Float values for continuous performance measurement
         * - Note hit events: Note references for detailed hit information
         * - Song change events: Song and chart data for UI updates
         */
        public event UnityAction<int> OnScoreChange;
        public event UnityAction<int> OnComboChange;
        public event UnityAction<float> OnAccuracyChange;
        public event UnityAction<Note> OnNoteHit;
        public event UnityAction<SongEntry, ChartData> OnSongChanged;

        #endregion

        #region Scoring System

        /*
         * Tracks player performance through score, combo, and gauge metrics.
         * Integer types for discrete values, float for continuous measurements.
         */
        private int _score = 0;
        private int _combo = 0;
        private float _gauge;
        private float _maxGauge = 100;

        #endregion

        #region Judgement System

        /*
         * Timing windows for different hit judgements.
         * Using const values ensures consistent timing across all hit detection.
         * MISS_WINDOW: 125ms, OKAY_WINDOW: 108ms, GOOD_WINDOW: 42ms
         */
        private const float MISS_WINDOW = 0.125f;  // 125ms
        private const float OKAY_WINDOW = 0.108f;  // 108ms
        private const float GOOD_WINDOW = 0.042f;  // 42ms

        #endregion

        #region Debug and Development Tools

        /*
         * Visual feedback during development and debugging sessions.
         * Debug indicators help verify system behavior and timing accuracy.
         */
        [SerializeField]
        private GameObject _debugDrumrollIndicator;

        [SerializeField]
        private TextMeshProUGUI _debugJudgementIndicator;

        /*
         * Performance tracking counters for hit statistics throughout gameplay.
         * Using integers ensures precise counting without floating-point precision issues.
         */
        private int _hitGoods;
        private int _hitOkays;
        private int _hitBads;

        #endregion

        #region Initialization Methods

        /*
         * Establishes the singleton instance before any other systems attempt to access it.
         * Ensures the GameManager is available throughout the entire component lifecycle.
         */
        private void Awake()
        {
            Instance = this;
        }

        /*
         * Initializes all game systems after the component hierarchy is fully established.
         * Includes audio configuration, event subscriptions, and initial state setup.
         */
        private void Start()
        {
            // AudioConfiguration config = AudioSettings.GetConfiguration();
            // config.dspBufferSize = 256;
            // AudioSettings.Reset(config);

            _songStartTime = (float)AudioSettings.dspTime;
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

        /*
         * Ensures proper cleanup of event subscriptions when the component is disabled or destroyed.
         * Prevents memory leaks and invalid event calls from disabled components.
         */
        private void OnDisable()
        {
            _drum.OnHit -= RegisterHit;
        }

        #endregion

        #region Core Game Loop

        /*
         * Provides controlled access to internal components without exposing field references.
         * Maintains encapsulation while allowing external systems to query component state.
         */
        public Transform GetJudgementCircle()
        {
            return _judgementCircle;
        }

        /*
         * Main game loop that executes every frame and manages all real-time gameplay systems.
         * Includes timing synchronization, state transitions, note management, and performance optimizations.
         */
        private void Update()
        {
            _debugDrumrollIndicator.SetActive(_isInDrumroll);
            
            /*
             * DSP time synchronization maintains high-precision timing by tracking current and previous values.
             * The smoothing algorithm prevents timing jitter while maintaining audio synchronization.
             */
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

            /*
             * Core timing update retrieves synchronized time and stores it as current song time.
             * This value drives all timing-dependent systems including note spawning, hit detection, and state transitions.
             */
            float synchronizedTime = GetSynchronizedSongTime();
            _songTime = synchronizedTime;

            /*
             * State Transition Logic:
             * The system automatically transitions from delay to playing state when
             * the countdown reaches zero. This transition triggers the start of
             * normal gameplay timing and note spawning.
             */
            if (_timingState == TimingState.Delay && _songTime >= 0f)
            {
                _timingState = TimingState.Playing;
                Debug.Log($"🎵 TRANSITIONING TO PLAYING STATE at songTime={_songTime:F3}s");
                Debug.Log($"🎵 Delay start DSP: {_delayStartDspTime:F3}s, Current DSP: {AudioSettings.dspTime:F3}s");
                Debug.Log($"🎵 Time since delay start: {AudioSettings.dspTime - _delayStartDspTime:F3}s");
            }

            /*
             * Performance Optimization:
             * Note cleanup is performed periodically rather than every frame to reduce
             * processing overhead. The 15-frame interval provides a good balance between
             * responsiveness and performance.
             */
            if (Time.frameCount % 15 == 0)
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
            float effectivePreemptTime = _preemptTime + 3f;

            // Debug timing info
            if (_nextNoteIndex < _notes.Count)
            {
                float nextNoteTime = _notes[_nextNoteIndex].Time;
                float timeUntilNote = nextNoteTime - _songTime;
                if (timeUntilNote <= effectivePreemptTime + 1f) // Log when close to spawning
                {
                    Debug.Log($"Timing: songTime={_songTime:F3}s, nextNote={nextNoteTime}s, timeUntil={timeUntilNote:F3}s, preempt={_preemptTime:F3}s, effectivePreempt={effectivePreemptTime:F3}s");

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
                /*
                 * Notes are spawned based on their hit time and the effective preempt time.
                 * The effective preempt time includes the base preempt time plus the 3-second
                 * delay period, ensuring notes spawn early enough during countdown.
                 */
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

            /*
             * Miss Detection System:
             * This system processes notes that have passed their hit window without
             * being hit by the player. Missed notes are marked as hit, combo is reset,
             * and visual effects are played to provide player feedback.
             * 
             * Processing Strategy:
             * - Iterates through active notes sequentially
             * - Skips already judged notes and special note types
             * - Uses smoothed timing for consistent miss detection
             * - Handles note destruction and list cleanup safely
             */
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

                if (note.IsHit) // Already judged
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }

                // Skip DrumrollBalloonEnd notes - they don't need to be judged
                if (note.NoteType == NoteType.DrumrollBalloonEnd)
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }

                float currentTime = GetSmoothedSongTime();
                if (currentTime > note.HitTime + MISS_WINDOW)
                {
                    note.IsHit = true;
                    
                    /*
                     * Miss Effect Processing:
                     * When a note is missed, the combo is reset and visual effects
                     * are played to provide immediate feedback to the player.
                     * The system safely handles cases where notes may be destroyed
                     * during processing.
                     */
                    _combo = 0;
                    OnComboChange?.Invoke(_combo);

                    // Check if note still exists before accessing transform
                    if (note != null && note.gameObject != null)
                    {
                        PlayMissEffect(note.transform.position);
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

            /*
             * Drumroll End Detection:
             * Monitors the current drumroll state and automatically ends it when
             * the drumroll duration expires. This ensures proper state management
             * and prevents drumrolls from continuing indefinitely.
             */
            if (_isInDrumroll)
            {
                float currentTime = GetSmoothedSongTime();
                if (currentTime > _drumrollEndTime)
                {
                    EndDrumroll();
                }
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Removes destroyed and invalid notes from the active notes list to maintain system integrity.
        /// </summary>
        /// <remarks>
        /// This method performs periodic cleanup to prevent null reference exceptions and
        /// maintain optimal performance. It adjusts the next judgable note index to ensure
        /// proper note processing order.
        /// </remarks>
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

        /// <summary>
        /// Validates that a note object is in a valid state for processing.
        /// </summary>
        /// <param name="note">The note object to validate.</param>
        /// <returns>True if the note is valid and can be processed, false otherwise.</returns>
        /// <remarks>
        /// This method checks for null references, destroyed GameObjects, and disabled
        /// components to prevent runtime errors during note processing.
        /// </remarks>
        private bool IsNoteValid(Note note)
        {
            return note != null &&
                   note.gameObject != null &&
                   note.enabled &&
                   note.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// Forces immediate cleanup of all invalid notes in the system.
        /// </summary>
        /// <remarks>
        /// This method is primarily used for debugging and development purposes.
        /// It performs a comprehensive cleanup and logs the results for analysis.
        /// Available through the Unity context menu for easy access during development.
        /// </remarks>
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

        /// <summary>
        /// Performs a comprehensive health check of the note processing system.
        /// </summary>
        /// <remarks>
        /// This diagnostic method outputs detailed information about the current state
        /// of the note system, including counts, indices, and timing information.
        /// Available through the Unity context menu for development and debugging.
        /// </remarks>
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
                else if (note.IsHit)
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

        /// <summary>
        /// Creates and initializes a new note GameObject from chart data.
        /// </summary>
        /// <param name="noteData">The chart data containing note timing and type information.</param>
        /// <remarks>
        /// This method handles the complete note spawning process including instantiation,
        /// initialization, positioning, and special drumroll bridge creation for drumroll notes.
        /// Notes are added to the active notes list for ongoing processing and management.
        /// </remarks>
        private void SpawnNote(HitObject noteData)
        {
            GameObject prefabToSpawn = _notePrefab;
            var obj = Instantiate(prefabToSpawn);
            var note = obj.GetComponent<Note>();

            // Use original preempt time for note travel speed (2.0s), but notes spawn early due to effectivePreemptTime
            note.Initialize(noteData.Time, _preemptTime, _travelDistance, noteData.ScrollSpeed, noteData);
            note.JudgementCirclePosition = _judgementCircle.position;
            _activeNotes.Add(note);

            /*
             * Drumroll Bridge Creation:
             * Drumroll notes require visual bridges to connect start and end points.
             * This system automatically creates and manages bridge objects for proper
             * visual representation of drumroll sequences.
             */
            if (noteData.Type == NoteType.Drumroll || noteData.Type == NoteType.DrumrollBig)
            {
                // Spawn bridge and assign head
                var bridgeObj = Instantiate(_drumrollBridgePrefab);
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
                        float diff = Mathf.Abs(note.HitTime - bridge.headNote.HitTime);
                        if (diff < minTimeDiff)
                        {
                            minTimeDiff = diff;
                            closestBridge = bridge;
                        }
                    }
                }
                if (closestBridge != null)
                {
                    closestBridge.headNote.DrumrollEndNote = note;
                    closestBridge.endNote = note;
                }
            }
        }

        /// <summary>
        /// Resets the game state to initial values for a new game session.
        /// </summary>
        /// <remarks>
        /// This method performs complete game initialization including chart loading,
        /// audio preparation, and system state setup. It marks the song as manually
        /// selected to prevent automatic song selection from interfering with gameplay.
        /// </remarks>
        private void InitializeGameState()
        {
            _score = 0;
            _combo = 0;
            OnScoreChange?.Invoke(_score);
            OnComboChange?.Invoke(_combo);
            // Note: Audio is now handled by the chart system, not here
        }

        /// <summary>
        /// Returns the current synchronized song time with smoothing applied.
        /// </summary>
        /// <returns>The current synchronized song time in seconds.</returns>
        /// <remarks>
        /// This method provides a smoothed version of the synchronized song time
        /// to reduce timing jitter and provide consistent gameplay timing.
        /// </remarks>
        public float GetSmoothedSongTime()
        {
            // Use synchronized time from audio source for more accurate timing
            return GetSynchronizedSongTime();
        }

        #endregion

        #region Chart System Methods

        /*
         * Chart System Methods:
         * These methods handle the loading and management of chart data,
         * including song selection, chart loading, and game initialization.
         */

        /// <summary>
        /// Initializes a new game session with the specified song and chart.
        /// </summary>
        /// <param name="song">The song entry containing metadata and audio information.</param>
        /// <param name="chart">The chart data containing gameplay timing and note information.</param>
        /// <remarks>
        /// This method performs complete game initialization including chart loading,
        /// audio preparation, and system state setup. It marks the song as manually
        /// selected to prevent automatic song selection from interfering with gameplay.
        /// </remarks>
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

        /// <summary>
        /// Loads chart data without audio for testing and development purposes.
        /// </summary>
        /// <param name="chart">The chart data to load for gameplay.</param>
        /// <remarks>
        /// This method is primarily used for testing chart functionality without
        /// requiring audio files. It maintains the manual selection flag to prevent
        /// automatic song selection interference.
        /// </remarks>
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

        /// <summary>
        /// Loads audio from a song entry for playback.
        /// </summary>
        /// <param name="song">The song entry containing the audio file information.</param>
        /// <remarks>
        /// This method initiates the audio loading process asynchronously using
        /// a coroutine. It provides debug logging for development and testing
        /// purposes to track audio file loading status.
        /// </remarks>
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

        /// <summary>
        /// Returns the current status of the audio system.
        /// </summary>
        /// <returns>A string describing the current audio system state.</returns>
        /// <remarks>
        /// This method provides human-readable status information for debugging
        /// and development purposes, helping developers understand the current
        /// state of audio loading and playback.
        /// </remarks>
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

        /// <summary>
        /// Returns detailed information about audio file paths and loading status.
        /// </summary>
        /// <returns>A string containing audio path and file information.</returns>
        /// <remarks>
        /// This method provides comprehensive audio system information for debugging
        /// file loading issues and path-related problems during development.
        /// </remarks>
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

        #endregion

        #region Debugging and Development Tools

        /// <summary>
        /// Manually loads audio from a hardcoded test path for development purposes.
        /// </summary>
        /// <remarks>
        /// This method is used for testing audio loading functionality without requiring
        /// a complete song selection. The test path should be modified to point to
        /// an actual audio file during development. Available through the Unity context menu.
        /// </remarks>
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

        /// <summary>
        /// Tests audio loading from the currently selected song for debugging purposes.
        /// </summary>
        /// <remarks>
        /// This method attempts to load and play audio from the current song entry,
        /// providing immediate feedback for audio system testing. Available through
        /// the Unity context menu for development and debugging.
        /// </remarks>
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

        /// <summary>
        /// Tests audio loading from a specific hardcoded path for development purposes.
        /// </summary>
        /// <remarks>
        /// This method loads audio from a predefined test path to verify audio system
        /// functionality. The path should be updated to point to a valid audio file
        /// in the project directory. Available through the Unity context menu.
        /// </remarks>
        [ContextMenu("Test Load Audio From Specific Path")]
        public void TestLoadAudioFromSpecificPath()
        {
            // You can modify this path to test with a known audio file
            string testPath = @"X:\Projects\AshTaiko\Part B\AshTaiko\Assets\_AshTaiko\Assets\01. St. Chroma.mp3";
            Debug.Log($"Testing audio load from specific path: {testPath}");
            StartCoroutine(LoadAndPlayAudio(testPath));
        }

        /// <summary>
        /// Tests the chart system by loading the first available song for development.
        /// </summary>
        /// <remarks>
        /// This method provides a quick way to test chart loading and gameplay
        /// systems without requiring manual song selection. Available through
        /// the Unity context menu for development and testing purposes.
        /// </remarks>
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

        #endregion

        #region Auto-Start System

        /// <summary>
        /// Automatically starts the game with the first available chart after initialization.
        /// </summary>
        /// <returns>An IEnumerator for coroutine execution.</returns>
        /// <remarks>
        /// This coroutine implements the auto-start functionality for development and
        /// testing purposes. It waits for system initialization, checks for manual
        /// song selection, and automatically loads the first available chart with
        /// preference for Normal difficulty. The method respects manual selection
        /// flags to prevent interference with user choices.
        /// </remarks>
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

        /// <summary>
        /// Manually triggers the auto-start system for testing purposes.
        /// </summary>
        /// <remarks>
        /// This method provides a way to test the auto-start functionality without
        /// waiting for scene initialization. Available through the Unity context menu
        /// for development and testing purposes.
        /// </remarks>
        [ContextMenu("Trigger Auto-Start")]
        public void TriggerAutoStart()
        {
            Debug.Log("Manually triggering auto-start...");
            StartCoroutine(AutoStartWithFirstChart());
        }

        /// <summary>
        /// Enables automatic game start behavior for the next scene load.
        /// </summary>
        /// <remarks>
        /// This method resets the manual selection flag, allowing the auto-start
        /// system to automatically begin games when scenes are loaded. Available
        /// through the Unity context menu for development and testing.
        /// </remarks>
        [ContextMenu("Enable Auto-Start")]
        public void EnableAutoStart()
        {
            Debug.Log("Auto-start enabled - will start automatically on next scene load");
            _songManuallySelected = false;
        }

        /// <summary>
        /// Disables automatic game start behavior, requiring manual song selection.
        /// </summary>
        /// <remarks>
        /// This method sets the manual selection flag, preventing the auto-start
        /// system from interfering with user choices. Available through the Unity
        /// context menu for development and testing.
        /// </remarks>
        [ContextMenu("Disable Auto-Start")]
        public void DisableAutoStart()
        {
            Debug.Log("Auto-start disabled - use SongSelectionEditor to start games");
            _songManuallySelected = true;
        }

        #endregion

        #region System Management Methods

        /// <summary>
        /// Loads chart data and initializes the note system for gameplay.
        /// </summary>
        /// <param name="chart">The chart data to load.</param>
        /// <remarks>
        /// This method prepares the game system for a new chart by clearing
        /// existing notes, sorting new notes by time, and initializing
        /// timing and state variables. It ensures clean state transitions
        /// between different charts.
        /// </remarks>
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
                _songEndTime = chart.TotalLength;
            }

            // Initialize timing for the new chart
            _songTime = -3f; // Start in delay period

            Debug.Log($"Chart loaded: {chart.HitObjects.Count} notes, first at {(chart.HitObjects.Count > 0 ? chart.HitObjects[0].Time : 0)}s");
            Debug.Log($"Timing initialized: songTime={_songTime}s, will start spawning notes at 0s");
        }

        /// <summary>
        /// Asynchronously loads and schedules audio playback with proper timing synchronization.
        /// </summary>
        /// <param name="audioPath">The file path to the audio file to load.</param>
        /// <returns>An IEnumerator for coroutine execution.</returns>
        /// <remarks>
        /// This coroutine handles the complete audio loading process including file validation,
        /// UnityWebRequest loading, and timing system initialization. It implements a 3-second
        /// delay system to ensure proper synchronization between visual and audio systems.
        /// The method uses modern Unity audio loading techniques for compatibility and performance.
        /// </remarks>
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

        /// <summary>
        /// Resets all game state variables to their initial values for a new game session.
        /// </summary>
        /// <remarks>
        /// This method performs a complete reset of the game state including scoring,
        /// combo, gauge, timing, and note systems. It ensures clean state transitions
        /// between different game sessions and prevents state contamination.
        /// </remarks>
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

        #endregion

        #region Public Access Methods

        /// <summary>
        /// Returns the currently selected song entry.
        /// </summary>
        /// <returns>The current song entry or null if none is selected.</returns>
        /// <remarks>
        /// This method provides access to the current song metadata for UI display
        /// and other systems that need song information.
        /// </remarks>
        public SongEntry GetCurrentSong()
        {
            return _currentSong;
        }

        /// <summary>
        /// Returns the currently selected chart data.
        /// </summary>
        /// <returns>The current chart data or null if none is selected.</returns>
        /// <remarks>
        /// This method provides access to the current chart data for UI display
        /// and other systems that need chart information.
        /// </remarks>
        public ChartData GetCurrentChart()
        {
            return _currentChart;
        }

        /// <summary>
        /// Checks if manual song selection is currently active.
        /// </summary>
        /// <returns>True if manual selection is active, false if auto-start is enabled.</returns>
        /// <remarks>
        /// This method provides information about the current selection mode
        /// for UI systems and other components that need to know the selection state.
        /// </remarks>
        public bool IsManualSelectionActive()
        {
            return _songManuallySelected;
        }

        /// <summary>
        /// Returns formatted information about the current song selection for UI display.
        /// </summary>
        /// <returns>A formatted string containing song title, chart version, difficulty, and selection type.</returns>
        /// <remarks>
        /// This method provides user-friendly information about the current selection
        /// including whether it was manually selected or automatically started.
        /// </remarks>
        public string GetCurrentSongInfo()
        {
            if (_currentSong == null || _currentChart == null)
            {
                return "No song selected";
            }

            string selectionType = _songManuallySelected ? "Manual Selection" : "Auto-Start";
            return $"{_currentSong.Title} - {_currentChart.Version} ({_currentChart.Difficulty}) [{selectionType}]";
        }

        /// <summary>
        /// Checks if the game system is ready to begin gameplay.
        /// </summary>
        /// <returns>True if all required systems are initialized and ready, false otherwise.</returns>
        /// <remarks>
        /// This method evaluates the readiness of all game systems including
        /// song selection, chart loading, and audio preparation.
        /// </remarks>
        public bool IsGameReadyToPlay()
        {
            return _currentSong != null && _currentChart != null && _songManager?.GetCurrentAudioClip() != null;
        }

        /// <summary>
        /// Returns a string describing the current game readiness status.
        /// </summary>
        /// <returns>A human-readable string describing the current game state.</returns>
        /// <remarks>
        /// This method provides user-friendly status information for UI display,
        /// helping players understand when the game is ready to play.
        /// </remarks>
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

        /// <summary>
        /// Sets the selected chart for preview purposes without starting the game.
        /// </summary>
        /// <param name="chart">The chart data to select for preview.</param>
        /// <remarks>
        /// This method is primarily used for editor preview functionality where
        /// developers want to examine chart data without initiating gameplay.
        /// It updates the current selection and loads chart data but does not
        /// start audio playback or game systems.
        /// </remarks>
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
                        break;
                    }
                }
            }
        }

        #endregion

        #region Hit Detection System

        /// <summary>
        /// Processes player input hits and determines note judgements.
        /// </summary>
        /// <param name="hitType">The type of input hit detected by the drum system.</param>
        /// <remarks>
        /// This method handles the core hit detection logic including drumroll processing,
        /// note validation, timing windows, and judgement determination. It processes
        /// notes sequentially and updates game state based on hit accuracy.
        /// </remarks>
        private void RegisterHit(HitType hitType)
        {
            float currentTime = GetSmoothedSongTime();
            float hitWindow = MISS_WINDOW; // Use miss window as the maximum hit window

            /*
             * Drumroll Hit Processing:
             * If currently in a drumroll, handle the hit according to drumroll rules
             * before processing regular note hits. This ensures proper drumroll scoring
             * and state management.
             */
            if (_isInDrumroll)
            {
                HandleDrumrollHit(hitType, currentTime);
                // Don't return here - continue to process regular notes
            }

            /*
             * Regular Note Hit Detection:
             * Process the next judgable note to determine if the input hit
             * corresponds to a valid note within the hit window.
             */
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

                if (note.IsHit)
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }

                // Skip DrumrollBalloonEnd notes - they don't need to be judged by input
                if (note.NoteType == NoteType.DrumrollBalloonEnd)
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }

                // If the note is too far in the future, ignore input
                if (currentTime < note.HitTime - hitWindow)
                {
                    break;
                }

                // If the note is within the hit window
                if (Mathf.Abs(currentTime - note.HitTime) <= hitWindow)
                {
                    // Only allow correct input type to hit the note
                    if (IsHitTypeMatchingNoteType(hitType, note.HitObject.Type))
                    {
                        // Determine judgement based on timing
                        Judgement judgement = GetJudgement(Mathf.Abs(currentTime - note.HitTime));
                        Debug.Log($"Hit! Judgement: {judgement}");

                        note.IsHit = true;

                        // --- Check if this starts a drumroll ---
                        if (note.HitObject.Type == NoteType.Drumroll)
                        {
                            StartDrumroll(note.HitObject, currentTime);
                        }

                        // --- Apply judgement effects ---
                        ApplyJudgementEffects(judgement, note);

                        // --- Special handling for drumroll head notes ---
                        if (note.NoteType == NoteType.Drumroll || note.NoteType == NoteType.DrumrollBig)
                        {
                            // Don't destroy drumroll head notes - they stay until the end note is hit
                            _nextJudgableNoteIndex++;
                        }
                        else if (note.NoteType == NoteType.DrumrollBalloonEnd)
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
                    note.IsHit = true;
                    // --- Miss Effect ---
                    ApplyJudgementEffects(Judgement.Miss, note);

                    // --- Special handling for drumroll head notes ---
                    if (note.NoteType == NoteType.Drumroll || note.NoteType == NoteType.DrumrollBig)
                    {
                        // Don't destroy drumroll head notes - they stay until the end note is hit
                        _nextJudgableNoteIndex++;
                    }
                    else if (note.NoteType == NoteType.DrumrollBalloonEnd)
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

        #endregion

        #region Drumroll System

        /*
         * Drumroll System Methods:
         * These methods manage the complex drumroll gameplay mechanics including
         * start detection, hit processing, and end conditions. Drumrolls provide
         * continuous gameplay challenges and bonus scoring opportunities.
         */

        /// <summary>
        /// Initializes a new drumroll sequence when a drumroll start note is hit.
        /// </summary>
        /// <param name="drumrollStart">The HitObject that initiated the drumroll.</param>
        /// <param name="currentTime">The current game time when the drumroll started.</param>
        /// <remarks>
        /// This method sets up the drumroll state, searches for the corresponding
        /// end note, and initializes all drumroll-related variables for proper
        /// gameplay tracking and scoring.
        /// </remarks>
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

        /// <summary>
        /// Processes hits during an active drumroll sequence.
        /// </summary>
        /// <param name="hitType">The type of input hit detected.</param>
        /// <param name="currentTime">The current game time.</param>
        /// <remarks>
        /// This method handles continuous input during drumrolls, awarding points
        /// for successful hits and maintaining combo progression. It automatically
        /// ends the drumroll when the time limit is reached.
        /// </remarks>
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

        /// <summary>
        /// Finalizes the drumroll sequence and awards bonus points.
        /// </summary>
        /// <remarks>
        /// This method calculates bonus points based on drumroll performance,
        /// resets all drumroll state variables, and updates the score display.
        /// The bonus calculation rewards players for maintaining consistent
        /// timing throughout the drumroll sequence.
        /// </remarks>
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

        /// <summary>
        /// Determines if a player input type matches a note type for hit validation.
        /// </summary>
        /// <param name="hitType">The type of input hit detected by the drum system.</param>
        /// <param name="noteType">The type of note being evaluated.</param>
        /// <returns>True if the input type is valid for the note type, false otherwise.</returns>
        /// <remarks>
        /// This method implements the core input validation logic for the game.
        /// During drumrolls, both Don and Ka inputs are accepted to provide
        /// flexible gameplay while maintaining challenge.
        /// </remarks>
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

        /// <summary>
        /// Destroys a drumroll head note and its associated bridge when the end note is processed.
        /// </summary>
        /// <param name="endNote">The drumroll end note that triggered the cleanup.</param>
        /// <remarks>
        /// This method ensures proper cleanup of drumroll visual elements when the
        /// sequence is completed. It searches for the corresponding bridge and
        /// destroys both the head note and bridge GameObject to prevent memory leaks.
        /// </remarks>
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

        #endregion

        #region Judgement System

        /*
         * Judgement System Methods:
         * These methods handle the core scoring and feedback system based on
         * player timing accuracy. The system provides immediate feedback and
         * maintains scoring consistency throughout gameplay.
         */

        /// <summary>
        /// Determines the judgement quality based on timing accuracy.
        /// </summary>
        /// <param name="timeDifference">The absolute time difference between hit time and actual hit.</param>
        /// <returns>The judgement quality (Good, Okay, or Miss) based on timing windows.</returns>
        /// <remarks>
        /// This method implements the core timing judgement logic using predefined
        /// timing windows. The windows are designed to provide fair and consistent
        /// scoring while maintaining gameplay challenge.
        /// </remarks>
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

        /// <summary>
        /// Applies the effects of a judgement to the game state and player feedback.
        /// </summary>
        /// <param name="judgement">The judgement quality determined by timing accuracy.</param>
        /// <param name="note">The note that was judged.</param>
        /// <remarks>
        /// This method handles all judgement consequences including score updates,
        /// combo progression, gauge changes, visual feedback, and event notifications.
        /// It ensures consistent application of judgement effects across all note types.
        /// </remarks>
        private void ApplyJudgementEffects(Judgement judgement, Note note)
        {
            switch (judgement)
            {
                case Judgement.Good:
                    _hitGoods++;
                    _debugJudgementIndicator.text = "Good!";
                    _combo++;
                    _score += 100; // Base score for Good
                    OnComboChange?.Invoke(_combo);
                    OnNoteHit?.Invoke(note);
                    PlayHitEffect(note.transform.position);
                    Debug.Log("Good!");
                    AddGauge(2.5f); // Increased from 0.25f for more responsive gauge
                    break;

                case Judgement.Okay:
                    _debugJudgementIndicator.text = "Okay!";
                    _hitOkays++;
                    _combo++;
                    _score += 50; // Lower score for Okay
                    OnComboChange?.Invoke(_combo);
                    OnNoteHit?.Invoke(note);
                    PlayHitEffect(note.transform.position);
                    Debug.Log("Okay!");
                    AddGauge(1.25f); // Increased from 0.125f for more responsive gauge
                    break;

                case Judgement.Miss:
                    _hitBads++;
                    _debugJudgementIndicator.text = "Bad";
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

        /// <summary>
        /// Represents the quality of a player's timing accuracy.
        /// </summary>
        /// <remarks>
        /// The judgement system provides three levels of accuracy feedback.
        /// Good represents excellent timing, Okay represents acceptable timing,
        /// and Miss represents poor timing or missed notes.
        /// </remarks>
        public enum Judgement
        {
            Miss,
            Okay,
            Good
        }

        /// <summary>
        /// Modifies the player's gauge value based on performance.
        /// </summary>
        /// <param name="amount">The amount to add to the current gauge value.</param>
        /// <remarks>
        /// This method updates the gauge system which provides visual feedback
        /// on overall performance. The gauge is clamped between 0 and maximum
        /// values to maintain consistent visual representation.
        /// </remarks>
        private void AddGauge(float amount)
        {
            float oldGauge = _gauge;
            _gauge += amount;
            _gauge = Mathf.Clamp(_gauge, 0, _maxGauge);

            Debug.Log($"Gauge: {oldGauge:F2} + {amount:F2} = {_gauge:F2} ({(_gauge / _maxGauge * 100):F1}%)");
        }

        /// <summary>
        /// Calculates the player's overall accuracy using the osu! Taiko formula.
        /// </summary>
        /// <returns>A percentage value representing overall accuracy.</returns>
        /// <remarks>
        /// The accuracy calculation uses the standard osu! Taiko formula which
        /// weights different judgement types appropriately for fair performance
        /// measurement across various skill levels.
        /// </remarks>
        private float GetAccuracy()
        {
            // uses the osu taiko formula
            float nominator = _hitGoods + _hitOkays * 0.5f;
            float sum = _hitGoods + _hitOkays + _hitBads;
            return nominator / sum * 100;
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// Plays visual and audio effects for successful note hits.
        /// </summary>
        /// <param name="position">The world position where the effect should appear.</param>
        /// <remarks>
        /// This method instantiates hit effect prefabs at the specified position
        /// to provide immediate visual feedback to the player. The effects are
        /// positioned at the judgement circle for consistent visual placement.
        /// </remarks>
        private void PlayHitEffect(Vector3 position)
        {
            // TODO: Instantiate hit effect prefab or play a sound at the given position
            Instantiate(_hitEffect, _judgementCircle.position, Quaternion.identity);
            Debug.Log("Play hit effect at: " + position);
        }

        /// <summary>
        /// Plays visual and audio effects for missed notes.
        /// </summary>
        /// <param name="position">The world position where the effect should appear.</param>
        /// <remarks>
        /// This method provides visual feedback when notes are missed, helping
        /// players understand their performance and timing accuracy.
        /// </remarks>
        private void PlayMissEffect(Vector3 position)
        {
            // TODO: Instantiate miss effect prefab or play a sound at the given position
            Debug.Log("Play miss effect at: " + position);
        }

        #endregion

        #region UI and Information Methods

        /// <summary>
        /// Calculates the current playback progress as a percentage of total song length.
        /// </summary>
        /// <returns>A value between 0.0 and 1.0 representing the current playback progress.</returns>
        /// <remarks>
        /// This method provides progress information for UI elements like progress bars.
        /// It safely handles cases where the audio system may not be ready or the
        /// song length is invalid, returning 0.0 in such cases.
        /// </remarks>
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

        /// <summary>
        /// Returns a formatted string showing current playback position and total song length.
        /// </summary>
        /// <returns>A formatted timestamp string in "MM:SS / MM:SS" format.</returns>
        /// <remarks>
        /// This method provides user-friendly time display for UI elements. It safely
        /// handles cases where the audio system may not be ready, returning a default
        /// format string in such cases.
        /// </remarks>
        public string GetSongTimestamp()
        {
            if (_songManager == null || !_songManager.IsAudioReady())
            {
                return "00:00 / 00:00";
            }
            return _songManager.GetSongPositionString() + " / " + _songManager.GetSongLengthString();
        }

        /// <summary>
        /// Returns the current gauge value as a percentage of the maximum gauge.
        /// </summary>
        /// <returns>A value between 0.0 and 1.0 representing the current gauge percentage.</returns>
        /// <remarks>
        /// This method provides normalized gauge values for UI display and other
        /// systems that require percentage-based gauge representation.
        /// </remarks>
        public float GetGaugePercentage()
        {
            return _gauge / _maxGauge;
        }

        /// <summary>
        /// Checks if the song progress system is ready to provide timing information.
        /// </summary>
        /// <returns>True if the audio system is initialized and ready, false otherwise.</returns>
        /// <remarks>
        /// This method provides a safe way to check system readiness before
        /// attempting to access song progress or timing information.
        /// </remarks>
        public bool IsSongProgressReady()
        {
            return _songManager != null && _songManager.IsAudioReady();
        }

        #endregion
    }

    [Serializable]
    public class HitObject
    {
        public HitObject(float time, float scrollSpeed, NoteType type)
        {
            Time = time;
            ScrollSpeed = scrollSpeed;
            Type = type;
        }
        public float Time; // in seconds
        public float ScrollSpeed = 1f; // multiplier relative to base speed
        public NoteType Type;   // "don", "ka", "drumroll", etc. 
        // Drumrolls are now defined by start and end notes, so no Duration here
    }
}
