using UnityEngine;
using TMPro;

namespace AshTaiko
{
    [CreateAssetMenu(menuName = "AshTaiko/Menu/Splash Text Entries", fileName = "New Splash Text Entries")]
    public class SplashTextEntries : ScriptableObject
    {
        [SerializeField]
        private string[] _entries;

        public string[] Entries => _entries;

        public string GetRandomEntry()
        {
            if (_entries.Length > 0)
            {
                return _entries[Random.Range(0, _entries.Length)];
            }
            return "Sample text!";
        }
    }
}
