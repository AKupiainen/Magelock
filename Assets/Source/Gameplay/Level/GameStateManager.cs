using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using MageLock.DependencyInjection;
using MageLock.Networking;

namespace MageLock.Gameplay
{
    public enum GameState
    {
        WaitingForPlayers,
        Playing,
        GameOver
    }

    public class GameStateManager : NetworkBehaviour
    {
        [Inject] private NetworkManagerCustom _networkManagerCustom;
        private PlayerSpawnManager _playerSpawnManager;
        
        private readonly NetworkVariable<GameState> _currentState = new();
        private readonly NetworkVariable<float> _matchTimer = new();
        
        private ulong _player1ClientId = ulong.MaxValue;
        private ulong _player2ClientId = ulong.MaxValue;
        private readonly float _matchDuration = 120f; 
        
        public event Action<GameState> OnGameStateChanged;
        public event Action<ulong> OnGameWon;
        
        public GameState CurrentState => _currentState.Value;
        public float MatchTimer => _matchTimer.Value;
        public bool IsFull => _player1ClientId != ulong.MaxValue && _player2ClientId != ulong.MaxValue;

        private void Awake()
        {
            DIContainer.Instance.Inject(this);
            _playerSpawnManager = new PlayerSpawnManager();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                _networkManagerCustom.OnClientDisconnectCallback += OnPlayerDisconnected;
                
                _playerSpawnManager.SpawnAllPlayers();
                SetupPlayers();
            }

            _currentState.OnValueChanged += (prev, current) => OnGameStateChanged?.Invoke(current);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                _networkManagerCustom.OnClientDisconnectCallback -= OnPlayerDisconnected;
            }
            
            if (_playerSpawnManager != null)
            {
                _playerSpawnManager.Cleanup();
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (_currentState.Value == GameState.Playing)
            {
                UpdateMatch();
            }
        }

        #region Server Methods

        private void SetupPlayers()
        {
            if (!IsServer) return;

            foreach (var clientId in _networkManagerCustom.ConnectedClientsIds)
            {
                if (_player1ClientId == ulong.MaxValue)
                {
                    _player1ClientId = clientId;
                    Debug.Log($"Player 1 assigned: {clientId}");
                }
                else if (_player2ClientId == ulong.MaxValue)
                {
                    _player2ClientId = clientId;
                    Debug.Log($"Player 2 assigned: {clientId}");
                }
            }

            if (IsFull && _currentState.Value == GameState.WaitingForPlayers)
            {
                StartMatch();
            }
        }

        private void OnPlayerDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            if (clientId == _player1ClientId || clientId == _player2ClientId)
            {
                Debug.Log($"Player disconnected during match: {clientId}");
                
                _playerSpawnManager.OnClientDisconnected(clientId);
                
                if (_currentState.Value == GameState.Playing)
                {
                    EndMatch(clientId == _player1ClientId ? _player2ClientId : _player1ClientId);
                }
                else
                {
                    if (clientId == _player1ClientId)
                    {
                        _player1ClientId = ulong.MaxValue;
                    }
                    else if (clientId == _player2ClientId)
                    {
                        _player2ClientId = ulong.MaxValue;
                    }
                }
            }
        }

        private void StartMatch()
        {
            if (!IsServer) return;

            Debug.Log("Starting match!");
            
            _matchTimer.Value = _matchDuration;
            
            _currentState.Value = GameState.Playing;
            
            StartMatchClientRpc();
        }

        private void UpdateMatch()
        {
            _matchTimer.Value -= Time.deltaTime;
            
            if (_matchTimer.Value <= 0)
            {
                EndMatch(ulong.MaxValue);
            }
        }

        private void EndMatch(ulong winnerClientId)
        {
            if (!IsServer) return;

            Debug.Log($"Match ended! Winner: {winnerClientId}");
            
            _currentState.Value = GameState.GameOver;
            OnGameWon?.Invoke(winnerClientId);
            
            EndMatchClientRpc(winnerClientId);
            
            ResetGame();
        }

        private void ResetGame()
        {
            if (!IsServer) return;

            _playerSpawnManager.ClearAllPlayers();
            _player1ClientId = ulong.MaxValue;
            _player2ClientId = ulong.MaxValue;
            _currentState.Value = GameState.WaitingForPlayers;
        }

        #endregion

        #region Public Server Methods

        [ServerRpc(RequireOwnership = false)]
        public void RegisterPlayerDeathServerRpc(ulong killerClientId, ulong victimClientId)
        {
            if (!IsServer) return;
            
            if (killerClientId == _player1ClientId || killerClientId == _player2ClientId)
            {
                EndMatch(killerClientId);
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void StartMatchClientRpc()
        {
            Debug.Log("Match started on client!");
        }

        [ClientRpc]
        private void EndMatchClientRpc(ulong winnerClientId)
        {
            Debug.Log($"Match ended on client! Winner: {winnerClientId}");
        }

        #endregion

        #region Public Getters

        public bool IsPlayer(ulong clientId)
        {
            return clientId == _player1ClientId || clientId == _player2ClientId;
        }

        public ulong GetOpponent(ulong clientId)
        {
            if (clientId == _player1ClientId) return _player2ClientId;
            if (clientId == _player2ClientId) return _player1ClientId;
            return ulong.MaxValue;
        }

        #endregion
    }
}