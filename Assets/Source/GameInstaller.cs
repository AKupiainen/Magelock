namespace MageLock.DependencyInjection 
{
    using MageLock.Assets;
    using MageLock.Events;
    using UnityEngine;
    using MageLock.Networking;
    using MageLock.UI;
    using MageLock.Shop;
    using MageLock.Audio;
    using MageLock.Localization;
    using MageLock.Player;
    using MageLock.GameModes;

    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private CurrencyIconMap currencyIconMap;
        [SerializeField] private PlayerNamesData playerNamesData;
        [SerializeField] private LevelData levelData;
        [SerializeField] private DefaultPlayerConfig defaultPlayerConfig;
        [SerializeField] private CharacterDatabase characterDatabase;

        public override void InstallBindings(DIContainer container)
        {
            container.RegisterSingleton(currencyIconMap);
            container.RegisterSingleton(playerNamesData);
            container.RegisterSingleton(levelData);
            container.RegisterSingleton(defaultPlayerConfig);
            container.RegisterSingleton(characterDatabase);

            IEventManager eventManager = new EventManager();
            container.RegisterSingleton(eventManager);

            EventsBus.Initialize(eventManager);

            ViewManager viewManager = new ViewManager();
            viewManager.Initialize();

            container.RegisterSingleton(viewManager);

            IAssetManager assetManager = new AssetManager(currencyIconMap);
            container.RegisterSingleton(assetManager);

            ShopManager shopManager = FindAnyObjectByType<ShopManager>();
            container.RegisterMonoBehaviourSingleton(shopManager);

            AudioManager audioManager = FindAnyObjectByType<AudioManager>();
            container.RegisterMonoBehaviourSingleton(audioManager);

            LocalizationService localizationService = FindAnyObjectByType<LocalizationService>();
            container.RegisterMonoBehaviourSingleton(localizationService);

            NetworkManagerCustom networkManagerCustom = FindAnyObjectByType<NetworkManagerCustom>();
            container.RegisterSingleton(networkManagerCustom);

            PopupManager popupManager = FindAnyObjectByType<PopupManager>();
            container.RegisterMonoBehaviourSingleton(popupManager);
        }
    }
}