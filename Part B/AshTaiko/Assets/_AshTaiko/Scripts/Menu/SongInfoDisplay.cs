using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Linq;

namespace AshTaiko.Menu
{
    /// <summary>
    /// Handles the display of song information including metadata, calculated statistics, and cover images.
    /// This class provides comprehensive song details to help users make informed decisions.
    /// Uses UI component references for efficient display updates.
    /// Implements image loading with coroutines for non-blocking operations.
    /// Stores calculated song statistics for performance optimization.
    /// </summary>
    public class SongInfoDisplay : MonoBehaviour
    {
        #region UI References

        [Header("Song Information")]
        [SerializeField] 
        private TextMeshProUGUI _songTitleText;
        [SerializeField] 
        private TextMeshProUGUI _songArtistText;
        [SerializeField] 
        private TextMeshProUGUI _songCreatorText;
        [SerializeField] 
        private TextMeshProUGUI _songLengthText;
        [SerializeField] 
        private TextMeshProUGUI _songBPMText;
        [SerializeField] 
        private TextMeshProUGUI _songFormatText;
        [SerializeField] 
        private TextMeshProUGUI _songNoteCountText;
        [SerializeField] 
        private Image _songCoverImage;
        
        [Header("Image Display Settings")]
        [SerializeField] 
        private bool _preserveAspectRatio = true;
        [SerializeField] 
        private Image.Type _imageType = Image.Type.Simple;

        #endregion

        #region Private Fields

        /// <summary>
        /// Current song data for display and calculations.
        /// Using nullable references ensures safe access and prevents null reference exceptions.
        /// </summary>
        private SongEntry _currentSong;
        private ChartData _currentChart;

        #endregion

        #region Initialization

        private void Awake()
        {
            // Validate required UI components
            ValidateUIComponents();
        }

