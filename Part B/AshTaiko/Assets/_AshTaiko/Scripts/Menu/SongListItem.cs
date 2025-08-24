using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using System.Linq;

namespace AshTaiko.Menu
{
    public class SongListItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _artistText;
        [SerializeField] private TextMeshProUGUI _creatorText;
        [SerializeField] private TextMeshProUGUI _lengthText;
        [SerializeField] private TextMeshProUGUI _difficultyText;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Button _itemButton;
        
        [Header("Visual States")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.yellow;
        [SerializeField] private Color _hoverColor = Color.cyan;
        
        private SongEntry _songData;
        private int _songIndex;
        private bool _isSelected = false;
        
        public UnityAction<int> OnSongSelected;
        
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
                _titleText.text = _songData.Title;
            
            if (_artistText != null)
                _artistText.text = _songData.Artist;
            
            if (_creatorText != null)
                _creatorText.text = _songData.Creator;
            
            // Update length
            if (_lengthText != null)
            {
                float totalLength = 0;
                if (_songData.Charts != null && _songData.Charts.Count > 0)
                {
                    totalLength = _songData.Charts[0].TotalLength;
                }
                _lengthText.text = FormatTime(totalLength);
            }
            
            // Update difficulty info
            if (_difficultyText != null)
            {
                _difficultyText.text = GetDifficultyString();
            }
            
            // Update background image if available
            if (_backgroundImage != null && !string.IsNullOrEmpty(_songData.BackgroundImage))
            {
                // Load background image (you'd need to implement image loading)
                // _backgroundImage.sprite = LoadSprite(_songData.BackgroundImage);
            }
        }
        
        private string GetDifficultyString()
        {
            if (_songData.Charts == null || _songData.Charts.Count == 0)
                return "No charts";
            
            var difficulties = _songData.Charts.Select(c => c.Difficulty.ToString()).ToArray();
            return string.Join(", ", difficulties);
        }
        
        private string FormatTime(float timeInSeconds)
        {
            int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
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
        }
        
        public void OnPointerEnter()
        {
            if (!_isSelected)
            {
                if (_titleText != null) _titleText.color = _hoverColor;
                if (_artistText != null) _artistText.color = _hoverColor;
                if (_creatorText != null) _creatorText.color = _hoverColor;
            }
        }
        
        public void OnPointerExit()
        {
            if (!_isSelected)
            {
                UpdateVisualState();
            }
        }
        
        public SongEntry GetSongData()
        {
            return _songData;
        }
        
        public int GetSongIndex()
        {
            return _songIndex;
        }
    }
}
