# Song Selection System Setup Guide

## Overview
This guide will help you set up the complete song selection system with difficulty selection and game launching functionality.

**New Behavior**: The difficulty selection panel now only appears when a song is clicked, and the play button is integrated into the difficulty selection window for better UX flow.

## Prerequisites
1. **Song Database**: Make sure you have songs imported into your database
2. **Game Scene**: Create a scene named "GameScene" for gameplay
3. **Main Menu Scene**: Create a scene named "MainMenu" for navigation

## Scene Setup

### 1. Song Selection Scene
Create a new scene or use your existing song selection scene with the following hierarchy:

```
SongSelectionScene
├── Canvas
│   ├── SongSelectionManager (GameObject)
│   │   ├── SongSelectionManager (Script)
│   │   ├── SongListManager (Script)
│   │   ├── DifficultySelector (Script)
│   │   ├── SongInfoDisplay (Script)
│   │   └── LeaderboardManager (Script)
│   ├── Song List Panel
│   │   ├── Search Input
│   │   ├── Sort Dropdown
│   │   ├── Difficulty Filter Dropdown
│   │   └── Song List Content (ScrollRect)
│   ├── Difficulty Selection Panel
│   │   ├── Difficulty Button Container
│   │   ├── Play Button
│   │   └── Back Button
│   ├── Song Info Panel
│   │   ├── Song Title Text
│   │   ├── Artist Text
│   │   ├── Creator Text
│   │   ├── Length Text
│   │   ├── BPM Text
│   │   ├── Format Text
│   │   └── Cover Image
│   ├── Leaderboard Panel
│   │   └── Leaderboard Content
│   ├── Control Panel
│   │   └── Back Button
│   └── Debug Panel (Optional)
│       ├── Test Song Selection Button
│       ├── Test Difficulty Selection Button
│       └── Test Play Button
```

### 2. Component Assignment

#### SongSelectionManager
- **SongListManager**: Assign the SongListManager component
- **DifficultySelector**: Assign the DifficultySelector component
- **SongInfoDisplay**: Assign the SongInfoDisplay component
- **LeaderboardManager**: Assign the LeaderboardManager component
- **Back Button**: Assign your back button

#### SongListManager
- **Song List Content**: Assign the Transform containing song list items
- **Song List Item Prefab**: Assign your song list item prefab
- **Search Input**: Assign your search input field
- **Sort Dropdown**: Assign your sort dropdown
- **Difficulty Filter Dropdown**: Assign your difficulty filter dropdown

#### DifficultySelector
- **Difficulty Button Container**: Assign the Transform containing difficulty buttons
- **Difficulty Button Prefab**: Assign your difficulty button prefab
- **Difficulty Panel**: Assign the GameObject containing the entire difficulty selection panel
- **Play Button**: Assign the play button that's integrated into the difficulty panel
- **Back Button**: Assign the back button to close the difficulty panel

#### SongInfoDisplay
- **Song Title Text**: Assign TextMeshProUGUI for title
- **Artist Text**: Assign TextMeshProUGUI for artist
- **Creator Text**: Assign TextMeshProUGUI for creator
- **Length Text**: Assign TextMeshProUGUI for length
- **BPM Text**: Assign TextMeshProUGUI for BPM
- **Format Text**: Assign TextMeshProUGUI for format
- **Cover Image**: Assign Image component for cover

#### LeaderboardManager
- **Leaderboard Content**: Assign the Transform containing leaderboard entries
- **Leaderboard Entry Prefab**: Assign your leaderboard entry prefab

## Prefab Requirements

### 1. Song List Item Prefab
Must contain:
- **Button** component for interaction
- **TextMeshProUGUI** for title
- **TextMeshProUGUI** for artist
- **TextMeshProUGUI** for creator
- **TextMeshProUGUI** for format
- **Image** for preview image
- **SongListItem** script

### 2. Difficulty Button Prefab
Must contain:
- **Button** component for interaction
- **TextMeshProUGUI** for button text
- **Image** for background (optional)

### 3. Leaderboard Entry Prefab
Must contain:
- **TextMeshProUGUI** components for rank, player name, score, and accuracy

## Build Settings Configuration

### 1. Add Scenes to Build
1. Go to **File > Build Settings**
2. Add the following scenes in order:
   - MainMenu
   - SongSelectionScene
   - GameScene

### 2. Scene Names
Make sure your scenes are named exactly:
- **MainMenu** (for main menu)
- **GameScene** (for gameplay)

## Testing the System

### 1. Basic Functionality Test
1. **Start the Song Selection Scene**
2. **Check Console Logs** for initialization messages
3. **Verify Song List** appears with imported songs
4. **Click on a Song** - should see difficulty selection panel appear with difficulty buttons
5. **Click on a Difficulty** - should see song info update and play button become active
6. **Click Play Button** (in difficulty panel) - should transition to game scene
7. **Click Back Button** (in difficulty panel) - should close difficulty panel while keeping song selected

### 2. Debug Helper (Optional)
Add the `SongSelectionDebugHelper` script to your scene for additional debugging:
- **Test Song Selection Button**: Check current song selection
- **Test Difficulty Selection Button**: Check available charts
- **Test Play Button**: Verify play readiness

