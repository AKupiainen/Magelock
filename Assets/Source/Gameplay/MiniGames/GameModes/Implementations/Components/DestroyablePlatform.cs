using UnityEngine;
using Unity.Netcode;

namespace MageLock.GameModes
{
    public class DestroyablePlatform : NetworkBehaviour
    {
        [SerializeField] private ChildCollisionDetector[] childDetectors;
        
        private bool isDestroyed;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            SetupChildDetectors();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            UnsubscribeFromChildDetectors();
        }

        private void SetupChildDetectors()
        {
            foreach (var detector in childDetectors)
            {
                if (detector != null)
                {
                    detector.OnChildTriggerEnter += HandleChildTriggerEnter;
                    detector.OnChildCollisionEnter += HandleChildCollisionEnter;
                }
            }
        }

        private void UnsubscribeFromChildDetectors()
        {
            if (childDetectors != null)
            {
                foreach (var detector in childDetectors)
                {
                    if (detector != null)
                    {
                        detector.OnChildTriggerEnter -= HandleChildTriggerEnter;
                        detector.OnChildCollisionEnter -= HandleChildCollisionEnter;
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleTriggerEnter(other);
        }

        private void HandleChildTriggerEnter(Collider other)
        {
            HandleTriggerEnter(other);
        }

        private void HandleChildCollisionEnter(Collider other)
        {
            HandleTriggerEnter(other);
        }

        private void HandleTriggerEnter(Collider other)
        {
            if (!IsServer || isDestroyed || !IsSpawned) return;
            
            var networkObject = other.GetComponent<NetworkObject>();
            if (networkObject == null || !networkObject.IsPlayerObject) return;

            isDestroyed = true;

            DestroyPlatform();
        }

        private void DestroyPlatform()
        {
            if (!IsServer || !IsSpawned) return;

            if (TryGetComponent<NetworkObject>(out var netObj) && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}