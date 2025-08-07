using UnityEngine;

namespace BrawlLine.Audio
{
    public class SfxAutoReturn : MonoBehaviour
    {
        private AudioManager audioManager;
        private float returnTime;
        private bool isActive;
        
        public void Initialize(AudioManager manager)
        {
            audioManager = manager;
        }
        
        public void SetReturnTime(float time)
        {
            returnTime = Time.time + time;
            isActive = true;
        }
        
        private void Update()
        {
            if (isActive && Time.time >= returnTime)
            {
                isActive = false;
                
                if (TryGetComponent(out AudioSource source))
                {
                    audioManager.ReturnToPool(source);
                }
            }
        }
    }
}