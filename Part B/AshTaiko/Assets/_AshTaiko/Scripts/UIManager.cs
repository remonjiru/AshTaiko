using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using DG.Tweening;

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
        [SerializeField]
        private TextMeshProUGUI _timestampText;
        [SerializeField]
        private Image _completeFill;

        [SerializeField]
        private Image _gaugeFill;

        private int lerpedScore;

        private void Awake()
        {
            _gaugeFill.fillAmount = 0;
            _gameManager.OnComboChange += OnComboChange;
            _gameManager.OnScoreChange += OnScoreChange;
            _gameManager.OnAccuracyChange += OnAccuracyChange;
        }

        private void OnAccuracyChange(float accuracy)
        {
            _accuracyText.text = accuracy.ToString("F2") + "%";
        }

        private void OnScoreChange(int score)
        {
            DOTween.To(() => lerpedScore, x => lerpedScore = x, score, 0.2f).OnUpdate(UpdateText);
        }

        private void UpdateText()
        {
            string padded = lerpedScore.ToString("D7");

            string formatted = $"{padded.Substring(0, 1)} {padded.Substring(1, 3)} {padded.Substring(4, 3)}";
            _scoreText.text = formatted;
        }

        private void OnDisable()
        {
            _gameManager.OnComboChange -= OnComboChange;
            _gameManager.OnScoreChange -= OnScoreChange;
            _gameManager.OnAccuracyChange -= OnAccuracyChange;

        }

        private void OnComboChange(int newComboValue)
        {
            FormattableString message = $"{newComboValue:N0}";
            string op4 = FormattableString.Invariant(message);
            _comboText.text = op4;
        }


        private void Update()
        {
            _timestampText.text = _gameManager.GetSongTimestamp();
            _completeText.text = Math.Round(_gameManager.GetSongProgressPercentage() * 100, 1) + "% complete";
            _completeFill.fillAmount = _gameManager.GetSongProgressPercentage();
            _gaugeFill.fillAmount = Mathf.Lerp(_gaugeFill.fillAmount, _gameManager.GetGaugePercentage(), Time.deltaTime * 10);
        }
    }
}
