# GameManager Class - Internal Documentation

## Overview
The GameManager class is the central orchestrator for the AshTaiko rhythm game. It manages game state, timing synchronization, note spawning, hit detection, scoring, and the overall game flow. This class implements a complex timing system that synchronizes audio playback with visual note movement and player input.

## Architecture & Design Patterns

### Singleton Pattern
```csharp
public static GameManager Instance { get; private set; }
```
- **Why Used**: Provides global access to game state and methods from anywhere in the codebase
- **Implementation**: Set in Awake() method, ensures only one instance exists per scene
- **Trade-offs**: While convenient for global access, this creates tight coupling. Consider dependency injection for better testability

### Event-Driven Architecture
```csharp
public event UnityAction<int> OnScoreChange;
public event UnityAction<int> OnComboChange;
public event UnityAction<float> OnAccuracyChange;
public event UnityAction<Note> OnNoteHit;
public event UnityAction<SongEntry, ChartData> OnSongChanged;
```
- **Why Used**: Decouples UI updates from game logic, allows multiple systems to react to game state changes
- **Data Type Choice**: UnityAction<T> provides type-safe event handling with Unity's event system
- **Usage Pattern**: UI components subscribe to these events to update displays in real-time

## Core Data Structures

### 1. Timing System State Machine
```csharp
private enum TimingState
{
    Uninitialized,    // No audio loaded yet
    Loading,          // Audio is loading
    Delay,            // Audio loaded, waiting for 3s delay
    Playing           // Audio is playing normally
}
```

**Design Rationale**:
- **Enum Choice**: Provides type safety and prevents invalid state transitions
- **State Names**: Self-documenting names that clearly indicate what each state represents
- **State Flow**: Linear progression from Uninitialized → Loading → Delay → Playing

**Why This Approach**:
- **Audio Synchronization**: The 3-second delay allows notes to spawn and travel before audio starts
- **Player Experience**: Players can see notes approaching before the music begins
- **Timing Accuracy**: DSP (Digital Signal Processing) time provides frame-rate independent timing

### 2. Note Management System
```csharp
[SerializeField] private List<HitObject> _notes = new List<HitObject>();
private int _nextNoteIndex = 0;
private List<Note> _activeNotes = new List<Note>();
private int _nextJudgableNoteIndex = 0;
```

**Data Structure Choices**:
- **List<HitObject>**: Stores chart data (immutable during gameplay)
- **List<Note>**: Stores active GameObjects that are currently moving on screen
- **Index Tracking**: Separate indices for spawning vs. judging to handle different timing requirements

**Why Separate Lists**:
- **Performance**: Chart data doesn't change during gameplay, active notes do
- **Memory Management**: Active notes can be destroyed/cleaned up independently
- **Timing Separation**: Spawning and judging happen at different rates

### 3. Drumroll State Management
```csharp
private bool _isInDrumroll = false;
private HitObject _currentDrumroll = null;
private float _drumrollStartTime = 0f;
private float _drumrollEndTime = 0f;
private int _drumrollHits = 0;
private float _drumrollHitWindow = 0.15f;
private List<DrumrollBridge> _activeDrumrollBridges = new List<DrumrollBridge>();
```

**Design Rationale**:
- **Boolean Flag**: Simple state tracking for drumroll mode
- **Time Tracking**: Precise timing for drumroll start/end detection
- **Hit Counting**: Accumulates hits during drumroll for scoring
- **Bridge Management**: Visual connection between drumroll start and end notes

## Data Types & Precision

### 1. Time Representation
```csharp
private float _songTime;
private double _delayStartDspTime;
private double _lastDspTime;
private double _currentDspTime;
```

**Type Choices**:
- **float for _songTime**: Sufficient precision for gameplay timing (millisecond accuracy)
- **double for DSP time**: Higher precision needed for audio synchronization
- **Why double for DSP**: Audio systems require sub-millisecond precision to avoid drift

**Precision Requirements**:
- **Gameplay Timing**: 42ms (GOOD_WINDOW) requires ~1ms precision
- **Audio Sync**: Sub-millisecond precision prevents cumulative timing errors
- **Note Spawning**: 2-second preempt time with 3-second delay requires accurate calculations

### 2. Scoring System
```csharp
private int _score = 0;
private int _combo = 0;
private float _gauge = 0f;
private const float _maxGauge = 100f;
```

