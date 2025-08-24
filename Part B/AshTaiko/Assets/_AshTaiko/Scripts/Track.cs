using UnityEngine;

namespace AshTaiko
{
    public class Track : MonoBehaviour
    {
        // singleton pattern
        public static Track Instance { get; private set; }

        [Header("Gameplay")]
        [SerializeField]
        private Transform _receptacle;
        
        [SerializeField]
        private Transform _startPosition;

        [SerializeField]
        private Transform _endPosition;

        [SerializeField]
        private Vector3 _offscreenOffsetStart;
        
        [SerializeField]
        private Vector3 _offscreenOffsetEnd;

        private void Awake()
        {
            Instance = this;
        }

        public Vector3 GetStart()
        {
            return _startPosition.position + _offscreenOffsetStart;
        }
        
        public Vector3 GetReceptacle()
        {
            return _receptacle.position;
        }
    }
}
