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
                if (song.HasImage())
                {
                    string imagePath = song.GetBestAvailableImagePath();
                    LoadSongCover(imagePath);
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
                        // Create a temporary sprite first (will be replaced with cropped version)
                        Sprite tempSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        _songCoverImage.sprite = tempSprite;
                        _songCoverImage.color = Color.white; // Reset to normal color
                        
                        // Configure the image to fit nicely without stretching
                        // This will create a new cropped sprite and replace the temporary one
                        ConfigureImageForTexture(texture);
                        
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
                
                // Reset image settings to default
                ResetImageToDefault();
            }
        }
        
        /*
            ConfigureImageForTexture sets up the Image component to display the texture
            without stretching, maintaining aspect ratio and filling the entire component.
            This creates a "fill and crop" effect similar to CSS object-fit: cover.
        */
        private void ConfigureImageForTexture(Texture2D texture)
        {
            if (_songCoverImage == null || texture == null) return;
            
            // The ConfigureImageWithFillAndCrop method will handle all the image configuration
            // including setting the correct Image type and aspect ratio handling
            ConfigureImageWithFillAndCrop(texture);
        }
        
        /*
            ConfigureImageWithFillAndCrop sets up the image to fill the entire component
            while maintaining aspect ratio. This creates a perfect square image with
            CSS object-fit: cover behavior - the image fills the square and is centered.
        */
        private void ConfigureImageWithFillAndCrop(Texture2D texture)
        {
            if (_songCoverImage == null || texture == null) return;
            
            // Get the RectTransform of the Image component
            RectTransform imageRect = _songCoverImage.rectTransform;
            if (imageRect == null) return;
            
            // Get the parent container's size
            RectTransform parentRect = imageRect.parent as RectTransform;
            if (parentRect == null) return;
            
            Vector2 parentSize = parentRect.rect.size;
            
            // Determine the target size - we want a square that fits within the parent
            float targetSize = Mathf.Min(parentSize.x, parentSize.y);
            
            // Keep the Image component itself as a square
            imageRect.sizeDelta = new Vector2(targetSize, targetSize);
            
            // Center the image within the parent container
            imageRect.anchoredPosition = Vector2.zero;
            
            // Set the image type to Simple for better control
            _songCoverImage.type = Image.Type.Simple;
            _songCoverImage.preserveAspect = false; // We're handling aspect ratio manually
            
            // Calculate the cropping rect to achieve object-fit: cover behavior
            float textureAspect = (float)texture.width / texture.height;
            Rect cropRect;
            
            if (textureAspect > 1.0f)
            {
                // Landscape image - crop width to fit height
                float visibleWidth = texture.height; // Square aspect ratio
                float startX = (texture.width - visibleWidth) * 0.5f; // Center the crop
                cropRect = new Rect(startX, 0, visibleWidth, texture.height);
            }
            else
            {
                // Portrait or square image - crop height to fit width
                float visibleHeight = texture.width; // Square aspect ratio
                float startY = (texture.height - visibleHeight) * 0.5f; // Center the crop
                cropRect = new Rect(0, startY, texture.width, visibleHeight);
            }
            
            // Update the sprite with the new crop rect
            if (_songCoverImage.sprite != null)
            {
                // Create a new sprite with the cropped area
                Sprite newSprite = Sprite.Create(texture, cropRect, new Vector2(0.5f, 0.5f));
                _songCoverImage.sprite = newSprite;
            }
            
            Debug.Log($"Image configured: texture {texture.width}x{texture.height} (aspect: {textureAspect:F2}), " +
                     $"parent {parentSize.x:F0}x{parentSize.y:F0}, " +
                     $"target square: {targetSize:F0}x{targetSize:F0}, " +
                     $"crop rect: {cropRect}");
        }
        
        /*
            ResetImageToDefault resets the Image component to its default size and settings
            when no image is loaded or when clearing the display.
        */
        private void ResetImageToDefault()
        {
            if (_songCoverImage == null) return;
            
            // Reset to default Image type and settings
            _songCoverImage.type = Image.Type.Simple;
            _songCoverImage.preserveAspect = false; // We handle aspect ratio manually
            
            // Reset the size to a square that fits within the parent container
            RectTransform imageRect = _songCoverImage.rectTransform;
            if (imageRect != null)
            {
                RectTransform parentRect = imageRect.parent as RectTransform;
                if (parentRect != null)
                {
                    // Create a square that fits within the parent
                    float targetSize = Mathf.Min(parentRect.rect.size.x, parentRect.rect.size.y);
                    imageRect.sizeDelta = new Vector2(targetSize, targetSize);
                    imageRect.anchoredPosition = Vector2.zero;
                    
                    Debug.Log($"Reset image to default: square {targetSize:F0}x{targetSize:F0}");
                }
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