**Type Choices**:
- **int for score/combo**: Whole numbers, no fractional scoring
- **float for gauge**: Smooth visual updates and precise calculations
- **Constants**: Magic numbers defined as constants for easy tuning

**Scoring Logic**:
- **Good Hit**: 100 points + combo increment + 2.5 gauge
- **Okay Hit**: 50 points + combo increment + 1.25 gauge  
- **Miss**: 0 points + combo reset + -2.5 gauge
- **Drumroll Hits**: 10 points each + combo increment

### 3. Judgement Windows
```csharp
private const float MISS_WINDOW = 0.125f;  // 125ms
private const float OKAY_WINDOW = 0.108f;  // 108ms
private const float GOOD_WINDOW = 0.042f;  // 42ms
```

**Timing Values**:
- **Based on osu! Taiko**: Industry standard rhythm game timing windows
- **Progressive Difficulty**: Good requires precise timing, Miss allows more leniency
- **Human Factors**: 42ms represents typical human reaction time for rhythm games

## Memory Management & Performance

### 1. Note Cleanup System
```csharp
private void CleanupDestroyedNotes()
{
    _activeNotes.RemoveAll(note => 
        note == null || 
        note.gameObject == null || 
        !note.enabled || 
        !note.gameObject.activeInHierarchy
    );
}
```

**Why This Approach**:
- **Null Safety**: Prevents crashes from destroyed GameObjects
- **Memory Leaks**: Removes references to destroyed objects
- **Performance**: Periodic cleanup (every 15 frames) balances performance vs. memory
- **Index Management**: Adjusts indices after cleanup to maintain consistency

### 2. List vs. Array Considerations
```csharp
private List<HitObject> _notes = new List<HitObject>();
private List<Note> _activeNotes = new List<Note>();
```

**Why Lists**:
- **Dynamic Sizing**: Notes can be added/removed during gameplay
- **Memory Efficiency**: Only allocates memory for actual notes
- **Unity Integration**: Better integration with Unity's serialization system
- **Performance**: Modern .NET Lists have minimal overhead for small collections

**Alternative Considerations**:
- **Arrays**: Could be used for _notes if chart size is fixed
- **Object Pooling**: Could improve performance for frequently created/destroyed notes
- **Linked Lists**: Could be more efficient for frequent insertions/deletions

## Input Handling & Hit Detection

### 1. Hit Type Matching
```csharp
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
            return hitType == HitType.Don || hitType == HitType.Ka;
        default:
            return false;
    }
}
```

**Design Rationale**:
- **Type Safety**: Prevents invalid input combinations
- **Flexibility**: Drumrolls accept both Don and Ka inputs
- **Extensibility**: Easy to add new note types and input mappings
- **Performance**: Switch statement is efficient for small type sets

### 2. Hit Window Calculation
```csharp
if (Mathf.Abs(currentTime - note.hitTime) <= hitWindow)
{
    // Note is within hit window
    Judgement judgement = GetJudgement(Mathf.Abs(currentTime - note.hitTime));
}
```

**Why Absolute Value**:
- **Bidirectional Timing**: Accepts both early and late hits
- **Player Experience**: More forgiving than one-directional timing
- **Standard Practice**: Matches industry rhythm game conventions

## Audio Synchronization System

### 1. DSP Time Integration
```csharp
public float GetSynchronizedSongTime()
{
    if (_timingState == TimingState.Delay)
    {
        double currentDspTime = AudioSettings.dspTime;
        double timeSinceDelayStart = currentDspTime - _delayStartDspTime;
        float synchronizedTime = -3f + (float)timeSinceDelayStart;
        return synchronizedTime;
    }
}
```

**Why DSP Time**:
- **Frame Rate Independence**: Not affected by frame rate fluctuations
- **Audio Accuracy**: Direct integration with Unity's audio system
- **Precision**: Sub-millisecond accuracy prevents timing drift
- **Synchronization**: Ensures audio and visual elements stay aligned

### 2. Negative Time System
```csharp
_songTime = -3f; // Start in delay period
```

**Design Rationale**:
- **Visual Countdown**: Players see negative time counting up to zero
- **Note Spawning**: Notes spawn early enough to be visible during delay
- **Smooth Transition**: Continuous timing from delay to playing state
- **Debugging**: Clear indication of timing system state

