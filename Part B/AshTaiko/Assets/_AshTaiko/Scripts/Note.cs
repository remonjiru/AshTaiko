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
            float currentTime = GameManager.Instance.GetSmoothedSongTime();
            float timeLeft = hitTime - currentTime;
            float effectivePreempt = preemptTime / scrollSpeed; // faster scroll = shorter preempt

            float t = 1f - timeLeft / effectivePreempt;

            // Interpolate from spawn position to hit bar
            Vector3 targetPosition = Vector3.LerpUnclamped(judgementCirclePosition + Vector3.right * travelDistance, judgementCirclePosition, t);
            transform.position = targetPosition;

            float distancePastHitBar = judgementCirclePosition.x - transform.position.x;

            if (noteType == NoteType.Drumroll || noteType == NoteType.DrumrollBig)
            {
                if (distancePastHitBar > 5f) // Adjust this value as needed (5 units past the hit bar)
                {
                    if (drumrollEndNote != null)
                    {
                        Destroy(gameObject);
                    }
                }
                return;
            }
            if (distancePastHitBar > 5f) // Adjust this value as needed (5 units past the hit bar)
            {
                Destroy(gameObject);
            }
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
