using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

namespace AshTaiko.Menu
{
    public class SongListItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _artistText;
        [SerializeField] private TextMeshProUGUI _creatorText;
        [SerializeField] private TextMeshProUGUI _formatText;
        [SerializeField] private Image _previewImage;
        [SerializeField] private Button _itemButton;
        
        [Header("Image Display Settings")]
        [SerializeField] private bool _preserveAspectRatio = true;
        [SerializeField] private Image.Type _imageType = Image.Type.Simple;
        
        [Header("Visual States")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _hoverColor = Color.cyan;
        
        private SongEntry _songData;
        private int _songIndex;
        private bool _isSelected = false;
        
        public UnityAction<int> OnSongSelected;
        
        /// <summary>
        /// Event triggered when the song item is hovered.
        /// </summary>
        public UnityAction<SongEntry> OnSongHovered;
        
        private void Awake()
        {
            if (_itemButton != null)
            {
                _itemButton.onClick.AddListener(OnItemClicked);
            }
        }
        
        public void Initialize(SongEntry song, int index)
        {
            _songData = song;
            _songIndex = index;
            
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (_songData == null) return;
            
            // Update text fields
            if (_titleText != null)
                _titleText.text = _songData.Title ?? "Unknown Title";
            
            if (_artistText != null)
                _artistText.text = _songData.Artist ?? "Unknown Artist";
            
            if (_creatorText != null)
                _creatorText.text = _songData.Creator ?? "Unknown Creator";
            
            // Update format information
            if (_formatText != null)
            {
                string format = GetFormatDisplayName(_songData.Format);
                _formatText.text = format;
            }
            
            // Update preview image if available
            if (_previewImage != null && _songData.HasImage())
            {
                string imagePath = _songData.GetBestAvailableImagePath();
                // Load preview image (you'd need to implement image loading)
                // _previewImage.sprite = LoadSprite(imagePath);
                // ConfigureImageForTexture(texture); // Call this after loading
            }
        }
        

        
        private void OnItemClicked()
        {
            OnSongSelected?.Invoke(_songIndex);
        }
        
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            UpdateVisualState();
        }
        
        private void UpdateVisualState()
        {
            if (_titleText != null)
            {
                _titleText.color = _isSelected ? _selectedColor : _normalColor;
            }
            
            if (_artistText != null)
            {
                _artistText.color = _isSelected ? _selectedColor : _normalColor;
            }
            
            if (_creatorText != null)
            {
                _creatorText.color = _isSelected ? _selectedColor : _normalColor;
            }
            
            if (_formatText != null)
            {
                _formatText.color = _isSelected ? _selectedColor : _normalColor;
            }
        }
        
        public void OnPointerEnter()
        {
            if (!_isSelected)
            {
                if (_titleText != null) _titleText.color = _hoverColor;
                if (_artistText != null) _artistText.color = _hoverColor;
                if (_creatorText != null) _creatorText.color = _hoverColor;
                if (_formatText != null) _formatText.color = _hoverColor;
            }
            
            // Trigger hover event for background system
            OnSongHovered?.Invoke(_songData);
        }
        
        public void OnPointerExit()
        {
            if (!_isSelected)
            {
                UpdateVisualState();
            }
            
            // Trigger hover event with null to indicate no song is hovered
            OnSongHovered?.Invoke(null);
        }
        
        public SongEntry GetSongData()
        {
            return _songData;
        }
        
        public int GetSongIndex()
        {
            return _songIndex;
        }
        
        /*
            ConfigureImageForTexture sets up the Image component to display the texture
            without stretching, maintaining aspect ratio and filling the entire component.
            This creates a "fill and crop" effect similar to CSS object-fit: cover.
        */
        public void ConfigureImageForTexture(Texture2D texture)
        {
            if (_previewImage == null || texture == null) return;
            
            // Set the image type and preserve aspect ratio
            _previewImage.type = _imageType;
            _previewImage.preserveAspect = _preserveAspectRatio;
            
            // Configure the image to fill the component while maintaining aspect ratio
            ConfigureImageWithFillAndCrop(texture);
        }
        
        /*
            ConfigureImageWithFillAndCrop sets up the image to fill the entire component
            while maintaining aspect ratio. This may crop parts of the image but ensures
            no stretching occurs.
        */
        private void ConfigureImageWithFillAndCrop(Texture2D texture)
        {
            if (_previewImage == null || texture == null) return;
            
            // Get the RectTransform of the Image component
            RectTransform imageRect = _previewImage.rectTransform;
            if (imageRect == null) return;
            
            // Get the parent container's size
            RectTransform parentRect = imageRect.parent as RectTransform;
            if (parentRect == null)
            {
                // If no parent, use the image's own size
                imageRect.sizeDelta = new Vector2(texture.width, texture.height);
                return;
            }
            
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
                    return "Unknown";
            }
        }
    }
}
