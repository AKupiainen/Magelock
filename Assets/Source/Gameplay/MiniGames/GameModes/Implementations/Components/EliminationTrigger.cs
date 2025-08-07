using MageLock.DependencyInjection;
using UnityEngine;
using Unity.Netcode;

namespace MageLock.GameModes
{
    [RequireComponent(typeof(Collider))]
    public class EliminationTrigger : NetworkBehaviour
    {
        [SerializeField] private string eliminationReason = "Eliminated";
        
        [Inject] private GameStateManager gameStateManager;
        
        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;
            
            var networkObject = other.GetComponent<NetworkObject>();
            if (networkObject == null || !networkObject.IsPlayerObject) return;

            var clientId = networkObject.OwnerClientId;
            gameStateManager?.PlayerEliminatedServerRpc(clientId, eliminationReason);
        }
    }
}