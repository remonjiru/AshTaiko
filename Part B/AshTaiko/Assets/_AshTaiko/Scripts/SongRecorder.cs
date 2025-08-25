using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshTaiko
{
    /// <summary>
    /// Utility component for quickly creating fake charts by recording player input.
    /// Records drum hits as HitObjects with current timing, allowing for rapid
    /// chart creation during development and testing.
    /// </summary>
    public class SongRecorder : MonoBehaviour
    {
        [SerializeField]
        private InputReader _input;

        /// <summary>
        /// List of recorded hit objects representing the fake chart.
        /// </summary>
        [SerializeField]
        private List<HitObject> _notes = new List<HitObject>();

        /// <summary>
        /// Sets up input event subscriptions for recording drum hits.
        /// </summary>
        private void Start()
        {
            _input.DonLeftEvent += DonLeft;
            _input.DonRightEvent += DonRight;
            _input.KaLeftEvent += KaLeft;
            _input.KaRightEvent += KaRight;
        }

        /// <summary>
        /// Records a left Don hit at the current song time.
        /// </summary>
        private void DonLeft()
        {
            _notes.Add(new HitObject(GameManager.Instance.GetSmoothedSongTime(), 1, NoteType.Don));
        }

        /// <summary>
        /// Records a right Don hit at the current song time.
        /// </summary>
        private void DonRight()
        {
            _notes.Add(new HitObject(GameManager.Instance.GetSmoothedSongTime(), 1, NoteType.Don));
        }

        /// <summary>
        /// Records a left Ka hit at the current song time.
        /// </summary>
        private void KaLeft()
        {
            _notes.Add(new HitObject(GameManager.Instance.GetSmoothedSongTime(), 1, NoteType.Ka));
        }

        /// <summary>
        /// Records a right Ka hit at the current song time.
        /// </summary>
        private void KaRight()
        {
            _notes.Add(new HitObject(GameManager.Instance.GetSmoothedSongTime(), 1, NoteType.Ka));
        }
    }
}
