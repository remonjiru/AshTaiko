# Pause Menu Setup Guide

This guide explains how to set up the pause menu system for AshTaiko.

## Overview

The pause menu system consists of several components:
- **PauseMenuManager**: Main controller for pause functionality
- **PauseMenuButtons**: Handles button interactions
- **ConfirmQuitPanel**: Confirmation dialog for quitting
- **GameManager Integration**: Pause state management
- **InputReader**: Pause input handling

## Setup Steps

### 1. Input System Configuration

The pause input action has been added to `GameInput.inputactions`:
- **Action Name**: Pause
- **Default Binding**: Escape key
- **Input Type**: Button

### 2. Scripts to Add

The following scripts have been created and should be attached to appropriate GameObjects:

#### PauseMenuManager
- **Purpose**: Main pause menu controller
- **Required References**:
  - `_pauseMenuPanel`: Main pause menu UI panel
  - `_pauseMenuButtons`: Container for pause menu buttons
  - `_confirmQuitPanel`: Confirmation quit dialog
  - `_gameManager`: Reference to GameManager instance

#### PauseMenuButtons
- **Purpose**: Handles pause menu button interactions
- **Required References**:
  - `_resumeButton`: Button to resume gameplay
  - `_mainMenuButton`: Button to return to main menu
  - `_quitButton`: Button to show quit confirmation
  - `_pauseMenuManager`: Reference to PauseMenuManager

#### ConfirmQuitPanel
- **Purpose**: Handles quit confirmation dialog
- **Required References**:
  - `_confirmQuitButton`: Button to confirm quitting
  - `_cancelButton`: Button to cancel and return to pause menu
  - `_pauseMenuManager`: Reference to PauseMenuManager

### 3. GameManager Configuration

The GameManager has been updated with:
- **InputReader Reference**: `_inputReader` field for pause input
- **PauseMenuManager Reference**: `_pauseMenuManager` field
- **Pause State Management**: `_isPaused` field and related methods
- **Pause Input Handling**: `OnPauseInput()` method

**Required Assignments in Inspector**:
- Assign the InputReader asset to `_inputReader`
- Assign the PauseMenuManager GameObject to `_pauseMenuManager`

### 4. UI Hierarchy Setup

Create the following UI hierarchy in your GameScene:

```
Canvas (or existing UI canvas)
├── PauseMenuPanel (GameObject with PauseMenuManager)
│   ├── PauseMenuButtons (GameObject with PauseMenuButtons)
│   │   ├── ResumeButton (Button)
│   │   ├── MainMenuButton (Button)
│   │   └── QuitButton (Button)
│   └── ConfirmQuitPanel (GameObject with ConfirmQuitPanel)
│       ├── ConfirmQuitButton (Button)
│       └── CancelButton (Button)
```

### 5. Component Assignment

#### PauseMenuManager GameObject
- Attach `PauseMenuManager` script
- Assign references in Inspector:
  - `_pauseMenuPanel` → Self (or parent panel)
  - `_pauseMenuButtons` → PauseMenuButtons GameObject
  - `_confirmQuitPanel` → ConfirmQuitPanel GameObject
  - `_gameManager` → GameManager instance

#### PauseMenuButtons GameObject
- Attach `PauseMenuButtons` script
- Assign references in Inspector:
  - `_resumeButton` → ResumeButton
  - `_mainMenuButton` → MainMenuButton
  - `_quitButton` → QuitButton
  - `_pauseMenuManager` → PauseMenuManager instance

#### ConfirmQuitPanel GameObject
- Attach `ConfirmQuitPanel` script
- Assign references in Inspector:
  - `_confirmQuitButton` → ConfirmQuitButton
  - `_cancelButton` → CancelButton
  - `_pauseMenuManager` → PauseMenuManager instance

### 6. InputReader Assignment

Ensure the InputReader asset is assigned to:
- **GameManager**: `_inputReader` field
- **Drum**: `_input` field (if not already assigned)

## Functionality

### Pause Input
- **Escape Key**: Toggles pause state
- **Game State**: Pauses gameplay, stops note spawning and movement
- **Time Scale**: Sets `Time.timeScale = 0` when paused

### Menu Navigation
- **Resume**: Returns to gameplay
- **Main Menu**: Returns to main menu scene
- **Quit**: Shows confirmation dialog
- **Confirm Quit**: Exits the game

### Pause State Management
- **GameManager**: Tracks pause state internally
- **PauseMenuManager**: Manages UI visibility and time scale
- **Input Handling**: Pause input is processed even when paused

## Testing

### Test Scenarios
1. **Pause During Gameplay**: Press Escape during song
2. **Resume Functionality**: Click Resume button
3. **Menu Navigation**: Test all button interactions
4. **Quit Confirmation**: Test quit flow
5. **Main Menu Return**: Test scene transition

### Expected Behavior
- Game pauses when Escape is pressed
- Notes stop moving and spawning
- Pause menu appears
- All buttons function correctly
- Game resumes normally when unpaused

## Troubleshooting

### Common Issues

#### Pause Not Working
- Check InputReader assignment in GameManager
- Verify PauseMenuManager reference
- Ensure InputReader has Pause action defined

#### UI Not Showing
- Check GameObject hierarchy
- Verify component assignments
- Check Canvas settings (Screen Space - Overlay recommended)

#### Game Not Pausing
- Check `_isPaused` field in GameManager
- Verify Update method pause check
- Check Time.timeScale changes

#### Button Clicks Not Working
- Verify button component assignments
- Check PauseMenuManager references
- Ensure scripts are properly attached

### Debug Information
- Check Console for error messages
- Verify all required fields are assigned in Inspector
- Test pause input in Input System Debugger

## Future Enhancements

### Potential Improvements
- **Pause Menu Animation**: Add fade/slide transitions
- **Background Blur**: Apply blur effect when paused
- **Audio Pause**: Pause background music
- **Save State**: Save game progress when pausing
- **Settings Integration**: Add options menu in pause

### Code Extensions
- **Pause Events**: Add events for pause state changes
- **Custom Pause Conditions**: Prevent pausing during certain game states
- **Pause Menu Themes**: Support for different visual styles
