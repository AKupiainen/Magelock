using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrawlLine.Player;
using BrawlLine.Events;

namespace BrawlLine.UI
{
    public class PlayerProfileView : BaseView
    {
        [Header("Profile UI References")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider experienceSlider;
        [SerializeField] private TextMeshProUGUI experienceText;
        
        protected override void Initialize()
        {
            base.Initialize();
            UpdateProfileDisplay();
        }
        
        protected override void OnShow()
        {
            base.OnShow();
            UpdateProfileDisplay();
        }
        
        protected override void SubscribeToEvents()
        {
            base.SubscribeToEvents();

            EventsBus.Subscribe<PlayerNameChangedEvent>(OnPlayerNameChanged);
            EventsBus.Subscribe<PlayerLevelChangedEvent>(OnPlayerLevelChanged);
            EventsBus.Subscribe<PlayerExperienceChangedEvent>(OnPlayerExperienceChanged);
            EventsBus.Subscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        }
        
        protected override void UnsubscribeFromEvents()
        {
            base.UnsubscribeFromEvents();

            EventsBus.Unsubscribe<PlayerNameChangedEvent>(OnPlayerNameChanged);
            EventsBus.Unsubscribe<PlayerLevelChangedEvent>(OnPlayerLevelChanged);
            EventsBus.Unsubscribe<PlayerExperienceChangedEvent>(OnPlayerExperienceChanged);
            EventsBus.Unsubscribe<PlayerLevelUpEvent>(OnPlayerLevelUp);
        }
        
        private void OnPlayerNameChanged(PlayerNameChangedEvent eventData)
        {
            UpdatePlayerName();
        }
        
        private void OnPlayerLevelChanged(PlayerLevelChangedEvent eventData)
        {
            UpdateLevel();
        }
        
        private void OnPlayerExperienceChanged(PlayerExperienceChangedEvent eventData)
        {
            UpdateExperience();
        }
        
        private void OnPlayerLevelUp(PlayerLevelUpEvent eventData)
        {
            OnLevelUp(eventData.OldLevel, eventData.NewLevel);
        }
        
        public void UpdateProfileDisplay()
        {
            UpdatePlayerName();
            UpdateLevel();
            UpdateExperience();
        }
        
        private void UpdatePlayerName()
        {
            if (playerNameText != null)
            {
                playerNameText.text = PlayerModel.GetPlayerName();
            }
        }
        
        private void UpdateLevel()
        {
            int currentLevel = PlayerModel.GetPlayerLevel();
            
            if (levelText != null)
            {
                levelText.text = currentLevel.ToString();
            }
        }
        
        private void UpdateExperience()
        {
            int currentExp = PlayerModel.GetCurrentExperience();
            int expForNextLevel = PlayerModel.GetExperienceForNextLevel();
            float progress = PlayerModel.GetLevelProgressPercentage();
            
            if (experienceSlider != null)
            {
                experienceSlider.value = progress;
            }
            
            if (experienceText != null)
            {
                experienceText.text = $"{currentExp}/{expForNextLevel}";
            }
        }
        
        public void RefreshDisplay()
        {
            UpdateProfileDisplay();
        }
        
        public void AddExperienceWithFeedback(int amount)
        {
            int oldLevel = PlayerModel.GetPlayerLevel();
            PlayerModel.AddExperience(amount);
            int newLevel = PlayerModel.GetPlayerLevel();
            
            UpdateProfileDisplay();
            
            if (newLevel > oldLevel)
            {
                OnLevelUp(oldLevel, newLevel);
            }
        }
        
        private void OnLevelUp(int oldLevel, int newLevel)
        {
            Debug.Log($"Player leveled up from {oldLevel} to {newLevel}!");
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (playerNameText == null)
            {
                Debug.LogWarning("Player Name Text is not assigned", this);
            }
            
            if (levelText == null)
            {
                Debug.LogWarning("Level Text is not assigned", this);
            }
            
            if (experienceSlider == null)
            {
                Debug.LogWarning("Experience Slider is not assigned", this);
            }
            
            if (experienceText == null)
            {
                Debug.LogWarning("Experience Text is not assigned", this);
            }
        }
#endif
    }
}