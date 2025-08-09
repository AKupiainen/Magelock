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
        
        private SimpleNetworkController _controller;
        private Vector2 _lastSentInput;
        
        private readonly NetworkVariable<PlayerNetworkState> _networkState = new();
        private readonly NetworkVariable<PlayerInfo> _playerInfo = new();
        
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
            _controller = GetComponent<SimpleNetworkController>();
            
            if (IsOwner)
            {
                _controller.SetIsLocalPlayer(true);
                
                SetupCamera();
                
                string playerName = PlayerModel.GetPlayerName();
                UpdatePlayerNameServerRpc(playerName);
            }
            else
            {
                _controller.SetIsLocalPlayer(false);
            }
            
            SetupPlayerBillboard();
            
            _playerInfo.OnValueChanged += OnPlayerInfoChanged;
            _networkState.OnValueChanged += OnNetworkStateChanged;
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            _controller.HandleInput();
            Vector2 currentInput = _controller.GetMoveInput();
            
            if (Vector2.Distance(currentInput, _lastSentInput) > 0.01f)
            {
                if (IsHost)
                {
                    _controller.SetNetworkInput(currentInput);
                }
                else
                {
                    _controller.SetNetworkInput(currentInput);
                    SendInputServerRpc(new InputData { MoveInput = currentInput });
                }
                
                _lastSentInput = currentInput;
            }
        }
        
        private void FixedUpdate()
        {
            if (IsServer)
            {
                _controller.ProcessMovement();
                
                float speed = _controller.GetAnimationSpeed();
                Vector2 currentInput = _controller.GetMoveInput();
                
                _networkState.Value = new PlayerNetworkState 
                { 
                    Speed = speed,
                    LastInput = currentInput
                };
            }
            else if (IsOwner)
            {
                _controller.ProcessMovement();
            }
        }
        
        private void OnNetworkStateChanged(PlayerNetworkState previousValue, PlayerNetworkState newValue)
        {
            if (!IsOwner)
            {
                _controller.SetNetworkInput(newValue.LastInput);
                _controller.SetAnimationSpeed(newValue.Speed);
            }
        }
        
        [ServerRpc]
        private void SendInputServerRpc(InputData inputData)
        {
            _controller.SetNetworkInput(inputData.MoveInput);
        }
        
        [ServerRpc]
        private void UpdatePlayerNameServerRpc(string playerName)
        {
            _playerInfo.Value = new PlayerInfo { PlayerName = playerName };
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
            _playerInfo.OnValueChanged -= OnPlayerInfoChanged;
            _networkState.OnValueChanged -= OnNetworkStateChanged;
            
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

                if (!string.IsNullOrEmpty(_playerInfo.Value.PlayerName.ToString()))
                {
                    playerBillboardText.SetText(_playerInfo.Value.PlayerName.ToString());
                }
            }
            else
            {
                Debug.LogWarning($"BillboardText not assigned for player {OwnerClientId}");
            }
        }
    }
}