using System;
using UnityEngine;

namespace AshTaiko
{
    public class DrumrollBridge : MonoBehaviour
    {
        [SerializeField]
        private Note _headNote;
        
        [SerializeField]
        private Note _endNote;

        private bool _hasExisted;

        // Public properties for access
        public Note headNote { get => _headNote; set => _headNote = value; }
        public Note endNote { get => _endNote; set => _endNote = value; }

        private void OnEnable()
        {

        }

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
            // Optionally, flip the bridge if needed (if your sprite/pivot requires it)
            // You can also set transform.right = (end - start).normalized; for rotation if needed
        }
    }
}