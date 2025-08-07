using Unity.Netcode;
using UnityEngine;
using MageLock.Player;

namespace MageLock.Controls
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject nameDisplay;
        
        private SimpleNetworkController controller;
        
        private readonly NetworkVariable<PlayerNetworkState> networkState = new();
        
        private struct InputData : INetworkSerializable
        {
            public Vector2 MoveInput;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref MoveInput);
            }
        }
        
        private struct PlayerNetworkState : INetworkSerializable
        {
            public float Speed;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Speed);
            }
        }
        
        public override void OnNetworkSpawn()
        {    
            controller = GetComponent<SimpleNetworkController>();
            
            if (IsOwner)
            {
                SetupLocalPlayer();
            }
            else
            {
                DisableLocalComponents();
            }
            
            SetupPlayerDisplay();
        }
        
        private void Update()
        {
            if (IsOwner)
            {
                HandleInputAndSend();
            }
            
            if (IsClient && !IsOwner)
            {
                SyncClientState();
            }
        }
        
        private void FixedUpdate()
        {
            if (IsServer)
            {
                ProcessServerMovement();
            }
        }
        
        private void HandleInputAndSend()
        {
            controller.HandleInput();
            
            Vector2 moveInput = controller.GetMoveInput();
            
            if (IsServer)
            {
                controller.SetNetworkInput(moveInput);
            }
            else
            {
                SendInputServerRpc(new InputData { MoveInput = moveInput });
            }
        }
        
        private void ProcessServerMovement()
        {
            controller.ProcessMovement();
            
            float speed = controller.GetAnimationSpeed();
            networkState.Value = new PlayerNetworkState { Speed = speed };
        }
        
        private void SyncClientState()
        {
            var state = networkState.Value;
            controller.UpdateNetworkAnimations(state.Speed);
        }
        
        [ServerRpc]
        private void SendInputServerRpc(InputData inputData)
        {
            controller.SetNetworkInput(inputData.MoveInput);
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }
    }
}