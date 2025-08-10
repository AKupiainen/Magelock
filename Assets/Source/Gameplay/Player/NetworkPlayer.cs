using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using MageLock.Player;
using MageLock.Gameplay;
using MageLock.Spells;
using MageLock.Events;

namespace MageLock.Controls
{
    public class PlayerHealthChangedEvent : IEventData
    {
        public ulong ClientId { get; set; }
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
        public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0;
    }
    
    public class PlayerDamagedEvent : IEventData
    {
        public ulong ClientId { get; set; }
        public float Damage { get; set; }
        public float RemainingHealth { get; set; }
        public Vector3 Position { get; set; }
    }
    
    public class PlayerHealedEvent : IEventData
    {
        public ulong ClientId { get; set; }
        public float HealAmount { get; set; }
        public float CurrentHealth { get; set; }
        public float MaxHealth { get; set; }
    }
    
    public class PlayerDeathEvent : IEventData
    {
        public ulong ClientId { get; set; }
        public string PlayerName { get; set; }
        public Vector3 DeathPosition { get; set; }
    }
    
    public class NetworkPlayer : NetworkBehaviour, IHealth
    {
        [Header("References")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private BillboardText playerBillboardText;
        
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float startingHealth = 100f;
        
        private SimpleNetworkController _controller;
        private Vector2 _lastSentInput;
        private bool _isDead;
        
        private readonly NetworkVariable<PlayerNetworkState> _networkState = new();
        private readonly NetworkVariable<PlayerInfo> _playerInfo = new();
        private readonly NetworkVariable<HealthData> _healthData = new();
        
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
        
        private struct HealthData : INetworkSerializable
        {
            public float CurrentHealth;
            public float MaxHealth;
            public bool IsDead;
            
            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref CurrentHealth);
                serializer.SerializeValue(ref MaxHealth);
                serializer.SerializeValue(ref IsDead);
            }
        }
        
        public override void OnNetworkSpawn()
        {    
            _controller = GetComponent<SimpleNetworkController>();
            
            if (IsServer)
            {
                _healthData.Value = new HealthData
                {
                    CurrentHealth = startingHealth,
                    MaxHealth = maxHealth,
                    IsDead = false
                };
            }
            
            if (IsOwner)
            {
                _controller.SetIsLocalPlayer(true);
                
                SetupCamera();
                UpdatePlayerNameServerRpc(PlayerModel.GetPlayerName());
            }
            else
            {
                _controller.SetIsLocalPlayer(false);
            }
            
            SetupPlayerBillboard();
            
            _playerInfo.OnValueChanged += OnPlayerInfoChanged;
            _networkState.OnValueChanged += OnNetworkStateChanged;
            _healthData.OnValueChanged += OnHealthDataChanged;
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
            if (!IsOwner && !_isDead)
            {
                _controller.SetNetworkInput(newValue.LastInput);
                _controller.SetAnimationSpeed(newValue.Speed);
            }
        }
        
        private void OnHealthDataChanged(HealthData previousValue, HealthData newValue)
        {
            _isDead = newValue.IsDead;
            
            EventsBus.Trigger(new PlayerHealthChangedEvent
            {
                ClientId = OwnerClientId,
                CurrentHealth = newValue.CurrentHealth,
                MaxHealth = newValue.MaxHealth
            });
            
            
            if (!previousValue.IsDead && newValue.IsDead)
            {
                HandleDeath();
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
            _healthData.OnValueChanged -= OnHealthDataChanged;
            
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

        #region IHealth Implementation
        
        public void TakeDamage(float damage)
        {
            if (!IsServer) 
            {
                TakeDamageServerRpc(damage);
                return;
            }
            
            if (_healthData.Value.IsDead) return;
            
            var currentData = _healthData.Value;
            float actualDamage = Mathf.Min(damage, currentData.CurrentHealth);
            currentData.CurrentHealth = Mathf.Max(0, currentData.CurrentHealth - damage);
            
            if (currentData.CurrentHealth <= 0 && !currentData.IsDead)
            {
                currentData.IsDead = true;
                currentData.CurrentHealth = 0;
            }
            
            _healthData.Value = currentData;
            
            EventsBus.Trigger(new PlayerDamagedEvent
            {
                ClientId = OwnerClientId,
                Damage = actualDamage,
                RemainingHealth = currentData.CurrentHealth,
                Position = transform.position
            });
            
            Debug.Log($"Player {OwnerClientId} took {actualDamage} damage. Health: {currentData.CurrentHealth}/{currentData.MaxHealth}");
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void TakeDamageServerRpc(float damage)
        {
            TakeDamage(damage);
        }

        public void Heal(float amount)
        {
            if (!IsServer)
            {
                HealServerRpc(amount);
                return;
            }
            
            if (_healthData.Value.IsDead) 
            {
                Debug.Log($"Cannot heal player {OwnerClientId} - player is dead");
                return;
            }
            
            var currentData = _healthData.Value;
            float previousHealth = currentData.CurrentHealth;
            currentData.CurrentHealth = Mathf.Min(currentData.MaxHealth, currentData.CurrentHealth + amount);
            float actualHeal = currentData.CurrentHealth - previousHealth;
            
            _healthData.Value = currentData;
            
            if (actualHeal > 0)
            {
                EventsBus.Trigger(new PlayerHealedEvent
                {
                    ClientId = OwnerClientId,
                    HealAmount = actualHeal,
                    CurrentHealth = currentData.CurrentHealth,
                    MaxHealth = currentData.MaxHealth
                });
            }
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void HealServerRpc(float amount)
        {
            Heal(amount);
        }

        public float GetHealth()
        {
            return _healthData.Value.CurrentHealth;
        }

        public float GetMaxHealth()
        {
            return _healthData.Value.MaxHealth;
        }
        

        #endregion
        
        #region Health Helper Methods
        
        private void HandleDeath()
        {
            EventsBus.Trigger(new PlayerDeathEvent
            {
                ClientId = OwnerClientId,
                PlayerName = _playerInfo.Value.PlayerName.ToString(),
                DeathPosition = transform.position
            });
            
            if (_controller != null)
            {
                _controller.enabled = false;
            }
            
            if (IsOwner)
            {
                _controller.SetNetworkInput(Vector2.zero);
            }
        }
        
        public void SetMaxHealth(float newMaxHealth, bool maintainHealthPercentage = true)
        {
            if (!IsServer)
            {
                SetMaxHealthServerRpc(newMaxHealth, maintainHealthPercentage);
                return;
            }
            
            if (_healthData.Value.IsDead) 
            {
                Debug.Log($"Cannot modify max health for dead player {OwnerClientId}");
                return;
            }
            
            var currentData = _healthData.Value;
            float healthPercentage = currentData.CurrentHealth / currentData.MaxHealth;
            currentData.MaxHealth = newMaxHealth;
            
            if (maintainHealthPercentage)
            {
                currentData.CurrentHealth = newMaxHealth * healthPercentage;
            }
            else
            {
                currentData.CurrentHealth = Mathf.Min(currentData.CurrentHealth, newMaxHealth);
            }
            
            _healthData.Value = currentData;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void SetMaxHealthServerRpc(float newMaxHealth, bool maintainHealthPercentage)
        {
            SetMaxHealth(newMaxHealth, maintainHealthPercentage);
        }
        
        #endregion
    }
}