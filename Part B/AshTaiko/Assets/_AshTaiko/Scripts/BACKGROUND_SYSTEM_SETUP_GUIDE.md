# Background System Setup Guide

## Overview

The Background System provides immersive gameplay experience by displaying chart-specific background images with adjustable dimming. This creates a more engaging visual environment while maintaining gameplay visibility.

## Features

- **Automatic Background Loading**: Reads background images from chart data
- **Adjustable Dimming**: Configurable dim level from 0% (no dim) to 100% (black)
- **Smart Scaling**: Fills screen while maintaining aspect ratio
- **Fallback Support**: Default background when no image is available
- **Runtime Controls**: Adjust dimming during gameplay

## Scene Setup

### 1. Background Image Setup

Create a UI Image component for the background:

```
Canvas (Screen Space - Overlay)
├── Background Container (Empty GameObject)
│   ├── Background Image (UI Image)
│   │   ├── Component: Image
│   │   ├── Raycast Target: false
│   │   ├── Source Image: None (set by script)
│   │   └── Color: White
│   └── Background Overlay (UI Image)
│       ├── Component: Image
│       ├── Raycast Target: false
│       ├── Source Image: None
│       └── Color: Black with Alpha 0.7
```

### 2. Background Controls Setup

Create UI controls for adjusting background settings:

```
Canvas (Screen Space - Overlay)
├── Background Controls Panel (Panel)
│   ├── Dim Slider (Slider)
│   │   ├── Min Value: 0
│   │   ├── Max Value: 1
│   │   └── Value: 0.7
│   ├── Dim Value Text (TextMeshPro)
│   │   └── Text: "Dim: 70%"
│   └── Toggle Background Button (Button)
│       └── Button Text: "Hide Background"
```

### 3. Component Assignment

#### GameManager
- **Background Image**: Assign the Background Image component
- **Background Overlay**: Assign the Background Overlay component
- **Background Dim**: Set default dim level (0.7 recommended)
- **Enable Background**: Check to enable background system

#### BackgroundControls
- **Dim Slider**: Assign the Dim Slider component
- **Dim Value Text**: Assign the Dim Value Text component
- **Toggle Background Button**: Assign the Toggle Background Button
- **Toggle Button Text**: Assign the Toggle Button Text component
- **Default Dim**: Set default dim level (0.7 recommended)

## How It Works

### 1. Background Loading
When a song starts:
1. **Chart Data Check**: Looks for background image in song data
2. **Image Loading**: Asynchronously loads the background image
3. **Smart Scaling**: Scales image to fill screen while maintaining aspect ratio
4. **Dimming Application**: Applies the configured dim level

### 2. Image Scaling Logic
- **Landscape Images**: Fit to height, crop excess width
- **Portrait Images**: Fit to width, crop excess height
- **Square Images**: Perfect fit with no cropping
- **Always Centered**: Image is centered within the screen

### 3. Dimming System
- **Overlay Method**: Uses a semi-transparent black overlay
- **Adjustable Range**: 0% (no dim) to 100% (completely black)
- **Real-time Updates**: Changes apply immediately during gameplay

## Configuration Options

### GameManager Settings
```csharp
[Header("Background System")]
[SerializeField] private UnityEngine.UI.Image _backgroundImage;
[SerializeField] private UnityEngine.UI.Image _backgroundOverlay;
[SerializeField] private float _backgroundDim = 0.7f; // 70% dim
[SerializeField] private bool _enableBackground = true;
```

### BackgroundControls Settings
```csharp
[Header("Settings")]
[SerializeField] private float _defaultDim = 0.7f;
[SerializeField] private bool _showControlsByDefault = true;
```

## API Methods

### GameManager Background Methods
```csharp
// Set background dim level (0.0 to 1.0)
public void SetBackgroundDim(float dimLevel)

// Get current background dim level
public float GetBackgroundDim()

// Enable/disable background system
public void SetBackgroundEnabled(bool enabled)

// Check if background system is enabled
public bool IsBackgroundEnabled()
```

