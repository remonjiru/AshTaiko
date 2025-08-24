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

        [Header("Song Information")]
        [SerializeField]
        private Image _songCoverImage;
        [SerializeField]
        private TextMeshProUGUI _songTitleText;
        [SerializeField]
        private TextMeshProUGUI _songArtistText;
        [SerializeField]
        private TextMeshProUGUI _songCreatorText;
        [SerializeField]
        private TextMeshProUGUI _chartVersionText;

        private int lerpedScore;

        private void Awake()
        {
            _gaugeFill.fillAmount = 0;
            _gameManager.OnComboChange += OnComboChange;
            _gameManager.OnScoreChange += OnScoreChange;
            _gameManager.OnAccuracyChange += OnAccuracyChange;
            _gameManager.OnSongChanged += OnSongChanged;
        }
        
        private void Start()
        {
            // Initialize UI with current song information if available
            InitializeSongInfo();
        }
        
        private void InitializeSongInfo()
        {
            var currentSong = _gameManager.GetCurrentSong();
            var currentChart = _gameManager.GetCurrentChart();
            
            if (currentSong != null && currentChart != null)
            {
                OnSongChanged(currentSong, currentChart);
            }
            else
            {
                ClearSongInfo();
            }
        }
        
        private void ClearSongInfo()
        {
            if (_songTitleText != null)
                _songTitleText.text = "No Song Selected";
            
            if (_songArtistText != null)
                _songArtistText.text = "Select a song to begin";
            
            if (_songCreatorText != null)
                _songCreatorText.text = "";
            
            if (_chartVersionText != null)
                _chartVersionText.text = "";
            
            if (_songCoverImage != null)
            {
                _songCoverImage.sprite = null;
                _songCoverImage.color = new Color(0.1f, 0.1f, 0.1f, 0.3f); // Very dark placeholder
            }
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
            _gameManager.OnSongChanged -= OnSongChanged;
        }

        private void OnComboChange(int newComboValue)
        {
            FormattableString message = $"{newComboValue:N0}";
            string op4 = FormattableString.Invariant(message);
            _comboText.text = op4;
        }

        private void OnSongChanged(SongEntry song, ChartData chart)
        {
            if (song == null || chart == null) 
            {
                ClearSongInfo();
                return;
            }
            
            // Update song information with null checks
            if (_songTitleText != null)
                _songTitleText.text = song.Title ?? "Unknown Title";
            
            if (_songArtistText != null)
                _songArtistText.text = song.Artist ?? "Unknown Artist";
            
            if (_songCreatorText != null)
                _songCreatorText.text = song.Creator ?? "Unknown Creator";
            
            if (_chartVersionText != null)
                _chartVersionText.text = $"Chart: {chart.Version} ({chart.Difficulty})";
            
            // Load song cover image if available
            if (_songCoverImage != null)
            {
                if (!string.IsNullOrEmpty(song.BackgroundImage))
                {
                    LoadSongCover(song.BackgroundImage);
                }
                else
                {
                    // Set a default cover or clear the image
                    _songCoverImage.sprite = null;
                    _songCoverImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Dark gray placeholder
                }
            }
            
            Debug.Log($"Song info updated: {song.Title} - {song.Artist} ({chart.Version})");
        }
        
        private void LoadSongCover(string imagePath)
        {
            // Start loading the image asynchronously
            StartCoroutine(LoadImageCoroutine(imagePath));
        }
        
        private System.Collections.IEnumerator LoadImageCoroutine(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) yield break;
            
            // Check if file exists
            if (!System.IO.File.Exists(imagePath))
            {
                Debug.LogWarning($"Song cover image not found: {imagePath}");
                SetDefaultCover();
                yield break;
            }
            
            // Load image using UnityWebRequest
            string fullPath = "file://" + imagePath;
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(fullPath))
            {
                yield return www.SendWebRequest();
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    if (texture != null)
                    {
                        // Create sprite from texture
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        _songCoverImage.sprite = sprite;
                        _songCoverImage.color = Color.white; // Reset to normal color
                        Debug.Log($"Song cover loaded: {imagePath}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to get texture from song cover: {imagePath}");
                        SetDefaultCover();
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load song cover: {www.error}");
                    SetDefaultCover();
                }
            }
        }
        
        private void SetDefaultCover()
        {
            if (_songCoverImage != null)
            {
                _songCoverImage.sprite = null;
                _songCoverImage.color = new Color(0.3f, 0.3f, 0.3f, 0.6f); // Medium gray placeholder
            }
        }


        private void Update()
        {
            _timestampText.text = _gameManager.GetSongTimestamp();
            _completeText.text = Math.Round(_gameManager.GetSongProgressPercentage() * 100, 1) + "% complete";
            _completeFill.fillAmount = _gameManager.GetSongProgressPercentage();
            _gaugeFill.fillAmount = Mathf.Lerp(_gaugeFill.fillAmount, _gameManager.GetGaugePercentage(), Time.deltaTime * 10);
        }
        
        // Public method to manually refresh song information
        public void RefreshSongInfo()
        {
            InitializeSongInfo();
        }
        
        // Public method to manually update song information
        public void UpdateSongInfo(SongEntry song, ChartData chart)
        {
            OnSongChanged(song, chart);
        }
    }
}
