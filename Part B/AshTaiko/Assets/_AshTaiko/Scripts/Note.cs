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

            // TO DO : CHAGNE THIS SHIT
            if (hitObject.Type == NoteType.Don)
            {
                _spriteRenderer.color = SkinManager.Instance.GetColor(SkinElement.Don);
            }
            if (hitObject.Type == NoteType.Ka)
            {
                _spriteRenderer.color = SkinManager.Instance.GetColor(SkinElement.Ka);
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
            // // If too late, destroy or mark as missed
            
            // if (currentTime > hitTime + 0.2f)
            // {
            //     Destroy(gameObject);
            // }
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
