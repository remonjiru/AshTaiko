using UnityEngine;

namespace AshTaiko
{
    public class Note : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

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
