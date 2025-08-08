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
        private Vector2 lastSentInput;
        
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
            public Vector2 LastInput;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref Speed);
                serializer.SerializeValue(ref LastInput);
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
                controller.SetIsLocalPlayer(true);
                
                SetupCamera();
                
                string playerName = PlayerModel.GetPlayerName();
                UpdatePlayerNameServerRpc(playerName);
            }
            else
            {
                controller.SetIsLocalPlayer(false);
            }
            
            SetupPlayerBillboard();
            
            playerInfo.OnValueChanged += OnPlayerInfoChanged;
            networkState.OnValueChanged += OnNetworkStateChanged;
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            controller.HandleInput();
            Vector2 currentInput = controller.GetMoveInput();
            
            if (Vector2.Distance(currentInput, lastSentInput) > 0.01f)
            {
                if (IsHost)
                {
                    controller.SetNetworkInput(currentInput);
                }
                else
                {
                    controller.SetNetworkInput(currentInput);
                    SendInputServerRpc(new InputData { MoveInput = currentInput });
                }
                
                lastSentInput = currentInput;
            }
        }
        
        private void FixedUpdate()
        {
            if (IsServer)
            {
                controller.ProcessMovement();
                
                float speed = controller.GetAnimationSpeed();
                Vector2 currentInput = controller.GetMoveInput();
                
                networkState.Value = new PlayerNetworkState 
                { 
                    Speed = speed,
                    LastInput = currentInput
                };
            }
            else if (IsOwner)
            {
                controller.ProcessMovement();
            }
        }
        
        private void OnNetworkStateChanged(PlayerNetworkState previousValue, PlayerNetworkState newValue)
        {
            if (!IsOwner)
            {
                controller.SetNetworkInput(newValue.LastInput);
                controller.SetAnimationSpeed(newValue.Speed);
            }
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
            if (playerBillboardText != null)
            {
                playerBillboardText.SetText(newValue.PlayerName.ToString());
            }
        }
        
        public override void OnNetworkDespawn()
        {
            playerInfo.OnValueChanged -= OnPlayerInfoChanged;
            networkState.OnValueChanged -= OnNetworkStateChanged;
            
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
                Debug.LogWarning("CameraController not assigned in inspector for player!");
            }
        }
        
        private void SetupPlayerBillboard()
        {
            if (playerBillboardText != null)
            {
                playerBillboardText.gameObject.SetActive(true);

                if (!string.IsNullOrEmpty(playerInfo.Value.PlayerName.ToString()))
                {
                    playerBillboardText.SetText(playerInfo.Value.PlayerName.ToString());
                }
            }
            else
            {
                Debug.LogWarning($"BillboardText not assigned for player {OwnerClientId}");
            }
        }
    }
}