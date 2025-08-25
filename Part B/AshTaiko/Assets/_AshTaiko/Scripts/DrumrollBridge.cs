using System;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Creates a visual bridge between drumroll start and end notes.
    /// Dynamically scales and positions itself to connect the two notes,
    /// providing visual feedback for drumroll duration and position.
    /// </summary>
    public class DrumrollBridge : MonoBehaviour
    {
        [SerializeField]
        private Note _headNote;
        
        [SerializeField]
        private Note _endNote;

        /// <summary>
        /// Tracks whether the end note has existed at some point.
        /// Used to prevent premature destruction of the bridge.
        /// </summary>
        private bool _hasExisted;

        /// <summary>
        /// The drumroll start note.
        /// </summary>
        public Note headNote { get => _headNote; set => _headNote = value; }
        
        /// <summary>
        /// The drumroll end note.
        /// </summary>
        public Note endNote { get => _endNote; set => _endNote = value; }

        /// <summary>
        /// Updates the bridge position and scale to connect the head and end notes.
        /// Destroys the bridge if both notes are missing.
        /// </summary>
        void LateUpdate()
        {
            if (_headNote == null && _endNote == null) Destroy(gameObject);
            
            if (_endNote == null && _hasExisted)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 fallback = (_headNote != null) ? _headNote.transform.position - transform.right * 100 : _endNote.transform.position - transform.right * 100;

            Vector3 start = fallback;
            if (_headNote != null) start = _headNote.transform.position;
            Vector3 end = fallback;
            if (_endNote != null)
            {
                end = _endNote.transform.position;
                _hasExisted = true;
            }

            //Vector3 mid = (start + end) / 2f;F
            transform.position = start;
            // Use absolute value for length
            float length = Mathf.Abs(end.x - start.x);
            transform.localScale = new Vector3(length, transform.localScale.y, transform.localScale.z);
        }
    }
}