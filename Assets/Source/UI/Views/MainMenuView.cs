using UnityEngine;
using UnityEngine.UI;
using BrawlLine.Player;
using BrawlLine.Events;
using BrawlLine.ModelRenderer;

namespace BrawlLine.UI
{
    public class MainMenuView : BaseView
    {
        [Header("Main Menu Buttons")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button charactersButton;
        
        [Header("Character Display")]
        [SerializeField] private ModelPrefabInstantiator characterInstantiator;
        [SerializeField] private ModelRenderGraphic characterRenderer;
        [SerializeField] private MouseRotationHandler mouseRotationHandler;
        
        private void OnEnable()
        {
            EventsBus.Subscribe<CharacterSelectedEvent>(OnCharacterSelected);
        }
        
        private void OnDisable()
        {
            EventsBus.Unsubscribe<CharacterSelectedEvent>(OnCharacterSelected);
        }
        
        protected override void Initialize()
        {
            InitializeButtons();
            InitializeCharacterDisplay();
        }
        
        protected override void UpdateLocalizedText() { }

        private void InitializeButtons()
        {
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OnShopClicked);
            }
            
            if (charactersButton != null)
            {
                charactersButton.onClick.AddListener(OnCharactesClicked);
            }
        }
        
        private void InitializeCharacterDisplay()
        {
            if (characterInstantiator == null)
            {
                Debug.LogWarning("Character instantiator is not assigned", this);
                return;
            }
            
            if (characterRenderer == null)
            {
                Debug.LogWarning("Character renderer is not assigned", this);
                return;
            }
            
            UpdateCharacterDisplay();
        }

        private void UpdateCharacterDisplay()
        {
            var selectedCharacter = PlayerModel.GetSelectedCharacter();

            if (selectedCharacter?.menuPrefab != null)
            {
                Transform characterTransform = characterInstantiator.ChangeModelPrefab(selectedCharacter.menuPrefab);
                Debug.Log($"Displaying character: {selectedCharacter.name} in main menu");
                
                UpdateRotationTarget(characterTransform);
            }
            else
            {
                Debug.LogWarning("No character selected or character prefab is null");
            }
        }
        
        private void UpdateRotationTarget(Transform characterTransform)
        {
            if (mouseRotationHandler != null && characterTransform != null)
            {
                mouseRotationHandler.SetTarget(characterTransform);
            }
            else if (mouseRotationHandler != null)
            {
                Debug.LogWarning("Could not find character transform for rotation");
            }
        }
        
        private void OnCharacterSelected(CharacterSelectedEvent eventData)
        {
            UpdateCharacterDisplay();
        }
        
        private void OnSettingsClicked()
        {
            PopupController.ShowPopup(PopupType.Settings, PopupOptions.DisableMainMenu);
        }
        
        private void OnPlayClicked()
        {
            PopupController.ShowPopup(PopupType.MatchMaking, PopupOptions.DisableMainMenu);
        }

        private void OnShopClicked()
        {
            PopupController.ShowPopup(PopupType.Shop, PopupOptions.HideAllExcept(ViewType.Currency));
        }
        
        private void OnCharactesClicked()
        {
            PopupController.ShowPopup(PopupType.Characters, PopupOptions.HideAllExcept(ViewType.Currency));
        }

#if UNITY_EDITOR
        protected new void OnValidate()
        {
            base.OnValidate();

            if (settingsButton == null)
            {
                Debug.LogWarning("Settings button is not assigned", this);
            }

            if (playButton == null)
            {
                Debug.LogWarning("Play button is not assigned", this);
            }

            if (characterInstantiator == null)
            {
                Debug.LogWarning("Character instantiator is not assigned", this);
            }

            if (characterRenderer == null)
            {
                Debug.LogWarning("Character renderer is not assigned", this);
            }

            if (mouseRotationHandler == null)
            {
                Debug.LogWarning("Mouse rotation handler is not assigned", this);
            }

            if (charactersButton == null)
            {
                Debug.LogWarning("Character button is not assigned", this);
            }
        }
#endif
    }
}