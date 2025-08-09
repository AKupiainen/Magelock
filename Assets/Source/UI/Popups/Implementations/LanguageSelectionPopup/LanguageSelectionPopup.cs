using UnityEngine;
using MageLock.Localization;
using MageLock.DependencyInjection;

namespace MageLock.UI
{
    public class LanguageSelectionPopup : Popup
    {
        [SerializeField] private Transform languageButtonContainer;
        [SerializeField] private GameObject languageButtonPrefab;
        
        [Inject] private LocalizationService _localizationService;

        public override void Initialize()
        {
            base.Initialize();
            PopulateLanguageButtons();
        }

        private void PopulateLanguageButtons()
        {
            foreach (Transform child in languageButtonContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var data in _localizationService.LocalizationDatas)
            {
                LanguageButton languageButton = DIContainer.Instance.InstantiateFromPrefab<LanguageButton>(
                    languageButtonPrefab,
                    dontDestroyOnLoad: false
                );

                languageButton.transform.SetParent(languageButtonContainer, false);
                languageButton.Initialize(data.LocalizationLanguage);
            }
        }
    } 
}