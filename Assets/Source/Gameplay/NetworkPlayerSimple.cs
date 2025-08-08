using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using MageLock.Player;
using MageLock.Gameplay;

namespace MageLock.Controls
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private BillboardText playerBillboardText;
        
        private SimpleNetworkController controller;
        
        private readonly NetworkVariable<PlayerNetworkState> networkState = new();
        private readonly NetworkVariable<PlayerInfo> playerInfo = new();
        
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
        
        private struct PlayerInfo : INetworkSerializable
        {
            public FixedString64Bytes PlayerName;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref PlayerName);
            }
        }
        
        public override void OnNetworkSpawn()
        {    
            controller = GetComponent<SimpleNetworkController>();
            
            if (IsOwner)
            {
                SetupCamera();
                
                // Send player name to server
                string playerName = PlayerModel.GetPlayerName();
                UpdatePlayerNameServerRpc(playerName);
            }
            
            SetupPlayerBillboard();
            
            // Subscribe to player info changes
            playerInfo.OnValueChanged += OnPlayerInfoChanged;
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
        
        [ServerRpc]
        private void UpdatePlayerNameServerRpc(string playerName)
        {
            playerInfo.Value = new PlayerInfo { PlayerName = playerName };
        }
        
        private void OnPlayerInfoChanged(PlayerInfo previousValue, PlayerInfo newValue)
        {
            // Update billboard text when network variable changes
            if (playerBillboardText != null)
            {
                playerBillboardText.SetText(newValue.PlayerName.ToString());
            }
        }
        
        public override void OnNetworkDespawn()
        {
            playerInfo.OnValueChanged -= OnPlayerInfoChanged;
            
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
                // Try to find camera in scene
                cameraController = FindObjectOfType<CameraController>();
                if (cameraController != null)
                {
                    cameraController.SetTarget(transform);
                    Debug.Log($"Found and assigned camera to player {OwnerClientId}");
                }
                else
                {
                    Debug.LogWarning("CameraController not found in scene!");
                }
            }
        }
        
        private void SetupPlayerBillboard()
        {
            if (playerBillboardText != null)
            {
                // For local player, hide the billboard
                if (IsOwner)
                {
                    playerBillboardText.gameObject.SetActive(false);
                }
                else
                {
                    playerBillboardText.gameObject.SetActive(true);
                    // Set initial name if available
                    if (!string.IsNullOrEmpty(playerInfo.Value.PlayerName.ToString()))
                    {
                        playerBillboardText.SetText(playerInfo.Value.PlayerName.ToString());
                    }
                }
            }
            else
            {
                Debug.LogWarning($"BillboardText not assigned for player {OwnerClientId}");
            }
        }
    }
}