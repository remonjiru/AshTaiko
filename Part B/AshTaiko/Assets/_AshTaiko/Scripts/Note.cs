using UnityEngine;

namespace AshTaiko
{
    public class Note : MonoBehaviour
    {
        public float hitTime;
        public float spawnTime;
        public float preemptTime;
        public float travelDistance;
        public Vector3 judgementCirclePosition;
        private Vector3 currentPosition;
        public float scrollSpeed = 1f;
        public bool isHit = false;
        public HitObject hitObject;
        public NoteType noteType; // Store the type of this note
                                  // Visual references for each type

        public GameObject donVisual;
        public GameObject kaVisual;
        public GameObject donBigVisual;
        public GameObject kaBigVisual;
        public GameObject drumrollHeadVisual;
        public GameObject drumrollEndVisual;
        public GameObject drumrollBridgeVisual;
        // For drumroll stretching
        public Note drumrollEndNote; // Set by GameManager if this is a drumroll head

        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        public void Initialize(float hitTime, float preemptTime, float travelDistance, float scrollSpeed, HitObject hitObject)
        {
            this.hitTime = hitTime;
            this.spawnTime = GameManager.Instance.SongTime;
            this.preemptTime = preemptTime;
            this.travelDistance = travelDistance;
            transform.position = judgementCirclePosition + Vector3.right * travelDistance;
            currentPosition = transform.position;
            this.scrollSpeed = scrollSpeed;
            this.hitObject = hitObject;
            this.noteType = hitObject.Type; // Set the note type (Don, Ka, etc.)
            SetVisuals();
        }

        private void SetVisuals()
        {
            // Disable all visuals first
            if (donVisual) donVisual.SetActive(false);
            if (kaVisual) kaVisual.SetActive(false);
            if (donBigVisual) donBigVisual.SetActive(false);
            if (kaBigVisual) kaBigVisual.SetActive(false);
            if (drumrollHeadVisual) drumrollHeadVisual.SetActive(false);
            if (drumrollEndVisual) drumrollEndVisual.SetActive(false);
            if (drumrollBridgeVisual) drumrollBridgeVisual.SetActive(false);

            switch (noteType)
            {
                case NoteType.Don:
                    if (donVisual) donVisual.SetActive(true);
                    break;
                case NoteType.Ka:
                    if (kaVisual) kaVisual.SetActive(true);
                    break;
                case NoteType.DonBig:
                    if (donBigVisual) donBigVisual.SetActive(true);
                    break;
                case NoteType.KaBig:
                    if (kaBigVisual) kaBigVisual.SetActive(true);
                    break;
                case NoteType.Drumroll:
                case NoteType.DrumrollBig:
                    if (drumrollHeadVisual) drumrollHeadVisual.SetActive(true);
                    break;
                case NoteType.DrumrollBalloonEnd:
                    if (drumrollEndVisual) drumrollEndVisual.SetActive(true);
                    break;
            }
        }

        void LateUpdate()
        {
            // Debug: Check if component is disabled
            if (!enabled)
            {
                Debug.LogWarning($"Note component at {hitTime}s is DISABLED! This will make it unhittable!");
                return;
            }
            
            float currentTime = GameManager.Instance.GetSmoothedSongTime();
            float timeLeft = hitTime - currentTime;
            float effectivePreempt = preemptTime / scrollSpeed; // faster scroll = shorter preempt

            float t = 1f - timeLeft / effectivePreempt;

            // Interpolate from spawn position to hit bar
            Vector3 targetPosition = Vector3.LerpUnclamped(judgementCirclePosition + Vector3.right * travelDistance, judgementCirclePosition, t);
            transform.position = targetPosition;

            float distancePastHitBar = judgementCirclePosition.x - transform.position.x;

            // Debug note destruction (only log occasionally to avoid spam)
            if (Time.frameCount % 60 == 0 && distancePastHitBar > 5f)
            {
                Debug.Log($"Note {hitTime}s: distance={distancePastHitBar:F1}, isHit={isHit}, type={noteType}, enabled={enabled}");
            }

            // Only destroy notes if they've been properly judged or are way past the hit window
            if (noteType == NoteType.Drumroll || noteType == NoteType.DrumrollBig)
            {
                // For drumrolls, only destroy if we have an end note and are way past
                if (distancePastHitBar > 10f && drumrollEndNote != null) // Increased from 5f to 10f
                {
                    Debug.Log($"Destroying drumroll note at {hitTime}s (distance: {distancePastHitBar:F1})");
                    Destroy(gameObject);
                }
                return;
            }
            
            // For regular notes, only destroy if they're way past the hit window
            // This prevents premature destruction due to timing issues
            if (distancePastHitBar > 10f && isHit) // Only destroy if already judged AND way past
            {
                Debug.Log($"Destroying judged note at {hitTime}s (distance: {distancePastHitBar:F1})");
                Destroy(gameObject);
            }
            else if (distancePastHitBar > 20f) // Emergency cleanup for notes that are very far past
            {
                Debug.LogWarning($"Note at {hitTime}s destroyed due to extreme distance: {distancePastHitBar:F1} units past hit bar");
                Destroy(gameObject);
            }
        }
        
        // Debug method to check note status
        [ContextMenu("Check Note Status")]
        public void CheckNoteStatus()
        {
            float currentTime = GameManager.Instance.GetSmoothedSongTime();
            float timeLeft = hitTime - currentTime;
            float distancePastHitBar = judgementCirclePosition.x - transform.position.x;
            
            Debug.Log($"=== NOTE STATUS CHECK ===");
            Debug.Log($"Note at time: {hitTime}s");
            Debug.Log($"Current song time: {currentTime:F3}s");
            Debug.Log($"Time until hit: {timeLeft:F3}s");
            Debug.Log($"Distance past hit bar: {distancePastHitBar:F1} units");
            Debug.Log($"Is hit: {isHit}");
            Debug.Log($"Note type: {noteType}");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"Judgement circle: {judgementCirclePosition}");
            Debug.Log($"Active: {gameObject.activeInHierarchy}");
            Debug.Log($"Enabled: {enabled}");
            Debug.Log($"================================");
        }
        
        // Method to force note to be hit (for debugging)
        [ContextMenu("Force Hit Note")]
        public void ForceHitNote()
        {
            isHit = true;
            Debug.Log($"Note at {hitTime}s forced to hit state");
        }
    }

    public enum NoteType
    {
        Blank = 0,
        Don = 1,
        Ka = 2,
        DonBig = 3,
        KaBig = 4,
        Drumroll = 5,
        DrumrollBig = 6,
        Balloon = 7,
        DrumrollBalloonEnd = 8,
        BalloonBig = 9,
    }
}

