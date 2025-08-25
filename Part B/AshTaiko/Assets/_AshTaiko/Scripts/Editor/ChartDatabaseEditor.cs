using UnityEngine;
using UnityEditor;
using System.IO;

namespace AshTaiko.Editor
{
    [CustomEditor(typeof(ChartDatabase))]
    public class ChartDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            ChartDatabase chartDatabase = (ChartDatabase)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Database Management", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Song Database"))
            {
                CreateSongDatabase(chartDatabase);
            }
            
            if (GUILayout.Button("Scan for Songs"))
            {
                chartDatabase.ScanForSongs();
                EditorUtility.SetDirty(chartDatabase);
            }
            
            if (GUILayout.Button("Create Songs Directory"))
            {
                CreateSongsDirectory();
            }
            
            if (GUILayout.Button("Open Songs Directory"))
            {
                OpenSongsDirectory();
            }
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("🎵 Open Song Selection Editor"))
            {
                SongSelectionEditor.ShowWindow();
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "1. Click 'Create Song Database' to create a SongDatabase asset\n" +
                "2. Place your .osz, .osu, and .tja files in the Songs directory\n" +
                "3. Click 'Scan for Songs' to import them into the database\n" +
                "4. Use 'Open Song Selection Editor' for easy song/difficulty selection\n\n" +
                "Supported formats:\n" +
                "• .osz - Compressed osu! beatmap packages\n" +
                "• .osu - Individual osu! beatmap files\n" +
                "• .tja - Taiko no Tatsujin chart files", 
                MessageType.Info
            );
        }
        
        private void CreateSongDatabase(ChartDatabase chartDatabase)
        {
            // Create Resources directory if it doesn't exist
            string resourcesPath = Path.Combine(Application.dataPath, "_AshTaiko", "Resources");
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }
            
            // Check if database already exists
            string assetPath = "Assets/_AshTaiko/Resources/SongDatabase.asset";
            if (File.Exists(assetPath))
            {
                Debug.LogWarning("SongDatabase already exists at: " + assetPath);
                // Load and assign the existing database
                SongDatabase existingDatabase = AssetDatabase.LoadAssetAtPath<SongDatabase>(assetPath);
                chartDatabase.GetType()
                    .GetField("database", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(chartDatabase, existingDatabase);
                EditorUtility.SetDirty(chartDatabase);
                Selection.activeObject = existingDatabase;
                return;
            }
            
            // Create the SongDatabase asset
            SongDatabase database = ScriptableObject.CreateInstance<SongDatabase>();
            
            AssetDatabase.CreateAsset(database, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            // Assign it to the ChartDatabase
            chartDatabase.GetType()
                .GetField("database", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(chartDatabase, database);
            
            EditorUtility.SetDirty(chartDatabase);
            
            Debug.Log($"Created SongDatabase at: {assetPath}");
            
            // Select the created asset
            Selection.activeObject = database;
        }
        
        private void CreateSongsDirectory()
        {
            string songsPath = Path.Combine(Application.dataPath, "Songs");
            if (!Directory.Exists(songsPath))
            {
                Directory.CreateDirectory(songsPath);
                AssetDatabase.Refresh();
                Debug.Log($"Created Songs directory at: {songsPath}");
            }
            else
            {
                Debug.Log("Songs directory already exists");
            }
        }
        
        private void OpenSongsDirectory()
        {
            string songsPath = Path.Combine(Application.dataPath, "Songs");
            if (Directory.Exists(songsPath))
            {
                EditorUtility.RevealInFinder(songsPath);
            }
            else
            {
                Debug.LogWarning("Songs directory does not exist. Create it first.");
            }
        }
    }
    
    [CustomEditor(typeof(SongDatabase))]
    public class SongDatabaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            SongDatabase songDatabase = (SongDatabase)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Database Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Save Database"))
            {
                EditorUtility.SetDirty(songDatabase);
                AssetDatabase.SaveAssets();
                Debug.Log("Database saved");
            }
            
            if (GUILayout.Button("Clear Database"))
            {
                if (EditorUtility.DisplayDialog("Clear Database", 
                    "Are you sure you want to clear all songs from the database?", 
                    "Yes", "No"))
                {
                    // Clear the database using the proper method
                    songDatabase.ClearDatabase();
                    EditorUtility.SetDirty(songDatabase);
                    AssetDatabase.SaveAssets();
                    Debug.Log("Database cleared and saved");
                }
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "This is the main song database. Songs are automatically imported here " +
                "when scanning for new files.", 
                MessageType.Info
            );
            
            // Show current song count
            EditorGUILayout.LabelField($"Current songs in database: {songDatabase.GetSongCount()}");
        }
        
        [ContextMenu("Clear Database")]
        private void ClearDatabaseFromContextMenu()
        {
            SongDatabase songDatabase = (SongDatabase)target;
            songDatabase.ClearDatabase();
            EditorUtility.SetDirty(songDatabase);
            AssetDatabase.SaveAssets();
            Debug.Log("Database cleared from context menu");
        }
    }
    
    public static class SongDatabaseMenu
    {
        [MenuItem("AshTaiko/Create Song Database")]
        public static void CreateSongDatabaseFromMenu()
        {
            // Create Resources directory if it doesn't exist
            string resourcesPath = Path.Combine(Application.dataPath, "_AshTaiko", "Resources");
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }
            
            // Check if database already exists
            string assetPath = "Assets/_AshTaiko/Resources/SongDatabase.asset";
            if (File.Exists(assetPath))
            {
                Debug.LogWarning("SongDatabase already exists at: " + assetPath);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<SongDatabase>(assetPath);
                return;
            }
            
            // Create the SongDatabase asset
            SongDatabase database = ScriptableObject.CreateInstance<SongDatabase>();
            
            AssetDatabase.CreateAsset(database, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Created SongDatabase at: {assetPath}");
            
            // Select the created asset
            Selection.activeObject = database;
        }
    }
}