### BackgroundControls Methods
```csharp
// Set dim level programmatically
public void SetDimLevel(float dimLevel)

// Get current dim level
public float GetDimLevel()

// Reset to default dim level
public void ResetToDefault()
```

## Testing

### Context Menu Testing
Right-click on GameManager in Inspector:
- **Test Background System**: Loads background for current song
- **Test Background Dim Levels**: Cycles through different dim levels
- **Test Background Scaling**: Tests background image scaling
- **Test Note Destruction Timing**: Debugs note destruction timing and logging

### Runtime Testing
1. **Start a song** with background image
2. **Adjust dim slider** to see real-time changes
3. **Toggle background** on/off
4. **Check console** for debug information

## Troubleshooting

### Common Issues

#### Background Not Loading
- **Check file paths**: Ensure background images exist in chart data
- **Verify image formats**: Supported formats: .jpg, .jpeg, .png, .bmp, .gif
- **Check console**: Look for "Background image file not found" messages

#### Dimming Not Working
- **Verify overlay assignment**: Ensure Background Overlay is assigned in GameManager
- **Check overlay color**: Overlay should be black with alpha > 0
- **Slider connection**: Ensure Dim Slider is connected to BackgroundControls

#### Image Scaling Issues
- **Check parent container**: Background Image should be in a full-screen container
- **Verify RectTransform**: Ensure proper anchoring and sizing
- **Console logs**: Check "Background configured" messages for scaling info

### Debug Information
The system provides comprehensive logging:
```
Loading background image: [path/to/image.jpg]
Background image loaded successfully: [path/to/image.jpg]
Background configured: texture 1920x1080 (aspect: 1.78), screen 1920x1080 (aspect: 1.78)
Background dim updated: 0.70
```

## Performance Considerations

### Memory Management
- **Texture cleanup**: Old background textures are automatically cleaned up
- **Sprite management**: New sprites are created for each background
- **Async loading**: Background loading doesn't block gameplay

### Optimization Tips
- **Image compression**: Use compressed image formats for large backgrounds
- **Resolution limits**: Consider maximum background resolution for performance
- **Lazy loading**: Backgrounds only load when songs start

## Customization

### Background Styles
- **Full screen**: Background fills entire screen
- **Aspect ratio preservation**: No stretching or distortion
- **Smart cropping**: Centers important image content

### Dimming Effects
- **Linear dimming**: Standard black overlay
- **Custom colors**: Modify overlay color for different effects
- **Gradient overlays**: Create more sophisticated dimming effects

## Integration with Existing Systems

### Song Selection
- **Automatic loading**: Background loads when song starts
- **Chart data integration**: Uses existing image loading system
- **Fallback handling**: Graceful degradation when no image available
- **Hover previews**: Background changes when hovering over songs during selection
- **Difficulty mode**: Background stays consistent when difficulty selector is active
- **Scene structure**: Song selection and main menu are in the same scene

### Sorting System
- **Multiple criteria**: Sort by title, artist, creator, length, difficulty, or date added
- **Sort order control**: Ascending or descending sort direction
- **Real-time updates**: Sorting changes apply immediately to the song list
- **Null-safe handling**: Gracefully handles missing or null song data
- **Performance optimized**: Efficient sorting algorithms for large song libraries

### Gameplay UI
- **Non-intrusive**: Background doesn't interfere with gameplay elements
- **Adjustable visibility**: Players can optimize for their preferences
- **Consistent behavior**: Works with all song types and difficulties

## Next Steps

Once the basic system is working:
1. **Add transition effects**: Fade between backgrounds
2. **Implement caching**: Store frequently used backgrounds
3. **Add visual effects**: Parallax, zoom, or other animations
4. **Custom themes**: Different background styles for different game modes
5. **Performance monitoring**: Track memory usage and loading times
