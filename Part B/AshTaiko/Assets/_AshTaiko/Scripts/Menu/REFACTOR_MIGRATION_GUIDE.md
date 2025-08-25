# SongSelectionManager Refactor Migration Guide

## Overview

The original monolithic `SongSelectionManager` has been refactored into four focused, single-responsibility classes that work together to provide the same functionality with better maintainability and testability.

## New Class Structure

### 1. SongListManager
**Responsibility**: Manages the song list display, filtering, sorting, and selection
**Key Features**:
- Song list UI management
- Search and filter functionality
- Sorting by various criteria
- Song selection events

**Required UI References**:
- `_songListContent` (Transform) - Container for song list items
- `_songListItemPrefab` (GameObject) - Prefab for individual song items
- `_searchInput` (TMP_InputField) - Search input field
- `_sortDropdown` (TMP_Dropdown) - Sort type selection
- `_difficultyFilterDropdown` (TMP_Dropdown) - Difficulty filter

### 2. DifficultySelector
**Responsibility**: Manages difficulty button creation and interaction
**Key Features**:
- Dynamic difficulty button generation
- Visual state management (normal, selected, hover)
- Difficulty selection events

**Required UI References**:
- `_difficultyButtonContainer` (Transform) - Container for difficulty buttons
- `_difficultyButtonPrefab` (GameObject) - Prefab for difficulty buttons

### 3. SongInfoDisplay
**Responsibility**: Displays song metadata, statistics, and cover images
**Key Features**:
- Song information display
- BPM and timing calculations
- Cover image loading (from background images)
- Statistics display

**Required UI References**:
- `_songTitleText` (TextMeshProUGUI) - Song title display
- `_artistText` (TextMeshProUGUI) - Artist name display
- `_creatorText` (TextMeshProUGUI) - Chart creator display
- `_lengthText` (TextMeshProUGUI) - Song length display
- `_bpmText` (TextMeshProUGUI) - BPM information display
- `_songCoverImage` (Image) - Song cover image display

### 4. LeaderboardManager
**Responsibility**: Manages leaderboard display and score data
**Key Features**:
- Leaderboard entry creation
- Score display and formatting
- No-scores message handling

**Required UI References**:
- `_leaderboardContent` (Transform) - Container for leaderboard entries
- `_leaderboardEntryPrefab` (GameObject) - Prefab for leaderboard entries

### 5. SongSelectionManager (New)
**Responsibility**: Orchestrates all components and manages overall flow
**Key Features**:
- Component coordination
- Event handling
- Game scene transitions
- Selection state management

**Required UI References**:
- `_playButton` (Button) - Main play button
- `_backButton` (Button) - Back to menu button

## Migration Steps

### Step 1: Update Scene Hierarchy
1. Create separate GameObjects for each manager component
2. Attach the appropriate script to each GameObject
3. Assign all required UI references in the Inspector

### Step 2: Update Prefabs
1. Ensure `_songListItemPrefab` has:
   - TextMeshProUGUI for title
   - TextMeshProUGUI for artist  
   - TextMeshProUGUI for creator
   - Image for preview image
   - Button for interaction
2. Ensure `_difficultyButtonPrefab` has Button, TextMeshProUGUI, and Image components
3. Ensure `_leaderboardEntryPrefab` has TextMeshProUGUI components for rank, player name, score, and accuracy

### Step 3: Event System Setup
The new system uses C# events for communication:
- `SongListManager.OnSongSelected` → `SongSelectionManager.OnSongSelected`
- `DifficultySelector.OnDifficultySelected` → `SongSelectionManager.OnDifficultySelected`

### Step 4: Remove Old Script
1. Remove the old `SongSelectionManager` script from your scene
2. Replace it with the new `SongSelectionManager` script
3. Assign all component references

## Image Handling System

### Background Image Support
The new system automatically detects and uses background images from both osu! and TJA formats:

**osu! Format**: 
- Automatically detects background images from the `[Events]` section
- Supports multiple image formats: `.jpg`, `.jpeg`, `.png`, `.bmp`, `.gif`
- Images are referenced relative to the beatmap directory

**TJA Format**:
- Searches for common image files in the TJA directory
- Looks for files like `bg.jpg`, `cover.png`, `title.jpg`, etc.
- Provides fallback when no images are found

**Usage**:
```csharp
// Check if a song has any image available
if (song.HasImage())
{
    // Get the best available image path
    string imagePath = song.GetBestAvailableImage();
    // Load and display the image
}
```

## Benefits of the New System

### Testability
- Individual components can be tested in isolation
- Mock objects can be easily created for testing
- Event-driven architecture allows for easy unit testing

### Extensibility
- New features can be added to specific components without affecting others
- Components can be reused in different contexts
- Clear interfaces make integration easier

### Performance
- Better memory management with proper cleanup
- Reduced coupling between components
- More efficient event handling

## Example Usage

```csharp
// The new system automatically handles all coordination
// Just ensure all references are set in the Inspector

// To manually trigger a database refresh:
songSelectionManager.RefreshDatabase();

// To get current selection:
SongEntry selectedSong = songSelectionManager.GetSelectedSong();
ChartData selectedChart = songSelectionManager.GetSelectedChart();

// To clear selection:
songSelectionManager.ClearSelection();
```

## Troubleshooting

### Common Issues
1. **Missing References**: Ensure all UI references are assigned in the Inspector
2. **Event Not Firing**: Check that event handlers are properly connected in `SetupEventHandlers()`
3. **UI Not Updating**: Verify that component managers are properly initialized

### Debug Information
- All managers include validation in `Awake()` methods
- Check Console for error messages about missing references
- Use Unity's Inspector to verify component assignments

## Backward Compatibility

The new system maintains the same public interface as the original:
- `GetSelectedSong()`
- `GetSelectedChart()`
- `RefreshDatabase()`
- `ClearSelection()`

Existing code that calls these methods will continue to work without modification.

