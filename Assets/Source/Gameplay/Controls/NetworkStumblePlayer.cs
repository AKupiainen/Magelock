using Unity.Netcode;
using UnityEngine;
using MageLock.Gameplay;
using MageLock.Player;

namespace MageLock.Controls
{
    public class NetworkStumblePlayer : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private BillboardText playerBillboardText;
        
        private StumbleNetworkController controller;
        
        private readonly NetworkVariable<PlayerNetworkState> networkState = new();
        
        private struct InputData : INetworkSerializable
        {
            public Vector2 MoveInput;
            public float JumpInput; // Changed from bool to float
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref MoveInput);
                serializer.SerializeValue(ref JumpInput);
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
            controller = GetComponent<StumbleNetworkController>();
            
            if (IsOwner)
            {
                SetupCamera();
            }
            
            SetupPlayerBillboard();
        }
        
        private void Update()
        {
            if (IsOwner)
            {
                HandleInputAndSend();
            }
            
            if (IsClient)
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
            float jumpInput = controller.GetJumpInput(); // Changed from GetJumpPressed()
            
            if (IsServer)
            {
                controller.SetNetworkInput(moveInput, jumpInput);
            }
            else
            {
                SendInputServerRpc(new InputData 
                { 
                    MoveInput = moveInput, 
                    JumpInput = jumpInput
                });
            }
        }
        
        private void ProcessServerMovement()
        {
            controller.ProcessMovement();
            
            float speed = controller.GetAnimationSpeed();
            
            networkState.Value = new PlayerNetworkState
            {
                Speed = speed
            };
        }
        
        private void SyncClientState()
        {
            var state = networkState.Value;
            controller.UpdateNetworkAnimations(state.Speed);
        }
        
        [ServerRpc]
        private void SendInputServerRpc(InputData inputData)
        {
            controller.SetNetworkInput(inputData.MoveInput, inputData.JumpInput);
        }
        
        public override void OnNetworkDespawn()
        {
            if (IsOwner && cameraController != null)
            {
                cameraController.SetTarget(null);
            }
            
            base.OnNetworkDespawn();
        }
        
        private void SetupCamera()
        {
            if (cameraController != null)
            {
                cameraController.gameObject.SetActive(true);
                cameraController.SetTarget(transform);
                Debug.Log($"Camera set to follow player {OwnerClientId}");
            }
            else
            {
                Debug.LogWarning("CameraController not found!");
            }
        }
        
        private void SetupPlayerBillboard()
        {
            if (playerBillboardText != null)
            {
                playerBillboardText.SetText(PlayerModel.GetPlayerName());
            }
        }
    }
}