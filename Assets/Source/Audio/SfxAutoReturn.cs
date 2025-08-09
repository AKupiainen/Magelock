using UnityEngine;

namespace MageLock.Audio
{
    public class SfxAutoReturn : MonoBehaviour
    {
        private AudioManager _audioManager;
        private float _returnTime;
        private bool _isActive;
        
        public void Initialize(AudioManager manager)
        {
            _audioManager = manager;
        }
        
        public void SetReturnTime(float time)
        {
            _returnTime = Time.time + time;
            _isActive = true;
        }
        
        private void Update()
        {
            if (_isActive && Time.time >= _returnTime)
            {
                _isActive = false;
                
                if (TryGetComponent(out AudioSource source))
                {
                    _audioManager.ReturnToPool(source);
                }
            }
        }
    }
}