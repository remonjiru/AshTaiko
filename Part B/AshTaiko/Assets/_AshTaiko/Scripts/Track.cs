using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Defines the track path that notes follow during gameplay.
    /// Provides positions for note spawning, judgement, and offscreen boundaries.
    /// Uses a singleton pattern for global access to track positioning information.
    /// </summary>
    public class Track : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance providing global access to track positioning.
        /// </summary>
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

        /// <summary>
        /// Sets up the singleton instance.
        /// </summary>
        private void Awake()
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the starting position for note spawning, including offscreen offset.
        /// </summary>
        /// <returns>World position where notes should spawn.</returns>
        public Vector3 GetStart()
        {
            return _startPosition.position + _offscreenOffsetStart;
        }
        
        /// <summary>
        /// Gets the receptacle position where notes are judged.
        /// </summary>
        /// <returns>World position of the judgement circle.</returns>
        public Vector3 GetReceptacle()
        {
            return _receptacle.position;
        }
    }
}