        /// <summary>
        /// Checks that all required UI components are properly assigned.
        /// This helps catch configuration issues early in development.
        /// </summary>
        private void ValidateUIComponents()
        {
            if (_songTitleText == null) Debug.LogWarning("SongTitleText is not assigned!");
            if (_songArtistText == null) Debug.LogWarning("SongArtistText is not assigned!");
            if (_songCreatorText == null) Debug.LogWarning("SongCreatorText is not assigned!");
            if (_songLengthText == null) Debug.LogWarning("SongLengthText is not assigned!");
            if (_songBPMText == null) Debug.LogWarning("SongBPMText is not assigned!");
            if (_songFormatText == null) Debug.LogWarning("SongFormatText is not assigned!");
            if (_songNoteCountText == null) Debug.LogWarning("SongNoteCountText is not assigned!");
            if (_songCoverImage == null) Debug.LogWarning("SongCoverImage is not assigned!");
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Updates the display with information about the selected song and chart.
        /// This method provides comprehensive song details for user decision making.
        /// </summary>
        /// <param name="song">The song entry to display information for.</param>
        /// <param name="chart">The chart data to display information for.</param>
        public void UpdateSongInfo(SongEntry song, ChartData chart)
        {
            _currentSong = song;
            _currentChart = chart;

            if (song == null)
            {
                ClearSongInfoDisplay();
                return;
            }

            // Update basic text fields
            UpdateBasicSongInfo();
            
            // Update calculated information
            UpdateCalculatedSongInfo();
            
            // Update cover image
            UpdateSongCoverImage();
        }

        /// <summary>
        /// Resets all song information display fields.
        /// This provides a clean state when no song is selected.
        /// </summary>
        public void ClearSongInfoDisplay()
        {
            if (_songTitleText != null)
                _songTitleText.text = "Select a song to view details";
            
            if (_songArtistText != null)
                _songArtistText.text = "";
            
            if (_songCreatorText != null)
                _songCreatorText.text = "";
            
            if (_songLengthText != null)
                _songLengthText.text = "";
            
            if (_songBPMText != null)
                _songBPMText.text = "";
            
            if (_songFormatText != null)
                _songFormatText.text = "";
                
            if (_songNoteCountText != null)
                _songNoteCountText.text = "";

            if (_songCoverImage != null)
            {
                _songCoverImage.sprite = null;
                _songCoverImage.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Populates the basic song metadata fields.
        /// This includes title, artist, creator, and other static information.
        /// </summary>
        private void UpdateBasicSongInfo()
        {
            if (_songTitleText != null)
                _songTitleText.text = _currentSong.Title ?? "Unknown Title";
            
            if (_songArtistText != null)
                _songArtistText.text = _currentSong.Artist ?? "Unknown Artist";
            
            if (_songCreatorText != null)
                _songCreatorText.text = _currentSong.Creator ?? "Unknown Creator";

            // Update length
            if (_songLengthText != null)
            {
                float totalLength = 0;
                if (_currentSong.Charts != null && _currentSong.Charts.Count > 0)
                {
                    totalLength = _currentSong.Charts[0].TotalLength;
                }
                _songLengthText.text = FormatTime(totalLength);
            }
            
            // Update format information
            if (_songFormatText != null)
            {
                string format = GetFormatDisplayName(_currentSong.Format);
                _songFormatText.text = $"Format: {format}";
            }
            
            // Update note count information
            if (_currentChart != null)
            {
                // If we have a specific chart selected, show its note count
                UpdateNoteCountDisplay();
            }
            else if (_songNoteCountText != null && _currentSong.Charts != null && _currentSong.Charts.Count > 0)
            {
                // Otherwise show total note count across all charts
                int totalNoteCount = _currentSong.Charts.Sum(c => c.HitObjects?.Count ?? 0);
                _songNoteCountText.text = $"Notes: {totalNoteCount:N0}";
            }
        }

        /// <summary>
        /// Calculates and displays dynamic song information.
        /// This includes BPM calculation from timing points and difficulty statistics.
        /// </summary>
        private void UpdateCalculatedSongInfo()
        {
            if (_currentChart == null) return;

            // Update BPM information
            if (_songBPMText != null)
            {
                float bpm = CalculateAverageBPM(_currentChart);
                _songBPMText.text = $"BPM: {bpm:F1}";
            }

            // Update additional song statistics
            UpdateSongStatistics();
        }

        /// <summary>
        /// Computes the average BPM from timing points in the chart.
        /// This provides users with tempo information to help with song selection.
        /// </summary>
        /// <param name="chart">The chart data to calculate BPM from.</param>
        /// <returns>The average BPM value.</returns>
        private float CalculateAverageBPM(ChartData chart)
        {
            if (chart.TimingPoints == null || chart.TimingPoints.Count == 0)
                return 120f; // Default BPM

            float totalBPM = 0f;
            int validPoints = 0;

            foreach (var timingPoint in chart.TimingPoints)
            {
                if (timingPoint.Uninherited && timingPoint.BeatLength > 0)
                {
                    float bpm = timingPoint.BPM;
                    if (bpm > 0 && bpm < 1000) // Sanity check for reasonable BPM values
                    {
                        totalBPM += bpm;
                        validPoints++;
                    }
                }
            }

            return validPoints > 0 ? totalBPM / validPoints : 120f;
        }

        /// <summary>
        /// Displays additional information about the selected song.
        /// This includes note counts, timing point information, and difficulty analysis.
        /// </summary>
        private void UpdateSongStatistics()
        {
            if (_currentChart == null) return;

            if (_currentSong != null)
            {
                string format = GetFormatDisplayName(_currentSong.Format);
                
                // Update note count when chart is selected
                UpdateNoteCountDisplay();
            }
        }
        
        /// <summary>
        /// Updates the note count display with the current chart's note count.
        /// This provides real-time feedback when switching between different difficulty charts.
        /// </summary>
        private void UpdateNoteCountDisplay()
        {
            if (_songNoteCountText == null || _currentChart == null) return;
            
            int noteCount = _currentChart.HitObjects?.Count ?? 0;
            _songNoteCountText.text = $"Notes: {noteCount:N0}";
        }

        /// <summary>
        /// Loads and displays the song's background image.
        /// This provides visual context and makes the selection interface more engaging.
        /// </summary>
        private void UpdateSongCoverImage()
        {
            if (_songCoverImage == null) return;

            if (_currentSong.HasImage())
            {
                // Load background image
                string imagePath = _currentSong.GetBestAvailableImagePath();
                LoadSongCoverImage(imagePath);
            }
            else
            {
                // Set default cover
                _songCoverImage.sprite = null;
                _songCoverImage.color = new Color(0.2f, 0.2f, 0.5f);
            }
        }

        /// <summary>
        /// Loads the song cover image from the file system.
        /// This method handles image loading with proper error handling and fallbacks.
        /// </summary>
        /// <param name="imagePath">The path to the image file to load.</param>
        private void LoadSongCoverImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                SetDefaultCoverImage();
                return;
            }

            // Check if the image file exists
            if (!System.IO.File.Exists(imagePath))
            {
                Debug.LogWarning($"Image file not found: {imagePath}");
                SetDefaultCoverImage();
                return;
            }

            // Start coroutine to load image asynchronously
            StartCoroutine(LoadImageCoroutine(imagePath));
        }

        /// <summary>
        /// Loads an image file asynchronously to prevent UI blocking.
        /// This provides smooth user experience during image loading operations.
        /// </summary>
        /// <param name="imagePath">The path to the image file to load.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private System.Collections.IEnumerator LoadImageCoroutine(string imagePath)
        {
            // Convert file path to Unity-compatible format
            string fileUrl = "file://" + imagePath;
            
            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(fileUrl))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    // Convert texture to sprite
                    var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    
                    if (_songCoverImage != null)
                    {
                        _songCoverImage.sprite = sprite;
                        _songCoverImage.color = Color.white;
                        
                        // Configure the image to fit nicely without stretching
                        ConfigureImageForTexture(texture);
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load image: {www.error}");
                    SetDefaultCoverImage();
                }
            }
        }

