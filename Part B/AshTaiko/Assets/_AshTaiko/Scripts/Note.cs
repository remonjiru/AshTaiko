using UnityEngine;

namespace AshTaiko
{
    public class Note : MonoBehaviour
    {
        [SerializeField]
        private float _hitTime;
        
        [SerializeField]
        private float _spawnTime;
        
        [SerializeField]
        private float _preemptTime;
        
        [SerializeField]
        private float _travelDistance;
        
        [SerializeField]
        private Vector3 _judgementCirclePosition;
        
        private Vector3 _currentPosition;
        
        [SerializeField]
        private float _scrollSpeed = 1f;
        
        private bool _isHit = false;
        
        [SerializeField]
        private HitObject _hitObject;
        
        [SerializeField]
        private NoteType _noteType; // Store the type of this note
        
        // Visual references for each type
        [SerializeField]
        private GameObject _donVisual;
        
        [SerializeField]
        private GameObject _kaVisual;
        
        [SerializeField]
        private GameObject _donBigVisual;
        
        [SerializeField]
        private GameObject _kaBigVisual;
        
        [SerializeField]
        private GameObject _drumrollHeadVisual;
        
        [SerializeField]
        private GameObject _drumrollEndVisual;
        
        [SerializeField]
        private GameObject _drumrollBridgeVisual;
        
        // For drumroll stretching
        [SerializeField]
        private Note _drumrollEndNote; // Set by GameManager if this is a drumroll head

        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        // Public properties for access
        public float HitTime => _hitTime;
        public float SpawnTime => _spawnTime;
        public float PreemptTime => _preemptTime;
        public float TravelDistance => _travelDistance;
        public Vector3 JudgementCirclePosition { get => _judgementCirclePosition; set => _judgementCirclePosition = value; }
        public float ScrollSpeed => _scrollSpeed;
        public bool IsHit { get => _isHit; set => _isHit = value; }
        public HitObject HitObject => _hitObject;
        public NoteType NoteType => _noteType;
        public Note DrumrollEndNote { get => _drumrollEndNote; set => _drumrollEndNote = value; }

        public void Initialize(float hitTime, float preemptTime, float travelDistance, float scrollSpeed, HitObject hitObject)
        {
            this._hitTime = hitTime;
            this._spawnTime = GameManager.Instance.SongTime;
            this._preemptTime = preemptTime;
            this._travelDistance = travelDistance;
            transform.position = JudgementCirclePosition + Vector3.right * TravelDistance;
            _currentPosition = transform.position;
            this._scrollSpeed = scrollSpeed;
            this._hitObject = hitObject;
            this._noteType = hitObject.Type; // Set the note type (Don, Ka, etc.)
            SetVisuals();
        }

        private void SetVisuals()
        {
            // Disable all visuals first
            if (_donVisual) _donVisual.SetActive(false);
            if (_kaVisual) _kaVisual.SetActive(false);
            if (_donBigVisual) _donBigVisual.SetActive(false);
            if (_kaBigVisual) _kaBigVisual.SetActive(false);
            if (_drumrollHeadVisual) _drumrollHeadVisual.SetActive(false);
            if (_drumrollEndVisual) _drumrollEndVisual.SetActive(false);
            if (_drumrollBridgeVisual) _drumrollBridgeVisual.SetActive(false);

            switch (NoteType)
            {
                case NoteType.Don:
                    if (_donVisual) _donVisual.SetActive(true);
                    break;
                case NoteType.Ka:
                    if (_kaVisual) _kaVisual.SetActive(true);
                    break;
                case NoteType.DonBig:
                    if (_donBigVisual) _donBigVisual.SetActive(true);
                    break;
                case NoteType.KaBig:
                    if (_kaBigVisual) _kaBigVisual.SetActive(true);
                    break;
                case NoteType.Drumroll:
                case NoteType.DrumrollBig:
                    if (_drumrollHeadVisual) _drumrollHeadVisual.SetActive(true);
                    break;
                case NoteType.DrumrollBalloonEnd:
                    if (_drumrollEndVisual) _drumrollEndVisual.SetActive(true);
                    break;
            }
        }

        void LateUpdate()
        {
            // Debug: Check if component is disabled
            if (!enabled)
            {
                Debug.LogWarning($"Note component at {HitTime}s is DISABLED! This will make it unhittable!");
                return;
            }
            
            float currentTime = GameManager.Instance.GetSmoothedSongTime();
            float timeLeft = HitTime - currentTime;
            float effectivePreempt = PreemptTime / ScrollSpeed; // faster scroll = shorter preempt

            float t = 1f - timeLeft / effectivePreempt;

            // Interpolate from spawn position to hit bar
            Vector3 targetPosition = Vector3.LerpUnclamped(JudgementCirclePosition + Vector3.right * TravelDistance, JudgementCirclePosition, t);
            transform.position = targetPosition;

            float distancePastHitBar = JudgementCirclePosition.x - transform.position.x;

            // Debug note destruction (only log occasionally to avoid spam)
            if (Time.frameCount % 60 == 0 && distancePastHitBar > 5f)
            {
                Debug.Log($"Note {HitTime}s: distance={distancePastHitBar:F1}, isHit={IsHit}, type={NoteType}, enabled={enabled}");
            }

            // Only destroy notes if they've been properly judged or are way past the hit window
            if (NoteType == NoteType.Drumroll || NoteType == NoteType.DrumrollBig)
            {
                // For drumrolls, only destroy if we have an end note and are way past
                if (distancePastHitBar > 10f && DrumrollEndNote != null) // Increased from 5f to 10f
                {
                    Debug.Log($"Destroying drumroll note at {HitTime}s (distance: {distancePastHitBar:F1})");
                    Destroy(gameObject);
                }
                return;
            }
            
            // For regular notes, only destroy if they're way past the hit window
            // This prevents premature destruction due to timing issues
            if (distancePastHitBar > 10f && IsHit) // Only destroy if already judged AND way past
            {
                Debug.Log($"Destroying judged note at {HitTime}s (distance: {distancePastHitBar:F1})");
                Destroy(gameObject);
            }
            else if (distancePastHitBar > 20f) // Emergency cleanup for notes that are very far past
            {
                Debug.LogWarning($"Note at {HitTime}s destroyed due to extreme distance: {distancePastHitBar:F1} units past hit bar");
                Destroy(gameObject);
            }
        }
        
        // Debug method to check note status
        [ContextMenu("Check Note Status")]
        public void CheckNoteStatus()
        {
            float currentTime = GameManager.Instance.GetSmoothedSongTime();
            float timeLeft = HitTime - currentTime;
            float distancePastHitBar = JudgementCirclePosition.x - transform.position.x;
            
            Debug.Log($"=== NOTE STATUS CHECK ===");
            Debug.Log($"Note at time: {HitTime}s");
            Debug.Log($"Current song time: {currentTime:F3}s");
            Debug.Log($"Time until hit: {timeLeft:F3}s");
            Debug.Log($"Distance past hit bar: {distancePastHitBar:F1} units");
            Debug.Log($"Is hit: {IsHit}");
            Debug.Log($"Note type: {NoteType}");
            Debug.Log($"Position: {transform.position}");
            Debug.Log($"Judgement circle: {JudgementCirclePosition}");
            Debug.Log($"Active: {gameObject.activeInHierarchy}");
            Debug.Log($"Enabled: {enabled}");
            Debug.Log($"================================");
        }
        
        // Method to force note to be hit (for debugging)
        [ContextMenu("Force Hit Note")]
        public void ForceHitNote()
        {
            _isHit = true;
            Debug.Log($"Note at {HitTime}s forced to hit state");
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

