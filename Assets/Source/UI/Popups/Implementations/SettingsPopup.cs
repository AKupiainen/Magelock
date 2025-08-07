using UnityEngine;
using UnityEngine.UI;
using BrawlLine.Player;
using BrawlLine.Localization;
using BrawlLine.Audio;
using TMPro;
using BrawlLine.DependencyInjection;

namespace BrawlLine.UI
{
    public class SettingsPopup : Popup
    {
        [Header("Sliders")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider soundSlider;

        [Header("Slider Icons")]
        [SerializeField] private Image musicIcon;
        [SerializeField] private Sprite musicOnSprite;
        [SerializeField] private Sprite musicOffSprite;

        [SerializeField] private Image soundIcon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;

        [Header("Vibration")]
        [SerializeField] private Slider vibrationSlider;

        [Header("Localization")]
        [SerializeField] private Button languageButton;
        [SerializeField] private TextMeshProUGUI languageLabel;
        [SerializeField] private Image languageFlagImage;

        [Inject] private LocalizationService localizationService;
        [Inject] private AudioManager audioManager;

        private SettingsData currentSettings;

        public override void Initialize()
        {
            base.Initialize();

            currentSettings = new SettingsData(PlayerModel.GetSettings());

            SetupSliders();
            SetupVibrationToggle();
            SetupLanguageButton();

            UpdateLanguageDisplay(currentSettings.language);
            UpdateVibrationVisual(currentSettings.vibrationEnabled);
            UpdateSliderIcons();

            LocalizationService.OnLanguageChangedCallback += OnLanguageChanged;
        }

        private void OnDisable()
        {
            SaveChanges();

            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            soundSlider.onValueChanged.RemoveListener(OnSoundChanged);
            vibrationSlider.onValueChanged.RemoveListener(OnVibrationChanged);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            LocalizationService.OnLanguageChangedCallback -= OnLanguageChanged;
        }

        private void SetupSliders()
        {
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            soundSlider.onValueChanged.AddListener(OnSoundChanged);

            musicSlider.value = currentSettings.musicVolume;
            soundSlider.value = currentSettings.soundEffectsVolume;
        }

        private void SetupVibrationToggle()
        {
            vibrationSlider.onValueChanged.RemoveAllListeners();
            vibrationSlider.value = currentSettings.vibrationEnabled ? 1f : 0f;
            vibrationSlider.onValueChanged.AddListener(OnVibrationChanged);
        }

        private void SetupLanguageButton()
        {
            languageButton.onClick.RemoveAllListeners();
            languageButton.onClick.AddListener(OnLanguageButtonClicked);
        }

        private void OnMusicChanged(float value)
        {
            currentSettings.musicVolume = value;
            audioManager.SetBGMVolume(value);
            
            UpdateSliderIcons();
        }

        private void OnSoundChanged(float value)
        {
            currentSettings.soundEffectsVolume = value;
            audioManager.SetSfxVolume(value);

            UpdateSliderIcons();
        }

        private void OnVibrationChanged(float value)
        {
            bool isOn = value > 0.5f;
            currentSettings.vibrationEnabled = isOn;
            UpdateVibrationVisual(isOn);
        }

        private void OnLanguageButtonClicked()
        {
            PopupController.ShowPopup(PopupType.LanguageSelection);
        }

        private void OnLanguageChanged(SystemLanguage language)
        {
            currentSettings.language = language;
            UpdateLanguageDisplay(language);
        }

        private void UpdateLanguageDisplay(SystemLanguage language)
        {
            languageFlagImage.sprite = localizationService.GetLanguageData.GetLanguageImage(language);
            languageLabel.text = localizationService.GetLanguageData.GetDisplayName(language);
        }

        private void UpdateVibrationVisual(bool isOn)
        {
            vibrationSlider.value = isOn ? 1f : 0f;
        }

        private void UpdateSliderIcons()
        {
            if (musicIcon != null)
            {
                musicIcon.sprite = musicSlider.value > 0f ? musicOnSprite : musicOffSprite;
            }

            if (soundIcon != null)
            {
                soundIcon.sprite = soundSlider.value > 0f ? soundOnSprite : soundOffSprite;
            }
        }

        public void SaveChanges()
        {
            PlayerModel.SetSettings(currentSettings);
        }
    }
}