### 3. Console Logs to Watch For
```
Song selected: [Title] - [Artist] (Charts: X)
Creating X difficulty buttons for song: [Title]
Created difficulty button: [Version] ([Difficulty])
Auto-selecting first difficulty: [Version] ([Difficulty])
Difficulty panel shown
Difficulty selected: [Version] ([Difficulty]) for song: [Title]
Play button clicked in difficulty panel: [Title] - [Version]
Starting game with: [Title] - [Version] ([Difficulty])
=== LOADING SONG INTO GAME ===
Selected song: [Title] - [Artist]
Selected chart: [Version] - [Difficulty]
Audio filename: [path/to/audio.mp3]
=== GAMEDATAMANAGER: SETTING SONG DATA ===
Song: [Title] - [Artist]
Chart: [Version] - [Difficulty]
Audio: [path/to/audio.mp3]
GameDataManager: Song data set successfully - [Title] - [Version]
=== END SETTING SONG DATA ===
Song data stored for scene transition: [Title] - [Version]
Loading game scene with: [Title] - [Version]
GameManager initialized.
=== CHECKING FOR SONG DATA FROM MENU ===
GameDataManager.HasSongData(): True
Song from GameDataManager: [Title] - [Artist]
Chart from GameDataManager: [Version] - [Difficulty]
Audio filename: [path/to/audio.mp3]
Starting game with song data from menu: [Title] - [Version] ([Difficulty])
=== MANUAL SONG SELECTION ===
Song: [Title] - [Artist]
Chart: [Version] ([Difficulty])
Notes: X
Audio: [path/to/audio.mp3]
=========================================
Auto-loading audio from chart: [path/to/audio.mp3]
Audio file exists: True
Difficulty back button clicked - closing difficulty panel (selection preserved)
```

## Common Issues and Solutions

### 1. "SongSelectionManager: [Component] reference is missing!"
**Solution**: Assign the missing component reference in the Inspector

### 2. "GameManager instance not found!"
**Solution**: Make sure GameManager is in your GameScene

### 3. "GameScene not found in build settings"
**Solution**: Add GameScene to Build Settings > Scenes in Build

### 4. Difficulty buttons not appearing
**Solution**: Check that:
- DifficultySelector has Difficulty Button Container assigned
- DifficultySelector has Difficulty Button Prefab assigned
- Songs have charts in the database

### 5. Play button not working
**Solution**: Check that:
- Both song and difficulty are selected
- Play button is assigned to DifficultySelector component
- DifficultySelector has OnPlayButtonClicked event connected to SongSelectionManager
- GameManager is in the scene

### 6. SongSelectionEditor Interfering with Gameplay
**Problem**: SongSelectionEditor changes song selection during gameplay
**Solution**: SongSelectionEditor GameManager integration has been disabled to prevent conflicts with the new scene transition system

### 7. Audio Not Loading
**Problem**: Audio files not found or not loading
**Solution**: 
1. Check that audio files exist in the paths specified in song data
2. Verify audio file paths are relative to the Songs directory
3. Ensure audio files are in supported formats (.mp3, .ogg, .wav)
4. Check Console for "Audio file exists: False" messages

### 8. Scene Transition Not Working
**Problem**: Game scene loads but song data is not passed
**Solution**:
1. Verify "GameScene" is added to Build Settings > Scenes in Build
2. Check Console for GameDataManager debug messages
3. Ensure song and chart selection is complete before clicking play

### 9. Game Starts with Wrong Song
**Problem**: Game starts with different song than selected
**Solution**:
1. SongSelectionEditor interference has been disabled
2. Use only the song selection screen in Menu scene for gameplay
3. SongSelectionEditor is now only for testing within the same scene

## Advanced Configuration

### 1. Custom Scene Names
If you want to use different scene names, modify the `LoadGameScene()` and `LoadMainMenuScene()` methods in `SongSelectionManager.cs`.

### 2. Transition Effects
Add transition effects by modifying the scene loading methods to include fade transitions or loading screens.

### 3. Persistent Data
The system automatically persists song and chart selection during the session. For cross-session persistence, implement save/load functionality.

## Performance Tips

1. **Limit Song List Items**: Use object pooling for large song lists
2. **Lazy Load Images**: Load cover images only when needed
3. **Efficient Filtering**: Use efficient search algorithms for large databases
4. **UI Batching**: Group UI updates to minimize draw calls

## Troubleshooting Checklist

- [ ] All component references assigned in Inspector
- [ ] Scenes added to Build Settings
- [ ] Scene names match exactly (case-sensitive)
- [ ] Song database has imported songs with charts
- [ ] Prefabs have required components
- [ ] Console shows no errors during initialization
- [ ] GameManager exists in GameScene
- [ ] DifficultySelector Play Button assigned
- [ ] DifficultySelector Difficulty Panel assigned
- [ ] DifficultySelector Back Button assigned
- [ ] Main Back Button OnClick event properly connected

## Next Steps

Once the basic system is working:
1. **Add Visual Polish**: Improve button animations and transitions
2. **Implement Search**: Add real-time search functionality
3. **Add Sorting**: Implement multiple sorting options
4. **Enhance UI**: Add progress bars, loading indicators
5. **Add Sound**: Include button click sounds and music previews

## Support

If you encounter issues:
1. Check the Console for error messages
2. Verify all component references are assigned
3. Use the debug helper to isolate problems
4. Check that scenes are properly named and added to build settings
