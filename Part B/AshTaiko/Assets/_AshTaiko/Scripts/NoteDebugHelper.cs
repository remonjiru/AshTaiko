using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Helper component for debugging note system issues.
    /// Provides easy access to note system health checks and fixes.
    /// </summary>
    public class NoteDebugHelper : MonoBehaviour
    {
        [Header("Debug Controls")]
        [SerializeField] private bool _enableAutoHealthChecks = true;
        [SerializeField] private float _healthCheckInterval = 5f; // seconds
        
        private float _lastHealthCheckTime = 0f;
        
        private void Update()
        {
            if (_enableAutoHealthChecks && Time.time - _lastHealthCheckTime > _healthCheckInterval)
            {
                _lastHealthCheckTime = Time.time;
                
                // Perform automatic health check
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckNoteSystemHealth();
                }
            }
        }
        
        [ContextMenu("Check Note System Health")]
        private void CheckNoteSystemHealth()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CheckNoteSystemHealth();
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null - cannot check note system health");
            }
        }
        
        [ContextMenu("Auto-Fix Note System")]
        private void AutoFixNoteSystem()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AutoFixNoteSystem();
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null - cannot auto-fix note system");
            }
        }
        
        [ContextMenu("Reset Note System")]
        private void ResetNoteSystem()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetNoteSystem();
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null - cannot reset note system");
            }
        }
        
        [ContextMenu("Force Cleanup All Notes")]
        private void ForceCleanupAllNotes()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ForceCleanupAllNotes();
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null - cannot force cleanup notes");
            }
        }
        
        [ContextMenu("Detect and Fix Stuck Notes")]
        private void DetectAndFixStuckNotes()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DetectAndFixStuckNotes();
            }
            else
            {
                Debug.LogWarning("GameManager.Instance is null - cannot detect stuck notes");
            }
        }
        
        private void OnGUI()
        {
            if (GameManager.Instance == null) return;
            
            // Draw debug controls on screen
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Note System Debug Controls", GUI.skin.box);
            
            if (GUILayout.Button("Check Health"))
            {
                CheckNoteSystemHealth();
            }
            
            if (GUILayout.Button("Auto-Fix"))
            {
                AutoFixNoteSystem();
            }
            
            if (GUILayout.Button("Reset System"))
            {
                ResetNoteSystem();
            }
            
            if (GUILayout.Button("Force Cleanup"))
            {
                ForceCleanupAllNotes();
            }
            
            if (GUILayout.Button("Detect Stuck Notes"))
            {
                DetectAndFixStuckNotes();
            }
            
            GUILayout.EndArea();
        }
    }
}
