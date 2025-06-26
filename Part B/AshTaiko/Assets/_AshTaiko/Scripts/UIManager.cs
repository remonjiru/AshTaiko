using UnityEngine;
using TMPro;
using System;

namespace AshTaiko
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField]
        private GameManager _gameManager;

        [SerializeField]
        private TextMeshProUGUI _accuracyText;
        [SerializeField]
        private TextMeshProUGUI _comboText;
        [SerializeField]
        private TextMeshProUGUI _scoreText;
        [SerializeField]
        private TextMeshProUGUI _completeText;

        private void Awake()
        {
            _gameManager.OnComboChange += OnComboChange;
            _gameManager.OnScoreChange += OnScoreChange;
        }

        private void OnScoreChange(int score)
        {
            string padded = score.ToString("D7");

            string formatted = $"{padded.Substring(0, 1)} {padded.Substring(1, 3)} {padded.Substring(4, 3)}";
            _scoreText.text = formatted;
        }

        private void OnDisable()
        {
            _gameManager.OnComboChange -= OnComboChange;
            _gameManager.OnScoreChange -= OnScoreChange;
        }

        private void OnComboChange(int newComboValue)
        {
            FormattableString message = $"{newComboValue:N0}";
            string op4 = FormattableString.Invariant(message);
            _comboText.text = op4;
        }


        private void Update()
        {
            _completeText.text = Math.Round(_gameManager.GetSongProgressPercentage(), 1) + "% complete";
        }
    }
}
