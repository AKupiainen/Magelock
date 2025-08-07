using System.Threading.Tasks;
using BrawlLine.Audio;
using BrawlLine.Player;
using BrawlLine.Localization;
using BrawlLine.Shop;
using BrawlLine.DependencyInjection;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;

namespace BrawlLine.Utilies
{
    [CreateAssetMenu(fileName = "BootstrapLoadStep", menuName = "Load Step/Bootstrap Load Step")]
    public class BootstrapLoadStep : LoadStep
    {
        [Inject] private ShopManager shopManager;
        [Inject] private LocalizationService localizationService;
        [Inject] private AudioManager audioManager;

        public override async Task LoadTaskAsync()
        {
            Application.targetFrameRate = 60;
            
            DIContainer.Instance.Inject(this);
            PlayerModel.Initialize();

            var settings = PlayerModel.GetSettings();

            audioManager.InitializeSfxPool();
            audioManager.SetMasterVolume(settings.masterVolume);
            audioManager.SetBGMVolume(settings.musicVolume);
            audioManager.SetSfxVolume(settings.soundEffectsVolume);

            localizationService.SetLanguage(settings.language);

            var options = new InitializationOptions();
            var profile = Guid.NewGuid().ToString()[..8];
            options.SetProfile(profile);

            await UnityServices.InitializeAsync(options);
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            shopManager.InitializeIAP();
            Progress = 1f;
        }
    }
}