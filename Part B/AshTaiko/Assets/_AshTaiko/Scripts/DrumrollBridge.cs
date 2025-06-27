using System;
using UnityEngine;

namespace AshTaiko
{
    public class DrumrollBridge : MonoBehaviour
    {
        public Note headNote;
        public Note endNote;

        private bool hasExisted;

        private void OnEnable()
        {

        }

        void LateUpdate()
        {
            if (headNote == null && endNote == null) Destroy(gameObject);
            
            if (endNote == null && hasExisted)
            {
                Destroy(gameObject);
                return;
            }
            Vector3 fallback = (headNote != null) ? headNote.transform.position - transform.right * 100 : endNote.transform.position - transform.right * 100;

            Vector3 start = fallback;
            if (headNote != null) start = headNote.transform.position;
            Vector3 end = fallback;
            if (endNote != null)
            {
                end = endNote.transform.position;
                hasExisted = true;
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