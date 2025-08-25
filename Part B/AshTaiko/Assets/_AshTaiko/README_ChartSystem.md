# AshTaiko Chart/Song Database System

This system provides a comprehensive chart and song management solution for your Taiko no Tatsujin-style rhythm game, supporting both osu! and TJA file formats.

## Features

- **Multi-format Support**: Import songs from osu! (.osu) and TJA (.tja) files
- **Song Database**: Persistent storage of song metadata and chart data
- **Difficulty Management**: Support for multiple difficulty levels per song
- **Search and Filter**: Find songs by title, artist, creator, or tags
- **Song Selection UI**: Intuitive interface for browsing and selecting songs
- **Automatic Scanning**: Automatically detect and import new song files

## File Structure

The system follows a structure similar to osu!:

```
Assets/
├── Songs/                    # Place your .osu and .tja files here
│   ├── Song1/
│   │   ├── song1.osu
│   │   ├── audio.mp3
│   │   └── background.jpg
│   └── Song2/
│       ├── song2.tja
│       └── audio.wav
├── _AshTaiko/
│   ├── Scripts/
│   │   ├── ChartDatabase.cs          # Main database manager
│   │   ├── OsuImporter.cs            # osu! file parser
│   │   ├── TjaImporter.cs            # TJA file parser
│   │   └── Menu/
│   │       ├── SongSelectionManager.cs    # Song selection UI
│   │       └── SongListItem.cs            # Individual song display
│   └── Resources/
│       └── SongDatabase.asset        # Persistent song database
```

## Setup Instructions

### 1. Create the Songs Directory

1. In Unity, select the `ChartDatabase` component in your scene
2. Click "Create Songs Directory" in the inspector
3. This creates an `Assets/Songs` folder where you'll place your song files

### 2. Add Song Files

- **osu! files**: Place `.osu` files along with their associated audio and image files
- **TJA files**: Place `.tja` files along with their associated audio files
- Organize files in subdirectories for better management

### 3. Import Songs

1. Select the `ChartDatabase` component
2. Click "Scan for Songs" to automatically import all supported files
3. The system will parse the files and add them to the database

### 4. Use in Your Game

1. Add the `SongSelectionManager` to your song selection scene
2. Configure the UI references in the inspector
3. Use the `GameManager.StartGame(song, chart)` method to start playing

## Supported File Formats

### osu! Format (.osu)

The system parses the following osu! sections:
- `[General]`: Audio filename, preview time
- `[Metadata]`: Title, artist, creator, source, tags, version
- `[Difficulty]`: HP, circle size, overall difficulty, approach rate
- `[Events]`: Background images
- `[TimingPoints]`: BPM and timing information
- `[HitObjects]`: Note placement and types

**Note**: Currently optimized for Taiko mode charts.

### TJA Format (.tja)

The system parses the following TJA sections:
- `TITLE`: Song title
- `ARTIST`: Song artist
- `CREATOR`: Chart creator
- `SOURCE`: Source material
- `BPM`: Base BPM
- `WAVE`: Audio file
- `COURSE`: Difficulty level
- `LEVEL`: Difficulty rating
- `NOTES`: Note placement data

## API Reference

### ChartDatabase

```csharp
// Get all songs
IReadOnlyList<SongEntry> songs = ChartDatabase.Instance.Songs;

// Search for songs
List<SongEntry> results = ChartDatabase.Instance.SearchSongs("query");

// Get songs by difficulty
List<SongEntry> hardSongs = ChartDatabase.Instance.GetSongsByDifficulty(Difficulty.Hard);

// Scan for new songs
ChartDatabase.Instance.ScanForSongs();
```

### SongEntry

```csharp
// Get chart by difficulty
ChartData chart = song.GetChart(Difficulty.Hard);

// Get chart by version name
ChartData chart = song.GetChart("Insane");

// Access song metadata
string title = song.Title;
string artist = song.Artist;
string creator = song.Creator;
```

### GameManager

```csharp
// Start a game with a specific song and chart
GameManager.Instance.StartGame(songEntry, chartData);

// Get current song/chart
SongEntry currentSong = GameManager.Instance.GetCurrentSong();
ChartData currentChart = GameManager.Instance.GetCurrentChart();
```

## UI Components

### SongSelectionManager

Manages the main song selection interface:
- Song list display
- Search functionality
- Difficulty filtering
- Song preview information
- Navigation controls

### SongListItem

Individual song display component:
- Song metadata display
- Visual feedback for selection
- Click handling

## Customization

### Adding New File Formats

1. Create a new importer class implementing the same interface as `OsuImporter`
2. Add the new format to the `ChartDatabase.ScanForSongs()` method
3. Update the file extension filtering

### Extending Song Metadata

1. Add new fields to the `SongEntry` class
2. Update the importers to parse the new data
3. Modify the UI components to display the new information

### Custom Difficulty Levels

1. Add new values to the `Difficulty` enum
2. Update the difficulty parsing logic in importers
3. Modify the UI to handle the new difficulties

## Troubleshooting

### Songs Not Importing

- Check that files are in the correct `Assets/Songs` directory
- Verify file extensions are `.osu` or `.tja`
- Check the console for import error messages
- Ensure audio files referenced in charts exist

### Audio Not Playing

- Verify audio file paths are correct
- Check that audio files are in supported formats (MP3, WAV, OGG)
- Ensure the `SongManager` component is properly configured

### UI Not Displaying

- Check that all UI references are assigned in the inspector
- Verify the `SongSelectionManager` is active in the scene
- Ensure the `ChartDatabase` instance exists

## Performance Considerations

- Large song databases may impact loading times
- Consider implementing pagination for very large song lists
- Audio file loading is asynchronous to prevent frame drops
- Chart data is cached in memory for fast access during gameplay

## Future Enhancements

- **Online Database**: Sync with online song repositories
- **Chart Editor**: Built-in chart creation and editing tools
- **Performance Metrics**: Track song play statistics and rankings
- **Custom Skins**: Support for custom note and UI skins
- **Multiplayer**: Synchronized multiplayer chart playback