        /// <summary>
        /// Provides a fallback when no cover image is available.
        /// This ensures the UI always has a consistent appearance.
        /// </summary>
        private void SetDefaultCoverImage()
        {
            if (_songCoverImage != null)
            {
                _songCoverImage.sprite = null;
                _songCoverImage.color = new Color(0.2f, 0.2f, 0.5f);
                
                // Reset image settings to default
                ResetImageToDefault();
            }
        }
        
        /// <summary>
        /// Sets up the Image component to display the texture without stretching,
        /// maintaining aspect ratio and filling the entire component.
        /// This creates a "fill and crop" effect similar to CSS object-fit: cover.
        /// </summary>
        /// <param name="texture">The texture to configure the image for.</param>
        private void ConfigureImageForTexture(Texture2D texture)
        {
            if (_songCoverImage == null || texture == null) return;
            
            // Set the image type and preserve aspect ratio
            _songCoverImage.type = _imageType;
            _songCoverImage.preserveAspect = _preserveAspectRatio;
            
            // Configure the image to fill the component while maintaining aspect ratio
            ConfigureImageWithFillAndCrop(texture);
        }
        
        /// <summary>
        /// Sets up the image to fill the entire component while maintaining aspect ratio.
        /// This may crop parts of the image but ensures no stretching occurs.
        /// </summary>
        /// <param name="texture">The texture to configure.</param>
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
            float parentAspect = parentSize.x / parentSize.y;
            float textureAspect = (float)texture.width / texture.height;
            
            Vector2 newSize;
            
            if (textureAspect > parentAspect)
            {
                // Texture is wider than parent - fit to height, crop width
                newSize = new Vector2(parentSize.y * textureAspect, parentSize.y);
            }
            else
            {
                // Texture is taller than parent - fit to width, crop height
                newSize = new Vector2(parentSize.x, parentSize.x / textureAspect);
            }
            
            // Set the size to fill the parent while maintaining aspect ratio
            imageRect.sizeDelta = newSize;
            
            // Center the image
            imageRect.anchoredPosition = Vector2.zero;
            
            Debug.Log($"Image configured: texture {texture.width}x{texture.height} (aspect: {textureAspect:F2}), " +
                     $"parent {parentSize.x:F0}x{parentSize.y:F0} (aspect: {parentAspect:F2}), " +
                     $"new size {newSize.x:F0}x{newSize.y:F0}");
        }
        
        /// <summary>
        /// Resets the Image component to its default size and settings
        /// when no image is loaded or when clearing the display.
        /// </summary>
        private void ResetImageToDefault()
        {
            if (_songCoverImage == null) return;
            
            // Reset to default Image type and settings
            _songCoverImage.type = _imageType;
            _songCoverImage.preserveAspect = _preserveAspectRatio;
            
            // Reset the size to match the parent container
            RectTransform imageRect = _songCoverImage.rectTransform;
            if (imageRect != null)
            {
                RectTransform parentRect = imageRect.parent as RectTransform;
                if (parentRect != null)
                {
                    // Use the parent's size as the default
                    imageRect.sizeDelta = parentRect.rect.size;
                    imageRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        /// <summary>
        /// Converts seconds to a human-readable MM:SS format.
        /// This provides consistent time display across the UI.
        /// </summary>
        /// <param name="timeInSeconds">The time in seconds to format.</param>
        /// <returns>A formatted time string in MM:SS format.</returns>
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        
        /// <summary>
        /// Converts the SongFormat enum to a human-readable display name.
        /// </summary>
        /// <param name="format">The format enum value to convert.</param>
        /// <returns>A user-friendly string representation of the format.</returns>
        private string GetFormatDisplayName(SongFormat format)
        {
            switch (format)
            {
                case SongFormat.Osu:
                    return "osu!";
                case SongFormat.Tja:
                    return "TJA";
                case SongFormat.Unknown:
                default:
                    return "Unknown Format";
            }
        }
        
        #endregion
    }
}

