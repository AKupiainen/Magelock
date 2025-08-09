using System.Threading.Tasks;
using MageLock.Audio;
using MageLock.Player;
using MageLock.Localization;
using MageLock.Shop;
using MageLock.DependencyInjection;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;

namespace MageLock.Utilies
{
    [CreateAssetMenu(fileName = "BootstrapLoadStep", menuName = "Load Step/Bootstrap Load Step")]
    public class BootstrapLoadStep : LoadStep
    {
        [Inject] private ShopManager _shopManager;
        [Inject] private LocalizationService _localizationService;
        [Inject] private AudioManager _audioManager;

        public override async Task LoadTaskAsync()
        {
            Application.targetFrameRate = 60;
            
            DIContainer.Instance.Inject(this);
            PlayerModel.Initialize();

            var settings = PlayerModel.GetSettings();

            _audioManager.InitializeSfxPool();
            _audioManager.SetMasterVolume(settings.masterVolume);
            _audioManager.SetBGMVolume(settings.musicVolume);
            _audioManager.SetSfxVolume(settings.soundEffectsVolume);

            _localizationService.SetLanguage(settings.language);

            var options = new InitializationOptions();
            var profile = Guid.NewGuid().ToString()[..8];
            options.SetProfile(profile);

            await UnityServices.InitializeAsync(options);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            _shopManager.InitializeIAP();
            Progress = 1f;
        }
    }
}