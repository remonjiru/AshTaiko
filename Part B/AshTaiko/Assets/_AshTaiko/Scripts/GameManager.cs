using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace AshTaiko
{
    /// <summary>
    /// Central game controller managing timing, note spawning, hit detection, scoring, and gameplay loop.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton and Core References

        /*
            The singleton pattern provides global access to game state from any component without requiring
            explicit references to be passed around. This simplifies component communication but creates
            tight coupling that makes testing and modularity difficult.
            
            A future refactor would probably involve changing the singleton pattern
            to something like dependency injection for managing component references.
        */

        /// <summary>
        /// Global singleton instance providing access to game state from anywhere in the codebase.
        /// </summary>
        public static GameManager Instance { get; private set; }

        #endregion

        #region Prefab and Component References

        /*
            Note prefabs are instantiated at runtime to represent individual chart notes. Using prefabs
            allows for efficient object pooling and consistent visual representation across all notes.
            The drumroll bridge prefab creates visual connections between drumroll start and end notes.
        */

        [Header("Note Prefabs")]
        // These are assigned in the inspector.

        [SerializeField]
        private GameObject _notePrefab;

        [SerializeField]
        private GameObject _drumrollBridgePrefab;

        /*
            SongManager handles audio playback and provides timing information. This separation of concerns
            allows the GameManager to focus on gameplay logic while delegating audio management to specialists.
            The relationship is loosely coupled through interfaces and events.
        */

        /// <summary>
        /// Manages audio playback and provides synchronized timing information for gameplay.
        /// </summary>
        [SerializeField]
        private SongManager _songManager;

        /*
            Drum component handles player input detection and converts raw input into game events.
            This separation allows for easy input system swapping (keyboard, controller, touch) without
            affecting core gameplay logic. This will be implemented later, as time constraints have prevented
            the implementation of controller input.
        */

        [SerializeField]
        private Drum _drum;

        /*
            Transform components provide world space positions for note spawning and hit detection.
            Using Transform instead of Vector3 allows for runtime position updates and makes it
            easier to visualise in the Unity Editor.
        */
        /// <summary>
        /// The transform of the judgement circle position.
        /// </summary>
        [SerializeField]
        private Transform _judgementCircle;

        /*
            Hit effects are instantiated at the judgement circle to provide player feedback.
            GameObject references allow for runtime instantiation and destruction of effect instances.
        */

        [SerializeField]
        private GameObject _hitEffect;

        /*
            Background system provides immersive gameplay experience with chart-specific backgrounds.
            The background image is loaded from the chart data and can be dimmed for better gameplay visibility.
        */

        [Header("Background System")]
        [SerializeField] private UnityEngine.UI.Image _backgroundImage;
        [SerializeField] private UnityEngine.UI.Image _backgroundOverlay; // For dimming effect
        [SerializeField] private float _backgroundDim = 0.7f; // Adjustable dim level (0 = no dim, 1 = black)
        [SerializeField] private bool _enableBackground = true;

        [Header("Input and Menu System")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private PauseMenuManager _pauseMenuManager;


        #endregion

        #region Note Management System

        /// <summary>
        /// The notes list stores all chart data as HitObject instances for note spawning.
        /// </summary>
        /// Using List<T> allows for dynamic chart loading and runtime modification.
        private List<HitObject> _notes = new List<HitObject>();

        //  The nextNoteIndex tracks the current position in the chart for sequential spawning.
        private int _nextNoteIndex = 0;

        #endregion

        #region Chart and Song Data

        /// <summary>
        /// Metadata about the current song (title, artist, audio file).
        /// </summary>
        private SongEntry _currentSong;

        /// <summary>
        /// Gameplay data including note timings, difficulty settings, etc.
        /// </summary>
        private ChartData _currentChart;

        // This separation allows for multiple difficulty charts per song while sharing common metadata.

        #endregion

        #region Timing System

        // Using float values provides sufficient precision for Unity compatibility.

        /// <summary>
        /// Determines how early notes spawn before their hit time in seconds.
        /// </summary>
        private float _preemptTime = 2.0f;

        /// <summary>
        /// Defines the spawn-to-hit distance for consistent note movement speeds.
        /// </summary>
        [SerializeField]
        private float _travelDistance = 15f;

        /// <summary>
        /// Represents the current playback position in the chart, synchronized with audio playback.
        /// </summary>
        /// <remarks>
        /// This value drives all timing-dependent systems including note spawning, hit detection, and scoring.
        /// </remarks>
        private float _songTime;

        /*
            TimingState enum implements a state machine pattern to manage audio loading and synchronization.
            Flow follows: Uninitialized -> Loading -> Delay -> Playing. 
            The Delay state represents a variable countdown before audio starts.
        */
        private enum TimingState
        {
            Uninitialized,
            Loading,
            Delay,
            Playing,
            Completed
        }

        private TimingState _timingState = TimingState.Uninitialized;

        /*
            DSP time provides high-precision timing for audio synchronization.
            Using double precision prevents timing drift during long audio sessions.
            The delay start time enables continuous timing through the delay period.
        */
        private double _delayStartDspTime = 0.0;

        /*
            This flag prevents automatic song selection from overriding user choices during gameplay.
            Boolean type provides simple state tracking with minimal memory overhead.
        */
        private bool _songManuallySelected = false;

        #endregion

        #region Core Properties

        /*
            SongTime provides read-only access to the current synchronized playback time.
            Using a property instead of a public field ensures encapsulation and allows for
            future validation or side effects without breaking external code.
        */

        /// <summary>
        /// Represents the current playback position in the chart, synchronized with audio playback.
        /// </summary>
        /// <remarks>
        /// This value drives all timing-dependent systems including note spawning, hit detection, and scoring.
        /// </remarks>
        public float SongTime
        {
            get => _songTime;
        }

        /// <summary>
        /// Gets whether the game is currently paused.
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
        }

        /// <summary>
        /// Sets the pause state of the game.
        /// </summary>
        /// <param name="paused">True to pause the game, false to resume.</param>
        public void SetPauseState(bool paused)
        {
            _isPaused = paused;
        }

        /// <summary>
        /// Restarts the current song/chart from the beginning.
        /// Resets all game state and begins playback from the start.
        /// </summary>
        public void RestartCurrentSong()
        {
            if (_currentSong == null || _currentChart == null)
            {
                Debug.LogWarning("Cannot restart: No song or chart loaded");
                return;
            }

            Debug.Log("RESTARTING");
            Debug.Log($"Song: {_currentSong.Title} - {_currentChart.Version}");
            
            // Stop current audio playback first
            if (_songManager != null)
            {
                _songManager.StopAudio();
                _songManager.ResetPauseTracking();
            }
            
            // Reset game state
            ResetGameState();
            
            // Reload the chart data
            LoadChart(_currentChart);
            
            // Start the song from the beginning
            if (_songManager != null)
            {
                _songManager.PlayInSeconds(3.0); // 3 second delay like initial start
            }
            
            Debug.Log("Song restarted successfully");
        }

        #endregion

        #region Timing Methods

        /// <summary>
        /// Provides core timing synchronization between visual and audio systems, handles timing calculations during the delay period 
        /// and maintains continuous timing. During delay, time counts from a negative value to to 0.0s, then continues normally.
        /// </summary>
        public float GetSynchronizedSongTime()
        {
            // If game is paused, return the last known song time without advancing
            if (_isPaused)
            {
                return _songTime;
            }

            // Don't try to get audio time if we're not in the right state otherwise Unity will complain
            if (_timingState == TimingState.Uninitialized || _timingState == TimingState.Loading)
            {
                return _songTime; // Return current song time until system is ready
            }

            // Get the actual audio playback time from SongManager
            if (_songManager != null)
            {
                // something is telling me to put a switch statement here but its literally one check so it should be okay LMAO

                if (_timingState == TimingState.Delay)
                {
                    // Calculate delay time accounting for paused time
                    double currentDspTime = AudioSettings.dspTime;
                    double timeSinceDelayStart = currentDspTime - _delayStartDspTime;
                    
                    // Subtract the total paused time to keep delay timing in sync
                    double correctedTimeSinceDelayStart = timeSinceDelayStart - _songManager.GetTotalPausedTime();

                    // Hardcoded 3 second countdown oopsie sorry mr toet
                    float synchronizedTime = -3f + (float)correctedTimeSinceDelayStart;

                    return synchronizedTime;
                }
                else if (_timingState == TimingState.Playing)
                {
                    // Use the corrected song time from SongManager that accounts for paused time
                    return (float)_songManager.GetCorrectedSongTime();
                }
            }

            return _songTime;
        }

        /// <summary>
        /// Converts internal synchronized time to positive display time for UI elements.
        /// </summary>
        //  Ensures UI never shows negative values during the delay period.
        public float GetDisplaySongTime()
        {
            float synchronizedTime = GetSynchronizedSongTime();
            return Mathf.Max(0f, synchronizedTime);
        }

        #endregion

        #region Game State Fields

        /*
            DSP time smoothing mechanism prevents jitter and provides stable timing.
            The smoothing algorithm interpolates between DSP time updates for consistent frame rates.
            Shoutout to that one guy on the Unity forums for this solution.
        */
        private float _smoothedDsp;
        private double _lastDsp;
        
        /// <summary>
        /// Indicates whether the game is currently paused.
        /// </summary>
        private bool _isPaused = false;

        /*
            Song duration tracking for progress calculations and UI display.
            Using float values provides sufficient precision for song duration.
        */
        private float _songStartTime = 0;
        private float _songEndTime = 0;

        /*
            Frame timing system tracks timing information for each frame.
            Uses both DSP time and Unity time for maximum accuracy.
        */
        private double _lastDspTime;
        private double _currentDspTime;
        private float _frameStartTime;

        /// <summary>
        /// List containing all currently spawned notes moving toward the hit bar.
        /// </summary>
        private List<Note> _activeNotes = new List<Note>();

        /// <summary>
        /// Index tracking the next note for hit detection evaluation.
        /// </summary>
        private int _nextJudgableNoteIndex = 0;

        #endregion

        #region Drumroll System

        /*
            Drumrolls are special note sequences that require continuous input from players.
            These sucked to implement
            This system tracks the current drumroll state, timing, and player performance to 
            provide appropriate scoring and visual feedback
        
            Boolean flags for state tracking provide simple and efficient binary states
            Float values provide precise timing for drumroll mechanics and align with the SongTime
            Integer because hits are discrete
            List for "bridge" management provides dynamic collection for visual drumroll connections
        */
        private bool _isInDrumroll = false;
        private HitObject _currentDrumroll = null;
        private float _drumrollStartTime = 0f;
        private float _drumrollEndTime = 0f;
        private int _drumrollHits = 0;

        private List<DrumrollBridge> _activeDrumrollBridges = new List<DrumrollBridge>();

        #endregion

        #region Event System

        /*
            UnityEvents provide a decoupled communication system between GameManager and UI components.
            This design allows UI elements to subscribe to game events without creating direct dependencies,
            enabling modular UI development and easy testing.
        
            Score/Combo events provide integer values for discrete metrics.
            Accuracy events provide float values for continuous performance measurement, and require decimal values. 
            Note hit events provide note references for detailed hit information. 
            Song change events provide song and chart data for UI updates.
        */

        /// <summary>
        /// Event called when the score is changed containing the new score value.
        /// </summary>
        public event UnityAction<int> OnScoreChange;
        /// <summary>
        /// Event called when the combo is updated containing the new combo value.
        /// </summary>
        public event UnityAction<int> OnComboChange;

        /// <summary>
        /// Event called when the accuracy is updated containing the new accuracy value.
        /// </summary>
        public event UnityAction<float> OnAccuracyChange;

        /// <summary>
        /// Event called when the accuracy is updated containing the new accuracy value.
        /// </summary>
        public event UnityAction<Note> OnNoteHit;

        /// <summary>
        /// Event called when the song is changed containing the new SongEntry and ChartData values.
        /// </summary>
        public event UnityAction<SongEntry, ChartData> OnSongChanged;

        #endregion

        #region Scoring System

        /*
            Tracks player performance through score, combo, and gauge metrics.
            Integer types for discrete values, float for continuous measurements.
        */
        private int _score = 0;
        private int _combo = 0;
        private int _maxCombo = 0; // Track the highest combo achieved during the song
        private float _gauge;
        private float _maxGauge = 100;

        // Public properties for external access
        public int Combo => _combo;
        public int MaxCombo => _maxCombo;
        public int Score => _score;
        public float Accuracy => GetAccuracy();

        /*
            Performance tracking counters for hit statistics throughout gameplay.
            Using integers ensures precise counting without floating-point precision issues.
        */
        private int _hitGoods;
        private int _hitOkays;
        private int _hitBads;

        #endregion

        #region Judgement System

        /*
            Timing windows for different hit judgements.
            Using const values ensures consistent timing across all hit detection.
            MISS_WINDOW: 125ms, OKAY_WINDOW: 108ms, GOOD_WINDOW: 42ms.
        */
        private const float MISS_WINDOW = 0.125f;  // 125ms
        private const float OKAY_WINDOW = 0.108f;  // 108ms
        private const float GOOD_WINDOW = 0.042f;  // 42ms
        
        /*
            Additional delay before destroying missed notes to allow for visual feedback
            and better player experience.
        */
        private const float MISS_DESTRUCTION_DELAY = 2.0f;  // 2 seconds after miss window

        #endregion

        #region Debug and Development Tools

        /*
            Visual feedback during development and debugging sessions.
            Debug indicators help verify system behavior and timing accuracy.
        */
        [SerializeField]
        private GameObject _debugDrumrollIndicator;

        [SerializeField]
        private TextMeshProUGUI _debugJudgementIndicator;

        #endregion

        #region Initialization Methods

        /*
            Establishes the singleton instance before any other systems attempt to access it.
            Ensures the GameManager is available throughout the entire component lifecycle.
        */
        private void Awake()
        {
            // Ensure only one instance of the GameManager exists as per the singleton pattern
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /*
            Initializes all game systems after the component hierarchy is fully established.
            Includes audio configuration, event subscriptions, and initial state setup.
        */
        private void Start()
        {
            _songStartTime = (float)AudioSettings.dspTime;
            _drum.OnHit += RegisterHit;

            // Subscribe to pause input
            if (_inputReader != null)
            {
                _inputReader.PauseEvent += OnPauseInput;
            }

            Debug.Log("GameManager initialized.");
            
            // Check if we have song data from the menu scene
            CheckForSongDataFromMenu();
        }

        /*
            Ensures proper cleanup of event subscriptions when the component is disabled or destroyed.
            Prevents memory leaks and invalid event calls from disabled components.
        */
        private void OnDisable()
        {
            _drum.OnHit -= RegisterHit;
            
            // Unsubscribe from pause input
            if (_inputReader != null)
            {
                _inputReader.PauseEvent -= OnPauseInput;
            }
        }

        #endregion

        #region Scene Transition Methods

        /// <summary>
        /// Checks if song data was passed from the menu scene and starts the game if available.
        /// </summary>
        private void CheckForSongDataFromMenu()
        {
            Debug.Log("=== CHECKING FOR SONG DATA FROM MENU ===");
            Debug.Log($"GameDataManager.HasSongData(): {GameDataManager.HasSongData()}");
            
            if (GameDataManager.HasSongData())
            {
                var song = GameDataManager.GetSelectedSong();
                var chart = GameDataManager.GetSelectedChart();
                
                Debug.Log($"Song from GameDataManager: {song?.Title ?? "null"} - {song?.Artist ?? "null"}");
                Debug.Log($"Chart from GameDataManager: {chart?.Version ?? "null"} - {chart?.Difficulty}");
                Debug.Log($"Audio filename: {song?.AudioFilename ?? "null"}");
                
                if (song != null && chart != null)
                {
                    Debug.Log($"Starting game with song data from menu: {song.Title} - {chart.Version} ({chart.Difficulty})");
                    
                    // Start the game with the selected song and chart
                    StartGame(song, chart);
                    
                    // Clear the stored data since we've used it
                    GameDataManager.ClearSongData();
                }
                else
                {
                    Debug.LogWarning("GameDataManager has song data but song or chart is null");
                }
            }
            else
            {
                Debug.Log("No song data from menu - GameManager will use default behavior");
            }
            
            Debug.Log("=== END CHECKING FOR SONG DATA ===");
        }

        #endregion

        #region Core Game Loop


        /*
            Main game loop that executes every frame and manages all real-time gameplay systems. This is an internal Unity function that is
            derived from MonoBehaviour. Includes timing synchronization, state transitions, note management, and performance optimizations.
        */
        private void Update()
        {
            // Check if game is paused - if so, skip all gameplay updates
            if (_isPaused)
            {
                return;
            }

            _debugDrumrollIndicator.SetActive(_isInDrumroll);

            /*
                DSP time synchronization maintains high-precision timing by tracking current and previous values.
                The smoothing algorithm prevents timing jitter while maintaining audio synchronization.
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
                Core timing update retrieves synchronized time and stores it as current song time.
                This value drives all timing-dependent systems including note spawning, hit detection, and state transitions.
            */
            // Only update song time if not paused
            if (!_isPaused)
            {
                float synchronizedTime = GetSynchronizedSongTime();
                _songTime = synchronizedTime;
            }

            /*
                The system automatically transitions from delay to playing state when
                the countdown reaches zero. This transition triggers the start of
                normal gameplay timing and note spawning.
            */
            if (_timingState == TimingState.Delay && _songTime >= 0f)
            {
                _timingState = TimingState.Playing;
            }

            /*
            Note cleanup is performed periodically rather than every frame to reduce
            processing overhead. This is performed every 30 frames.
            */
            if (Time.frameCount % 30 == 0)
            {
                CleanupDestroyedNotes();
            }
            
            // Perform automatic health checks every 300 frames (about 5 seconds at 60fps)
            if (Time.frameCount % 300 == 0)
            {
                CheckNoteSystemHealth();
                
                // Auto-fix if the system is severely corrupted
                if (_activeNotes.Count > 0 && (_nextJudgableNoteIndex < 0 || _nextJudgableNoteIndex >= _activeNotes.Count))
                {
                    Debug.LogWarning("Automatic note system corruption detected - performing auto-fix");
                    CheckAndAutoFixNoteSystem();
                }
                
                // Check for stuck notes that might be causing unhittable note issues
                DetectAndFixStuckNotes();
            }

            // Calculate effective preempt time once (base + 3s delay)
            float effectivePreemptTime = _preemptTime + 3f;

            // Note spawning logic - only spawn notes when timing system is ready
            if (_timingState == TimingState.Delay || _timingState == TimingState.Playing)
            {
                /*
                    Notes are spawned based on their hit time and the effective preempt time.
                    The effective preempt time includes the base preempt time plus the 3-second
                    delay period, ensuring notes spawn early enough during countdown.
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
                This system processes notes that have passed their hit window without
                being hit by the player. Missed notes are marked as hit, combo is reset,
                and visual effects are played to provide player feedback.
            
                We now process ALL notes that have passed their hit window, not just
                notes in sequential order. This prevents notes from slipping through
                without being judged.
            */

            // Process ALL notes that have passed their hit window, not just sequential ones
            for (int i = 0; i < _activeNotes.Count; i++)
            {
                Note note = _activeNotes[i];
                
                // Skip if note is invalid
                if (!IsNoteValid(note))
                {
                    continue;
                }

                // Skip if already judged
                if (note.IsHit)
                {
                    continue;
                }

                // Skip DrumrollBalloonEnd notes - they don't need to be judged
                if (note.NoteType == NoteType.DrumrollBalloonEnd)
                {
                    continue;
                }

                float currentTime = GetSmoothedSongTime();
                if (currentTime > note.HitTime + MISS_WINDOW)
                {
                    // Mark note as missed
                    note.IsHit = true;

                    /*
                        Combo is reset and visual effects are played
                        The system safely handles cases where notes may be destroyed during processing
                    */
                    _combo = 0;
                    OnComboChange?.Invoke(_combo);

                    PlayMissEffect(_judgementCircle.position);
                    
                    Debug.Log($"Note missed at {note.HitTime}s - will be destroyed in {MISS_DESTRUCTION_DELAY}s");
                }
            }
            
            // Now clean up notes that have been missed for long enough
            for (int i = _activeNotes.Count - 1; i >= 0; i--)
            {
                Note note = _activeNotes[i];
                
                if (note == null || !IsNoteValid(note))
                {
                    _activeNotes.RemoveAt(i);
                    continue;
                }

                // Only destroy notes that have been missed for long enough
                if (note.IsHit && note.NoteType != NoteType.Drumroll && note.NoteType != NoteType.DrumrollBig)
                {
                    float currentTime = GetSmoothedSongTime();
                    if (currentTime > note.HitTime + MISS_WINDOW + MISS_DESTRUCTION_DELAY)
                    {
                        // Destroy the missed note and remove it from the list
                        if (note.gameObject != null)
                        {
                            Destroy(note.gameObject);
                            Debug.Log($"Destroying missed note at {note.HitTime}s after {MISS_DESTRUCTION_DELAY}s delay");
                        }

                        _activeNotes.RemoveAt(i);
                    }
                }
            }
            
            // Update the next judgable note index to point to the first unjudged note
            UpdateNextJudgableNoteIndex();

            /*
                Monitors the current drumroll state and automatically ends it when
                the drumroll duration expires. This prevents drumrolls from continuing indefinitely.
            */
            if (_isInDrumroll)
            {
                float currentTime = GetSmoothedSongTime();
                if (currentTime > _drumrollEndTime)
                {
                    EndDrumroll();
                }
            }
            
            /*
                Check for song completion when all notes have been processed and
                the song has reached its end time.
            */
            CheckForSongCompletion();
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Removes destroyed and invalid notes from the active notes list to maintain system integrity.
        /// </summary>
        /// <remarks>
        /// This method performs periodic cleanup to prevent null reference exceptions and
        /// maintain performance. The next judgable note index is also incremented to ensure
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
            if (_activeNotes.Count == 0)
            {
                _nextJudgableNoteIndex = 0;
            }
            else if (_nextJudgableNoteIndex >= _activeNotes.Count)
            {
                _nextJudgableNoteIndex = _activeNotes.Count - 1;
            }
            else if (_nextJudgableNoteIndex < 0)
            {
                _nextJudgableNoteIndex = 0;
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
            if (note == null)
            {
                Debug.LogWarning("Note validation failed: note is null");
                return false;
            }
            
            if (note.gameObject == null)
            {
                Debug.LogWarning($"Note validation failed: GameObject is null for note at {note.HitTime}s");
                return false;
            }
            
            if (!note.enabled)
            {
                Debug.LogWarning($"Note validation failed: Note component is disabled for note at {note.HitTime}s");
                return false;
            }
            
            if (!note.gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"Note validation failed: GameObject is inactive for note at {note.HitTime}s");
                return false;
            }
            
            if (note.gameObject == null || note.gameObject.scene.name == null)
            {
                Debug.LogWarning($"Note validation failed: GameObject is being destroyed for note at {note.HitTime}s");
                return false;
            }
            
            return true;
        }


        /// <summary>
        /// Forces immediate cleanup of all invalid notes in the system.
        /// </summary>
        /// <remarks>
        /// For debugging and development purposes, available through context menu
        /// Performs comprehensive cleanup and logs results for analysis.
        /// This was made available through the Unity context menu for easy access during development.
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
        /// For debugging and development purposes, available through context menu
        /// Outputs detailed information about the current state of the note system, 
        /// including counts, indices, and timing information.
        /// </remarks>
        [ContextMenu("Check Note System Health")]
        public void CheckNoteSystemHealth()
        {
            Debug.Log($"NOTE SYSTEM HEALTH CHECK");
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
                Debug.LogWarning($"Found {destroyedNotes} destroyed notes in list - this indicates cleanup issues.");
            }

            if (disabledNotes > 0)
            {
                Debug.LogWarning($"Found {disabledNotes} disabled notes - this may cause hit detection issues.");
            }
        }

        /// <summary>
        /// Creates and initializes a new note GameObject from chart data.
        /// </summary>
        /// <param name="noteData">The chart data containing note timing and type information.</param>
        /// <remarks>
        /// This method handles the complete note spawning process including instantiation,
        /// initialization, positioning, and drumroll bridge creation for drumroll notes.
        /// Notes are added to the active notes list for processing and management.
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
                Drumroll notes require visual bridges to connect start and end points.
                This system automatically creates and manages bridge objects for proper
                visual representation of drumroll sequences.
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
        /// audio preparation, and system state setup. 
        /// Marks the song as manually selected to prevent automatic song selection from interfering with gameplay.
        /// </remarks>
        private void InitializeGameState()
        {
            _score = 0;
            _combo = 0;
            OnScoreChange?.Invoke(_score);
            OnComboChange?.Invoke(_combo);
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
            // If game is paused, return the last known song time without advancing
            if (_isPaused)
            {
                return _songTime;
            }
            
            // Use synchronized time from audio source for more accurate timing
            return GetSynchronizedSongTime();
        }

        #endregion

        #region Chart System Methods

        /*
            These methods handle the loading and management of chart data,
            including song selection, chart loading, and game initialization.
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

            // Load background image from chart data
            LoadBackgroundImage();

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
            
            // Load background image if we have song data
            if (_currentSong != null)
            {
                LoadBackgroundImage();
            }
            
            ResetGameState();

            Debug.Log($"Loaded chart only: {chart.Version}");
            Debug.Log($"Chart has {chart.HitObjects.Count} hit objects");
            Debug.Log($"First note at: {(chart.HitObjects.Count > 0 ? chart.HitObjects[0].Time : 0)}s");
            Debug.Log($"Last note at: {(chart.HitObjects.Count > 0 ? chart.HitObjects[chart.HitObjects.Count - 1].Time : 0)}s");
        }

        #endregion

        #region Debugging and Development Tools

        /// <summary>
        /// Manually loads audio from a hardcoded test path for development purposes.
        /// </summary>
        /// <remarks>
        /// This method is used for testing audio loading functionality without requiring
        /// a complete song selection.
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
        /// Attempts to load and play audio from the current song entry,
        /// providing immediate feedback for audio system testing.
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
        /// Tests the chart system by loading the first available song for development.
        /// </summary>
        /// <remarks>
        /// This method provides a quick way to test chart loading and gameplay
        /// systems without requiring manual song selection.
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
        private System.Collections.IEnumerator AutoStartWithFirstChart()
        {
            // Wait a few seconds for everything to initialize
            yield return new WaitForSeconds(2f);

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
        [ContextMenu("Enable Auto-Start")]
        public void EnableAutoStart()
        {
            Debug.Log("Auto-start enabled - will start automatically on next scene load");
            _songManuallySelected = false;
        }

        /// <summary>
        /// Disables automatic game start behavior, requiring manual song selection.
        /// </summary>
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
        }

        /// <summary>
        /// Asynchronously loads and schedules audio playback with proper timing synchronization.
        /// </summary>
        /// <param name="audioPath">The file path to the audio file to load.</param>
        /// <returns>An IEnumerator for coroutine execution.</returns>
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

            // Load audio file using UnityWebRequest (instead of WWW)
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
        private void ResetGameState()
        {
            _score = 0;
            _combo = 0;
            _maxCombo = 0; // Reset max combo for new game
            _gauge = 0f; // Start at 0 - player must earn gauge by hitting notes
            _songTime = -3f; // Start in delay period
            _timingState = TimingState.Delay; // Reset timing state
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
            
            // Reset hit statistics
            _hitGoods = 0;
            _hitOkays = 0;
            _hitBads = 0;

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
        public SongEntry GetCurrentSong()
        {
            return _currentSong;
        }

        /// <summary>
        /// Returns the currently selected chart data.
        /// </summary>
        /// <returns>The current chart data or null if none is selected.</returns>
        public ChartData GetCurrentChart()
        {
            return _currentChart;
        }

        /// <summary>
        /// Checks if manual song selection is currently active.
        /// </summary>
        /// <returns>True if manual selection is active, false if auto-start is enabled.</returns>
        public bool IsManualSelectionActive()
        {
            return _songManuallySelected;
        }

        /// <summary>
        /// Returns formatted information about the current song selection for UI display.
        /// </summary>
        /// <returns>A formatted string containing song title, chart version, difficulty, and selection type.</returns>
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
        public bool IsGameReadyToPlay()
        {
            return _currentSong != null && _currentChart != null && _songManager?.GetCurrentAudioClip() != null;
        }

        /// <summary>
        /// Returns a string describing the current game readiness status.
        /// </summary>
        /// <returns>A human-readable string describing the current game state.</returns>
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
        /// Core hit detection logic with drumroll processing, note validation, timing windows, and judgement determination. 
        /// It processes notes sequentially and updates game state based on hit accuracy.
        /// </remarks>
        private void RegisterHit(HitType hitType)
        {
            float currentTime = GetSmoothedSongTime();
            float hitWindow = MISS_WINDOW;

            if (_isInDrumroll)
            {
                HandleDrumrollHit(hitType, currentTime);
            }

            // Process notes sequentially, but be more careful about index management
            int processedCount = 0;
            int maxProcessedPerFrame = 10; // Prevent infinite loops
            
            while (_nextJudgableNoteIndex < _activeNotes.Count && processedCount < maxProcessedPerFrame)
            {
                processedCount++;
                
                // Validate index bounds
                if (_nextJudgableNoteIndex < 0 || _nextJudgableNoteIndex >= _activeNotes.Count)
                {
                    Debug.LogWarning($"Invalid note index: {_nextJudgableNoteIndex}, resetting to 0");
                    _nextJudgableNoteIndex = 0;
                    break;
                }

                Note note = _activeNotes[_nextJudgableNoteIndex];

                // Check if note is valid before processing
                if (!IsNoteValid(note))
                {
                    // Remove invalid note from list and continue
                    _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                    // Don't increment index since we removed an element
                    continue;
                }

                if (note.IsHit)
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }

                if (note.NoteType == NoteType.DrumrollBalloonEnd)
                {
                    _nextJudgableNoteIndex++;
                    continue;
                }

                // Check if note is too early to hit
                if (currentTime < note.HitTime - hitWindow)
                {
                    // Note is too early, stop processing
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

                        if (note.HitObject.Type == NoteType.Drumroll)
                        {
                            StartDrumroll(note.HitObject, currentTime);
                        }

                        ApplyJudgementEffects(judgement, note);

                        if (note.NoteType == NoteType.Drumroll || note.NoteType == NoteType.DrumrollBig)
                        {
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
                        
                        // Note was successfully processed, break to allow for next input
                        break;
                    }
                    else
                    {
                        // Input type doesn't match, but note is in hit window
                        // Don't advance index, allow other input types to try
                        Debug.Log($"Input type mismatch: {hitType} vs {note.HitObject.Type}");
                        break;
                    }
                }
                else
                {
                    // Note is outside hit window - mark as missed
                    if (!note.IsHit)
                    {
                        note.IsHit = true;
                        ApplyJudgementEffects(Judgement.Miss, note);
                        Debug.Log($"Note missed at {note.HitTime}s (time diff: {Mathf.Abs(currentTime - note.HitTime):F3}s)");
                    }

                    if (note.NoteType == NoteType.Drumroll || note.NoteType == NoteType.DrumrollBig)
                    {
                        _nextJudgableNoteIndex++;
                    }
                    else if (note.NoteType == NoteType.DrumrollBalloonEnd)
                    {
                        DestroyDrumrollHeadAndBridge(note);
                        Destroy(note.gameObject);
                        _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                        // Don't increment index since we removed an element
                    }
                    else
                    {
                        Destroy(note.gameObject);
                        _activeNotes.RemoveAt(_nextJudgableNoteIndex);
                        // Don't increment index since we removed an element
                    }
                }
            }
            
            // Safety check: if we processed too many notes, log a warning
            if (processedCount >= maxProcessedPerFrame)
            {
                Debug.LogWarning($"Processed {processedCount} notes in one frame - this may indicate a problem");
            }
        }

        /// <summary>
        /// Handles pause input from the InputReader.
        /// Toggles pause state and notifies the PauseMenuManager.
        /// </summary>
        private void OnPauseInput()
        {
            if (_pauseMenuManager != null)
            {
                _pauseMenuManager.TogglePause();
            }
        }

        #endregion

        #region Drumroll System

        /*
            These methods manage the complex drumroll gameplay mechanics including
            start detection, hit processing, and end conditions. Drumrolls provide
            continuous gameplay challenges and bonus scoring opportunities.
        */

        /// <summary>
        /// Initializes a new drumroll sequence when a drumroll start note is hit.
        /// </summary>
        /// <param name="drumrollStart">The HitObject that initiated the drumroll.</param>
        /// <param name="currentTime">The current game time when the drumroll started.</param>
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
                _maxCombo = Mathf.Max(_maxCombo, _combo); // Update max combo
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
        private void EndDrumroll()
        {
            Debug.Log("Drumroll ended! Total hits: " + _drumrollHits);
            // Award bonus points based on drumroll performance
            int bonusPoints = _drumrollHits * 5;
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
            These methods handle the core scoring and feedback system based on
            player timing accuracy. The system provides immediate feedback and
            maintains scoring consistency throughout gameplay.
        */

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
        /// Determines the judgement quality based on timing accuracy.
        /// </summary>
        /// <param name="timeDifference">The absolute time difference between hit time and actual hit.</param>
        /// <returns>The judgement quality (Good, Okay, or Miss) based on timing windows.</returns>
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
        /// Hndles all judgement effects including score updates, combo progression, gauge changes, visual feedback, and event notifications.
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
                    _maxCombo = Mathf.Max(_maxCombo, _combo); // Update max combo
                    _score += 100; // Base score for Good
                    OnComboChange?.Invoke(_combo);
                    OnNoteHit?.Invoke(note);
                    PlayHitEffect(note.transform.position);
                    Debug.Log("Good!");
                    AddGauge(2.5f);
                    break;

                case Judgement.Okay:
                    _debugJudgementIndicator.text = "Okay!";
                    _hitOkays++;
                    _combo++;
                    _maxCombo = Mathf.Max(_maxCombo, _combo); // Update max combo
                    _score += 50;
                    OnComboChange?.Invoke(_combo);
                    OnNoteHit?.Invoke(note);
                    PlayHitEffect(note.transform.position);
                    Debug.Log("Okay!");
                    AddGauge(1.25f);
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
        /// and Miss represents really poor timing or missed notes.
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
        /// The accuracy calculation uses the standard osu! Taiko formula which weights different judgement types appropriately for fair performance measurement across various skill levels.
        /// </remarks>
        public float GetAccuracy()
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
        /// This method instantiates hit effect prefabs at the specified position to provide immediate visual feedback to the player. 
        /// The effects are positioned at the judgement circle for consistent visual placement.
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
        /// Safely handles cases where the audio system may not be ready or the
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
        /// Time display for UI elements. Safely handles cases where the audio system may not be ready.
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
        /// This method provides normalized gauge values for UI display and other systems that require percentage-based gauge representation.
        /// </remarks>
        public float GetGaugePercentage()
        {
            return _gauge / _maxGauge;
        }

        #endregion

        #region Background Management

        /// <summary>
        /// Tests the background system by loading a sample image.
        /// </summary>
        /// <remarks>
        /// This method is available through the Unity context menu for testing
        /// background loading and display functionality.
        /// </remarks>
        [ContextMenu("Test Background System")]
        public void TestBackgroundSystem()
        {
            if (_currentSong != null && _currentSong.HasImage())
            {
                Debug.Log("Testing background system with current song image");
                LoadBackgroundImage();
            }
            else
            {
                Debug.LogWarning("No song image available for background testing");
            }
        }

        /// <summary>
        /// Tests different background dim levels.
        /// </summary>
        /// <remarks>
        /// This method cycles through different dim levels to test
        /// the dimming overlay functionality.
        /// </remarks>
        [ContextMenu("Test Background Dim Levels")]
        public void TestBackgroundDimLevels()
        {
            float[] testLevels = { 0.0f, 0.3f, 0.5f, 0.7f, 0.9f };
            StartCoroutine(TestBackgroundDimCoroutine(testLevels));
        }

        /// <summary>
        /// Tests background scaling with different fit modes.
        /// </summary>
        /// <remarks>
        /// This method tests different background scaling approaches
        /// to help debug scaling issues.
        /// </remarks>
        [ContextMenu("Test Background Scaling")]
        public void TestBackgroundScaling()
        {
            if (_currentSong != null && _currentSong.HasImage())
            {
                Debug.Log("Testing background scaling...");
                LoadBackgroundImage();
            }
            else
            {
                Debug.LogWarning("No song image available for scaling test");
            }
        }

        /// <summary>
        /// Tests note destruction timing and logging.
        /// </summary>
        /// <remarks>
        /// This method helps debug note destruction issues
        /// by checking current note states and timing.
        /// </remarks>
        [ContextMenu("Test Note Destruction Timing")]
        public void TestNoteDestructionTiming()
        {
            Debug.Log("=== NOTE DESTRUCTION TIMING TEST ===");
            Debug.Log($"Current song time: {_songTime:F3}s");
            Debug.Log($"Active notes count: {_activeNotes.Count}");
            Debug.Log($"Next judgable note index: {_nextJudgableNoteIndex}");
            Debug.Log($"Miss destruction delay: {MISS_DESTRUCTION_DELAY:F1}s");
            
            if (_activeNotes.Count > 0)
            {
                for (int i = 0; i < Mathf.Min(_activeNotes.Count, 5); i++) // Show first 5 notes
                {
                    var note = _activeNotes[i];
                    if (note != null)
                    {
                        float timeUntilMiss = note.HitTime + MISS_WINDOW - _songTime;
                        float timeUntilDestruction = note.HitTime + MISS_WINDOW + MISS_DESTRUCTION_DELAY - _songTime;
                        
                        Debug.Log($"Note {i}: Time={note.HitTime:F3}s, IsHit={note.IsHit}, " +
                                $"TimeUntilMiss={timeUntilMiss:F3}s, TimeUntilDestruction={timeUntilDestruction:F3}s");
                    }
                }
            }
            
            Debug.Log("=====================================");
        }

        /// <summary>
        /// Coroutine to test different background dim levels.
        /// </summary>
        /// <param name="dimLevels">Array of dim levels to test.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private System.Collections.IEnumerator TestBackgroundDimCoroutine(float[] dimLevels)
        {
            foreach (float level in dimLevels)
            {
                SetBackgroundDim(level);
                Debug.Log($"Testing background dim level: {level:F2}");
                yield return new WaitForSeconds(1f);
            }
            
            // Reset to default
            SetBackgroundDim(0.7f);
            Debug.Log("Background dim test complete, reset to default: 0.7");
        }

        /// <summary>
        /// Loads and displays the background image from the current chart data.
        /// </summary>
        /// <remarks>
        /// This method loads the background image associated with the current song/chart
        /// and applies it to the background system with proper scaling and dimming.
        /// </remarks>
        private void LoadBackgroundImage()
        {
            if (!_enableBackground || _backgroundImage == null) return;

            if (_currentSong != null && _currentSong.HasImage())
            {
                string imagePath = _currentSong.GetBestAvailableImagePath();
                if (!string.IsNullOrEmpty(imagePath))
                {
                    Debug.Log($"Loading background image: {imagePath}");
                    StartCoroutine(LoadBackgroundImageCoroutine(imagePath));
                }
                else
                {
                    Debug.LogWarning("Song has image but path is null or empty");
                    SetDefaultBackground();
                }
            }
            else
            {
                Debug.Log("No background image available for current song");
                SetDefaultBackground();
            }
        }

        /// <summary>
        /// Coroutine to load background image asynchronously.
        /// </summary>
        /// <param name="imagePath">Path to the background image file.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private System.Collections.IEnumerator LoadBackgroundImageCoroutine(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) yield break;

            // Check if file exists
            if (!System.IO.File.Exists(imagePath))
            {
                Debug.LogWarning($"Background image file not found: {imagePath}");
                SetDefaultBackground();
                yield break;
            }

            // Load image using UnityWebRequest
            string fullPath = "file://" + imagePath;
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(fullPath))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    if (texture != null)
                    {
                        // Create sprite from texture
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        
                        // Apply to background image
                        _backgroundImage.sprite = sprite;
                        _backgroundImage.color = Color.white;
                        
                        // Configure background image for proper display
                        ConfigureBackgroundImage(texture);
                        
                        // Apply dimming overlay
                        UpdateBackgroundDim();
                        
                        Debug.Log($"Background image loaded successfully: {imagePath}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to get texture from background image: {imagePath}");
                        SetDefaultBackground();
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load background image: {www.error}");
                    SetDefaultBackground();
                }
            }
        }

        /// <summary>
        /// Configures the background image for proper display.
        /// </summary>
        /// <param name="texture">The loaded background texture.</param>
        /// <remarks>
        /// This method sets up the background image to fit within the screen boundaries
        /// while maintaining aspect ratio and ensuring good gameplay visibility.
        /// </remarks>
        private void ConfigureBackgroundImage(Texture2D texture)
        {
            if (_backgroundImage == null || texture == null) return;

            // Set the image type for proper scaling
            _backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
            _backgroundImage.preserveAspect = false;

            // Get the RectTransform of the background image
            RectTransform imageRect = _backgroundImage.rectTransform;
            if (imageRect == null) return;

            // Get the parent container's size (should be full screen)
            RectTransform parentRect = imageRect.parent as RectTransform;
            if (parentRect == null) return;

            Vector2 parentSize = parentRect.rect.size;
            
            // Calculate how the image should be sized to fit within the screen
            float textureAspect = (float)texture.width / texture.height;
            float screenAspect = parentSize.x / parentSize.y;
            
            Vector2 newSize;
            
            if (textureAspect > screenAspect)
            {
                // Texture is wider than screen - fit to width, may crop height
                newSize = new Vector2(parentSize.x, parentSize.x / textureAspect);
            }
            else
            {
                // Texture is taller than screen - fit to height, may crop width
                newSize = new Vector2(parentSize.y * textureAspect, parentSize.y);
            }
            
            // Ensure the image doesn't exceed screen boundaries
            if (newSize.x > parentSize.x)
            {
                newSize.x = parentSize.x;
                newSize.y = newSize.x / textureAspect;
            }
            
            if (newSize.y > parentSize.y)
            {
                newSize.y = parentSize.y;
                newSize.x = newSize.y * textureAspect;
            }
            
            // Set the size to fit within the screen while maintaining aspect ratio
            imageRect.sizeDelta = newSize;
            
            // Center the image
            imageRect.anchoredPosition = Vector2.zero;
            
            Debug.Log($"Background configured: texture {texture.width}x{texture.height} (aspect: {textureAspect:F2}), " +
                     $"screen {parentSize.x:F0}x{parentSize.y:F0} (aspect: {screenAspect:F2}), " +
                     $"new size {newSize.x:F0}x{newSize.y:F0}");
        }

        /// <summary>
        /// Sets a default background when no image is available.
        /// </summary>
        /// <remarks>
        /// This method provides a fallback background to ensure the gameplay
        /// always has a consistent visual appearance.
        /// </remarks>
        private void SetDefaultBackground()
        {
            if (_backgroundImage == null) return;

            // Clear the sprite and set a default color
            _backgroundImage.sprite = null;
            _backgroundImage.color = new Color(0.1f, 0.1f, 0.15f, 1f); // Dark blue-gray
            
            // Apply dimming overlay
            UpdateBackgroundDim();
            
            Debug.Log("Default background applied");
        }

        /// <summary>
        /// Updates the background dimming overlay.
        /// </summary>
        /// <remarks>
        /// This method applies the dimming effect to ensure gameplay elements
        /// remain visible over the background image.
        /// </remarks>
        private void UpdateBackgroundDim()
        {
            if (_backgroundOverlay == null) return;

            // Set the overlay color based on dim level
            Color overlayColor = new Color(0f, 0f, 0f, _backgroundDim);
            _backgroundOverlay.color = overlayColor;
            
            Debug.Log($"Background dim updated: {_backgroundDim:F2}");
        }

        /// <summary>
        /// Sets the background dimming level.
        /// </summary>
        /// <param name="dimLevel">Dim level from 0 (no dim) to 1 (black).</param>
        /// <remarks>
        /// This method allows runtime adjustment of background dimming
        /// for optimal gameplay visibility.
        /// </remarks>
        public void SetBackgroundDim(float dimLevel)
        {
            _backgroundDim = Mathf.Clamp01(dimLevel);
            UpdateBackgroundDim();
        }

        /// <summary>
        /// Gets the current background dimming level.
        /// </summary>
        /// <returns>Current dim level from 0 to 1.</returns>
        public float GetBackgroundDim()
        {
            return _backgroundDim;
        }

        /// <summary>
        /// Enables or disables the background system.
        /// </summary>
        /// <param name="enabled">Whether the background system should be enabled.</param>
        public void SetBackgroundEnabled(bool enabled)
        {
            _enableBackground = enabled;
            
            if (_backgroundImage != null)
            {
                _backgroundImage.gameObject.SetActive(enabled);
            }
            
            if (_backgroundOverlay != null)
            {
                _backgroundOverlay.gameObject.SetActive(enabled);
            }
            
            Debug.Log($"Background system {(enabled ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// Gets whether the background system is currently enabled.
        /// </summary>
        /// <returns>True if background system is enabled, false otherwise.</returns>
        public bool IsBackgroundEnabled()
        {
            return _enableBackground;
        }

        #endregion

        /// <summary>
        /// Resets the note system to a clean state when it gets corrupted.
        /// This is a safety mechanism to prevent notes from becoming permanently unhittable.
        /// </summary>
        [ContextMenu("Reset Note System")]
        public void ResetNoteSystem()
        {
            Debug.Log("=== RESETTING NOTE SYSTEM ===");
            
            // Clear all active notes
            foreach (var note in _activeNotes)
            {
                if (note != null && note.gameObject != null)
                {
                    Destroy(note.gameObject);
                }
            }
            _activeNotes.Clear();
            
            // Reset indices
            _nextNoteIndex = 0;
            _nextJudgableNoteIndex = 0;
            
            // Clear drumroll state
            _isInDrumroll = false;
            _currentDrumroll = null;
            _drumrollHits = 0;
            _drumrollStartTime = 0f;
            _drumrollEndTime = 0f;
            
            // Clear drumroll bridges
            foreach (var bridge in _activeDrumrollBridges)
            {
                if (bridge != null && bridge.gameObject != null)
                {
                    Destroy(bridge.gameObject);
                }
            }
            _activeDrumrollBridges.Clear();
            
            Debug.Log("Note system reset complete");
            Debug.Log("=== END RESET ===");
        }

        /// <summary>
        /// Checks if the note system is in a healthy state and automatically resets if corrupted.
        /// This helps prevent notes from becoming permanently unhittable.
        /// </summary>
        [ContextMenu("Check and Auto-Fix Note System")]
        public void CheckAndAutoFixNoteSystem()
        {
            Debug.Log("=== CHECKING NOTE SYSTEM HEALTH ===");
            
            bool needsReset = false;
            
            // Check for invalid indices
            if (_nextJudgableNoteIndex < 0 || _nextJudgableNoteIndex >= _activeNotes.Count)
            {
                Debug.LogWarning($"Invalid note index: {_nextJudgableNoteIndex} (should be 0-{_activeNotes.Count - 1})");
                needsReset = true;
            }
            
            // Check for null notes in the list
            int nullNoteCount = 0;
            int disabledNoteCount = 0;
            int invalidNoteCount = 0;
            
            for (int i = 0; i < _activeNotes.Count; i++)
            {
                var note = _activeNotes[i];
                if (note == null)
                {
                    nullNoteCount++;
                }
                else if (!note.enabled)
                {
                    disabledNoteCount++;
                }
                else if (!IsNoteValid(note))
                {
                    invalidNoteCount++;
                }
            }
            
            if (nullNoteCount > 0)
            {
                Debug.LogWarning($"Found {nullNoteCount} null notes in active notes list");
                needsReset = true;
            }
            
            if (disabledNoteCount > 0)
            {
                Debug.LogWarning($"Found {disabledNoteCount} disabled notes in active notes list");
                needsReset = true;
            }
            
            if (invalidNoteCount > 0)
            {
                Debug.LogWarning($"Found {invalidNoteCount} invalid notes in active notes list");
                needsReset = true;
            }
            
            // Check if notes are stuck in the system
            if (_activeNotes.Count > 0)
            {
                var firstNote = _activeNotes[0];
                if (firstNote != null && firstNote.IsHit)
                {
                    float currentTime = GetSmoothedSongTime();
                    float timeSinceHit = currentTime - firstNote.HitTime;
                    
                    if (timeSinceHit > 10f) // If a hit note has been in the system for more than 10 seconds
                    {
                        Debug.LogWarning($"Hit note has been in system for {timeSinceHit:F1}s - this indicates a problem");
                        needsReset = true;
                    }
                }
            }
            
            if (needsReset)
            {
                Debug.LogWarning("Note system is corrupted - auto-resetting...");
                ResetNoteSystem();
            }
            else
            {
                Debug.Log("Note system is healthy");
            }
            
            Debug.Log("=== END HEALTH CHECK ===");
        }

        /// <summary>
        /// Manually triggers a note system health check and auto-fix.
        /// Available through the Unity context menu for development and debugging purposes.
        /// </summary>
        [ContextMenu("Auto-Fix Note System")]
        public void AutoFixNoteSystem()
        {
            Debug.Log("Manual note system auto-fix triggered");
            CheckAndAutoFixNoteSystem();
        }
        
        /// <summary>
        /// Immediately ends the current song and returns to main menu.
        /// This is useful for testing and development purposes.
        /// </summary>
        [ContextMenu("End Song Immediately")]
        public void EndSongImmediately()
        {
            EndSongImmediatelyInternal();
        }

        [ContextMenu("Restart Current Song")]
        public void RestartCurrentSongFromContextMenu()
        {
            RestartCurrentSong();
        }
        
        /// <summary>
        /// Public method to immediately end the current song and return to main menu.
        /// Can be called from UI buttons or other scripts.
        /// </summary>
        public void EndSongImmediatelyPublic()
        {
            EndSongImmediatelyInternal();
        }
        
        /// <summary>
        /// Internal implementation of song ending logic.
        /// </summary>
        private void EndSongImmediatelyInternal()
        {
            if (_timingState == TimingState.Completed)
            {
                Debug.Log("Song is already completed");
                return;
            }
            
            if (_currentChart == null)
            {
                Debug.LogWarning("No chart loaded - cannot end song");
                return;
            }
            
            Debug.Log("=== MANUALLY ENDING SONG ===");
            Debug.Log($"Current state: {_timingState}");
            Debug.Log($"Current song time: {_songTime:F3}s");
            Debug.Log($"Chart length: {_currentChart.TotalLength:F3}s");
            
            // Force completion by setting state and returning to main menu
            CompleteSong();
            
            Debug.Log("Song ended manually");
            Debug.Log("=== END MANUAL SONG END ===");
        }

        /// <summary>
        /// Updates the next judgable note index to point to the first unjudged note.
        /// This ensures the hit detection system always points to the correct note.
        /// </summary>
        private void UpdateNextJudgableNoteIndex()
        {
            // Find the first unjudged note
            for (int i = 0; i < _activeNotes.Count; i++)
            {
                if (_activeNotes[i] != null && !_activeNotes[i].IsHit)
                {
                    _nextJudgableNoteIndex = i;
                    return;
                }
            }
            
            // If all notes are judged, set index to -1 (no more notes to judge)
            _nextJudgableNoteIndex = -1;
        }

        /// <summary>
        /// Detects and fixes stuck notes that might be causing unhittable note issues.
        /// This method looks for notes that are past their hit window but haven't been processed.
        /// </summary>
        [ContextMenu("Detect and Fix Stuck Notes")]
        public void DetectAndFixStuckNotes()
        {
            Debug.Log("=== DETECTING STUCK NOTES ===");
            
            int stuckNotesFound = 0;
            float currentTime = GetSmoothedSongTime();
            
            for (int i = 0; i < _activeNotes.Count; i++)
            {
                var note = _activeNotes[i];
                if (note == null || !IsNoteValid(note)) continue;
                
                // Check if note is past hit window but not marked as hit
                if (!note.IsHit && currentTime > note.HitTime + MISS_WINDOW)
                {
                    stuckNotesFound++;
                    Debug.LogWarning($"Found stuck note at {note.HitTime}s (current time: {currentTime:F3}s, time past window: {currentTime - note.HitTime - MISS_WINDOW:F3}s)");
                    
                    // Mark as missed and apply effects
                    note.IsHit = true;
                    _combo = 0;
                    OnComboChange?.Invoke(_combo);
                    PlayMissEffect(_judgementCircle.position);
                }
            }
            
            if (stuckNotesFound > 0)
            {
                Debug.LogWarning($"Found and fixed {stuckNotesFound} stuck notes");
                
                // Update the next judgable note index
                UpdateNextJudgableNoteIndex();
            }
            else
            {
                Debug.Log("No stuck notes found");
            }
            
            Debug.Log("=== END STUCK NOTE DETECTION ===");
        }
        
        /// <summary>
        /// Checks if the song has been completed and triggers return to main menu.
        /// </summary>
        private void CheckForSongCompletion()
        {
            // Only check for completion if we're in the playing state and have a chart loaded
            if (_timingState != TimingState.Playing || _currentChart == null)
            {
                return;
            }
            
            // Check if all notes have been processed and we're past the last note's time
            if (_nextNoteIndex >= _notes.Count && _activeNotes.Count == 0)
            {
                // Check if we've reached the end of the song (with a small buffer)
                float currentTime = GetSmoothedSongTime();
                float songEndTime = _currentChart.TotalLength;
                
                if (currentTime >= songEndTime)
                {
                    CompleteSong();
                }
            }
        }
        
        /// <summary>
        /// Handles song completion by transitioning to the completed state and returning to main menu.
        /// </summary>
        private void CompleteSong()
        {
            if (_timingState == TimingState.Completed)
            {
                return; // Already completed
            }
            
            Debug.Log("=== SONG COMPLETED ===");
            Debug.Log($"Final Score: {_score:N0}");
            Debug.Log($"Final Accuracy: {GetAccuracy():F2}%");
            Debug.Log($"Max Combo: {_maxCombo:N0}");
            Debug.Log("========================");
            
            // Set state to completed
            _timingState = TimingState.Completed;
            
            // Return to main menu
            SceneManager.LoadScene("Menu");
        }
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
        public float Time;
        public float ScrollSpeed = 1f; 
        public NoteType Type;
    }
}
