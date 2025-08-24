using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AshTaiko
{
    // this one's basically a utility to just really quickly make a fake chart so i can play them 
    public class SongRecorder : MonoBehaviour
    {
        [SerializeField]
        private InputReader _input;

        [SerializeField]
        private List<HitObject> _notes = new List<HitObject>();

        private void Start()
        {
            _input.DonLeftEvent += DonLeft;
            _input.DonRightEvent += DonRight;
            _input.KaLeftEvent += KaLeft;
            _input.KaRightEvent += KaRight;
        }

        private void DonLeft()
        {
            _notes.Add(new HitObject(GameManager.Instance.GetSmoothedSongTime(), 1, NoteType.Don));
        }

        private void DonRight()
        {
            _notes.Add(new HitObject(GameManager.Instance.GetSmoothedSongTime(), 1, NoteType.Don));
        }

        private void KaLeft()
        {
            _notes.Add(new HitObject(GameManager.Instance.GetSmoothedSongTime(), 1, NoteType.Ka));
        }

        private void KaRight()
        {
            _notes.Add(new HitObject(GameManager.Instance.GetSmoothedSongTime(), 1, NoteType.Ka));
        }
    }
}
