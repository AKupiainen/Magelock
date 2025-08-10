using UnityEngine;
using Unity.Netcode;

namespace MageLock.Networking
{
    public class NetworkDespawner : MonoBehaviour
    {
        private float _despawnTime;
        private NetworkObject _networkObject;
        
        public void Initialize(float duration)
        {
            _despawnTime = Time.time + duration;
            _networkObject = GetComponent<NetworkObject>();
        }
        
        private void Update()
        {
            if (Time.time >= _despawnTime)
            {
                if (_networkObject != null && _networkObject.IsSpawned)
                {
                    _networkObject.Despawn();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}