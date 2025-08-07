using UnityEngine;
using UnityEngine.UI;
using BrawlLine.Localization;
using BrawlLine.Player;
using BrawlLine.DependencyInjection;

namespace BrawlLine.UI
{
    [RequireComponent(typeof(Button))]
    public class LanguageButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image languageFlagImage;
        [SerializeField] private GameObject checkmarkObject;

        [Inject] private LocalizationService localizationService;

        private SystemLanguage language;
        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClicked);
        }

        public void Initialize(SystemLanguage languageToSet)
        {
            this.language = languageToSet;
            languageFlagImage.sprite = localizationService.GetLanguageData.GetLanguageImage(languageToSet);
            checkmarkObject.SetActive(PlayerModel.GetSettings().language == languageToSet);
        }

        private void OnButtonClicked()
        {
            localizationService.SetLanguage(this.language);
            PopupController.CloseCurrentPopup();
        }
    }
}