using MageLock.DependencyInjection;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace MageLock.Localization
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;

        [Inject] private LocalizationService _localizationService;

        private TextMeshProUGUI _textComponent;

        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();

            if (_textComponent == null)
            {
                Debug.LogError("LocalizedText script must be attached to a GameObject with a TextMeshProUGUI component.");
            }
        }

        [UsedImplicitly]
        [PostInject]
        private void InitializeLocalization()
        {
            if (_localizationService == null)
            {
                Debug.LogError($"LocalizationService not injected into LocalizedText on {gameObject.name}");
                return;
            }

            LocalizationService.OnLanguageChangedCallback += UpdateText;
            UpdateText(_localizationService.GetCurrentLanguage);
            
            Debug.Log($"[LocalizedText] Initialized localization for {gameObject.name} with key: {localizationKey}");
        }

        private void OnDestroy()
        {
            LocalizationService.OnLanguageChangedCallback -= UpdateText;
        }
        
        private void UpdateText(SystemLanguage language)
        {
            if (_textComponent != null && _localizationService != null)
            {
                string localizedValue = _localizationService.GetLocalizedValue(localizationKey);
                _textComponent.text = localizedValue;
            }
        }
    }
}