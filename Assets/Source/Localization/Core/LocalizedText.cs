using BrawlLine.DependencyInjection;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace BrawlLine.Localization
{
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string localizationKey;

        [Inject] private LocalizationService localizationService;

        private TextMeshProUGUI textComponent;

        private void Awake()
        {
            textComponent = GetComponent<TextMeshProUGUI>();

            if (textComponent == null)
            {
                Debug.LogError("LocalizedText script must be attached to a GameObject with a TextMeshProUGUI component.");
            }
        }

        [UsedImplicitly]
        [PostInject]
        private void InitializeLocalization()
        {
            if (localizationService == null)
            {
                Debug.LogError($"LocalizationService not injected into LocalizedText on {gameObject.name}");
                return;
            }

            LocalizationService.OnLanguageChangedCallback += UpdateText;
            UpdateText(localizationService.GetCurrentLanguage);
            
            Debug.Log($"[LocalizedText] Initialized localization for {gameObject.name} with key: {localizationKey}");
        }

        private void OnDestroy()
        {
            LocalizationService.OnLanguageChangedCallback -= UpdateText;
        }
        
        private void UpdateText(SystemLanguage language)
        {
            if (textComponent != null && localizationService != null)
            {
                string localizedValue = localizationService.GetLocalizedValue(localizationKey);
                textComponent.text = localizedValue;
            }
        }
    }
}