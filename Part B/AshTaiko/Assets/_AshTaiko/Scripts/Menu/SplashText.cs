using UnityEngine;
using TMPro;

namespace AshTaiko
{
    public class SplashText : MonoBehaviour
    {
        private TextMeshProUGUI _text;

        [SerializeField]
        private SplashTextEntries _entries;

        private void Start()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _text.text = _entries.GetRandomEntry();
        }

    }
}
