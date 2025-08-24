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
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI artistText;
        [SerializeField] private TextMeshProUGUI creatorText;
        [SerializeField] private TextMeshProUGUI lengthText;
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button itemButton;
        
        [Header("Visual States")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color hoverColor = Color.cyan;
        
        private SongEntry songData;
        private int songIndex;
        private bool isSelected = false;
        
        public UnityAction<int> OnSongSelected;
        
        private void Awake()
        {
            if (itemButton != null)
            {
                itemButton.onClick.AddListener(OnItemClicked);
            }
        }
        
        public void Initialize(SongEntry song, int index)
        {
            songData = song;
            songIndex = index;
            
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            if (songData == null) return;
            
            // Update text fields
            if (titleText != null)
                titleText.text = songData.Title;
            
            if (artistText != null)
                artistText.text = songData.Artist;
            
            if (creatorText != null)
                creatorText.text = songData.Creator;
            
            // Update length
            if (lengthText != null)
            {
                float totalLength = 0;
                if (songData.Charts != null && songData.Charts.Count > 0)
                {
                    totalLength = songData.Charts[0].TotalLength;
                }
                lengthText.text = FormatTime(totalLength);
            }
            
            // Update difficulty info
            if (difficultyText != null)
            {
                difficultyText.text = GetDifficultyString();
            }
            
            // Update background image if available
            if (backgroundImage != null && !string.IsNullOrEmpty(songData.BackgroundImage))
            {
                // Load background image (you'd need to implement image loading)
                // backgroundImage.sprite = LoadSprite(songData.BackgroundImage);
            }
        }
        
        private string GetDifficultyString()
        {
            if (songData.Charts == null || songData.Charts.Count == 0)
                return "No charts";
            
            var difficulties = songData.Charts.Select(c => c.Difficulty.ToString()).ToArray();
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
            OnSongSelected?.Invoke(songIndex);
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisualState();
        }
        
        private void UpdateVisualState()
        {
            if (titleText != null)
            {
                titleText.color = isSelected ? selectedColor : normalColor;
            }
            
            if (artistText != null)
            {
                artistText.color = isSelected ? selectedColor : normalColor;
            }
            
            if (creatorText != null)
            {
                creatorText.color = isSelected ? selectedColor : normalColor;
            }
        }
        
        public void OnPointerEnter()
        {
            if (!isSelected)
            {
                if (titleText != null) titleText.color = hoverColor;
                if (artistText != null) artistText.color = hoverColor;
                if (creatorText != null) creatorText.color = hoverColor;
            }
        }
        
        public void OnPointerExit()
        {
            if (!isSelected)
            {
                UpdateVisualState();
            }
        }
        
        public SongEntry GetSongData()
        {
            return songData;
        }
        
        public int GetSongIndex()
        {
            return songIndex;
        }
    }
}
