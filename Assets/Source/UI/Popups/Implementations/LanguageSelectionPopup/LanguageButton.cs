using UnityEngine;
using UnityEngine.UI;
using MageLock.Localization;
using MageLock.Player;
using MageLock.DependencyInjection;

namespace MageLock.UI
{
    [RequireComponent(typeof(Button))]
    public class LanguageButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image languageFlagImage;
        [SerializeField] private GameObject checkmarkObject;

        [Inject] private LocalizationService _localizationService;

        private SystemLanguage _language;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnButtonClicked);
        }

        public void Initialize(SystemLanguage languageToSet)
        {
            _language = languageToSet;
            languageFlagImage.sprite = _localizationService.GetLanguageData.GetLanguageImage(languageToSet);
            checkmarkObject.SetActive(PlayerModel.GetSettings().language == languageToSet);
        }

        private void OnButtonClicked()
        {
            _localizationService.SetLanguage(this._language);
            PopupController.CloseCurrentPopup();
        }
    }
}