## Chart System Integration

### 1. Song and Chart Data
```csharp
private SongEntry _currentSong;
private ChartData _currentChart;
```

**Data Structure Choice**:
- **SongEntry**: Contains metadata (title, artist, audio file)
- **ChartData**: Contains gameplay data (notes, timing points, difficulty)
- **Separation of Concerns**: Metadata vs. gameplay data are logically separate
- **Reusability**: Multiple charts can share the same song metadata

### 2. Chart Loading Process
```csharp
private void LoadChart(ChartData chart)
{
    _notes.Clear();
    _notes.AddRange(chart.HitObjects);
    _notes.Sort((a, b) => a.Time.CompareTo(b.Time));
    // ... initialization code
}
```

**Why This Approach**:
- **Data Validation**: Ensures notes are properly sorted by time
- **Memory Management**: Clears old data before loading new chart
- **Performance**: Sorted list enables efficient note spawning logic
- **State Reset**: Properly initializes all game systems for new chart

## Error Handling & Debugging

### 1. Context Menu Methods
```csharp
[ContextMenu("Check Timing Status")]
[ContextMenu("Check Note System Health")]
[ContextMenu("Reset Timing System")]
```

**Why Context Menus**:
- **Runtime Debugging**: Can be accessed during gameplay
- **No Code Changes**: Debugging without modifying source
- **Immediate Feedback**: Real-time system status information
- **Developer Experience**: Easy access to debugging tools

### 2. Comprehensive Logging
```csharp
Debug.Log($"Timing: songTime={_songTime:F3}s, nextNote={nextNoteTime}s, timeUntil={timeUntilNote:F3}s");
```

**Logging Strategy**:
- **Precision**: 3 decimal places for timing (millisecond accuracy)
- **Context**: Always include relevant state information
- **Performance**: Conditional logging to avoid spam in production
- **Debugging**: Sufficient detail to diagnose timing issues

## Performance Considerations

### 1. Frame-Based Operations
```csharp
if (Time.frameCount % 15 == 0) // Every 15 frames
{
    CleanupDestroyedNotes();
}
```

**Why Frame-Based**:
- **Performance**: Avoids expensive operations every frame
- **Balance**: Sufficient frequency for cleanup without performance impact
- **Predictability**: Consistent performance characteristics
- **Tunable**: Easy to adjust frequency based on performance requirements

### 2. Early Exit Conditions
```csharp
while (_nextNoteIndex < _notes.Count)
{
    float nextNoteTime = _notes[_nextNoteIndex].Time;
    float timeUntilNote = nextNoteTime - _songTime;
    
    if (timeUntilNote <= effectivePreemptTime)
    {
        SpawnNote(_notes[_nextNoteIndex]);
        _nextNoteIndex++;
    }
    else
    {
        break; // Note is too far in the future
    }
}
```

**Performance Benefits**:
- **Eliminates Unnecessary Iterations**: Breaks early when notes are too far ahead
- **Predictable Performance**: O(1) average case for note spawning
- **Scalability**: Performance doesn't degrade with chart size
- **Real-time Requirements**: Maintains consistent frame rate

## Future Improvements & Refactoring

### 1. Code Organization
- **Current State**: Single large class with multiple responsibilities
- **Recommended**: Split into focused classes (TimingManager, NoteManager, ScoreManager)
- **Benefits**: Better testability, maintainability, and single responsibility principle

### 2. Data Structure Optimizations
- **Object Pooling**: For frequently created/destroyed notes
- **Spatial Partitioning**: For note collision detection
- **Event Queuing**: For better performance with many simultaneous events

### 3. Configuration System
- **Current**: Hard-coded constants throughout the class
- **Recommended**: ScriptableObject-based configuration
- **Benefits**: Easy tuning without code changes, designer-friendly

## Conclusion

The GameManager class demonstrates a sophisticated approach to rhythm game development with careful attention to timing precision, performance optimization, and user experience. The use of DSP time, state machines, and event-driven architecture creates a robust foundation for rhythm gameplay. While the current implementation works well, the class would benefit from refactoring into smaller, more focused components to improve maintainability and testability.

The design choices reflect a deep understanding of rhythm game requirements, particularly the need for precise audio-visual synchronization and responsive player input handling. The extensive debugging and context menu support shows good development practices for complex timing systems.
