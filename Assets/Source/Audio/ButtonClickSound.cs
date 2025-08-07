using BrawlLine.DependencyInjection;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace BrawlLine.Audio
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickSound : MonoBehaviour
    {
        [SerializeField] private Sfx clickSfx;
        [Inject] private AudioManager audioManager;
        
        private Button button;
        
        [UsedImplicitly]
        [PostInject]
        private void InitializeAudio()
        {
            button = GetComponent<Button>();

            if (audioManager == null)
            {
                Debug.LogWarning($"AudioManager not found in scene for button: {gameObject.name}");
                return;
            }

            button.onClick.AddListener(PlayClickSound);
        }
        
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
            }
        }
        
        private void PlayClickSound()
        {
            if (audioManager != null && clickSfx != null)
            {
                audioManager.PlaySfx(clickSfx);
            }
        }
        
        public void SetClickSfx(Sfx sfx)
        {
            clickSfx = sfx;
        }
    }
}