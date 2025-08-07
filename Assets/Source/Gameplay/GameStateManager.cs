using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using MageLock.DependencyInjection;
using MageLock.Networking;

namespace MageLock.GameModes
{
    public enum GameState
    {
        WaitingForPlayers,
        Playing,
        GameOver
    }

    public class GameStateManager : NetworkBehaviour
    {
        [Inject] private NetworkManagerCustom networkManagerCustom;
        private PlayerSpawnManager playerSpawnManager;
        
        private NetworkVariable<GameState> currentState = new(GameState.WaitingForPlayers);
        private NetworkVariable<float> matchTimer = new(0f);
        
        private ulong player1ClientId = ulong.MaxValue;
        private ulong player2ClientId = ulong.MaxValue;
        private float matchDuration = 120f; 
        
        public event Action<GameState> OnGameStateChanged;
        public event Action<ulong> OnGameWon;
        
        public GameState CurrentState => currentState.Value;
        public float MatchTimer => matchTimer.Value;
        public bool IsFull => player1ClientId != ulong.MaxValue && player2ClientId != ulong.MaxValue;

        private void Awake()
        {
            DIContainer.Instance.Inject(this);
            playerSpawnManager = new PlayerSpawnManager();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                networkManagerCustom.OnClientConnectedCallback += OnPlayerConnected;
                networkManagerCustom.OnClientDisconnectCallback += OnPlayerDisconnected;
            }

            currentState.OnValueChanged += (prev, current) => OnGameStateChanged?.Invoke(current);
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
            {
                networkManagerCustom.OnClientConnectedCallback -= OnPlayerConnected;
                networkManagerCustom.OnClientDisconnectCallback -= OnPlayerDisconnected;
            }
            
            if (playerSpawnManager != null)
            {
                playerSpawnManager.Cleanup();
            }
        }

        private void Update()
        {
            if (!IsServer) return;

            if (currentState.Value == GameState.Playing)
            {
                UpdateMatch();
            }
        }

        #region Server Methods

        private void OnPlayerConnected(ulong clientId)
        {
            if (!IsServer) return;

            if (player1ClientId == ulong.MaxValue)
            {
                player1ClientId = clientId;
                Debug.Log($"Player 1 connected: {clientId}");
            }
            else if (player2ClientId == ulong.MaxValue)
            {
                player2ClientId = clientId;
                Debug.Log($"Player 2 connected: {clientId}");
            }
            else
            {
                Debug.LogWarning($"Game full, rejecting player: {clientId}");
                return;
            }

            if (IsFull && currentState.Value == GameState.WaitingForPlayers)
            {
                StartMatch();
            }
        }

        private void OnPlayerDisconnected(ulong clientId)
        {
            if (!IsServer) return;

            if (clientId == player1ClientId || clientId == player2ClientId)
            {
                Debug.Log($"Player disconnected during match: {clientId}");
                
                playerSpawnManager.OnClientDisconnected(clientId);
                
                EndMatch(clientId == player1ClientId ? player2ClientId : player1ClientId);
            }
        }

        private void StartMatch()
        {
            if (!IsServer) return;

            Debug.Log("Starting match!");
            
            matchTimer.Value = matchDuration;
            
            playerSpawnManager.SpawnAllPlayers();
            
            currentState.Value = GameState.Playing;
            
            StartMatchClientRpc();
        }

        private void UpdateMatch()
        {
            matchTimer.Value -= Time.deltaTime;
            
            if (matchTimer.Value <= 0)
            {
                EndMatch(ulong.MaxValue);
            }
        }

        private void EndMatch(ulong winnerClientId)
        {
            if (!IsServer) return;

            Debug.Log($"Match ended! Winner: {winnerClientId}");
            
            currentState.Value = GameState.GameOver;
            OnGameWon?.Invoke(winnerClientId);
            
            EndMatchClientRpc(winnerClientId);
            
            ResetGame();
        }

        private void ResetGame()
        {
            if (!IsServer) return;

            playerSpawnManager.ClearAllPlayers();
            player1ClientId = ulong.MaxValue;
            player2ClientId = ulong.MaxValue;
            currentState.Value = GameState.WaitingForPlayers;
        }

        #endregion

        #region Public Server Methods

        [ServerRpc(RequireOwnership = false)]
        public void RegisterPlayerDeathServerRpc(ulong killerClientId, ulong victimClientId)
        {
            if (!IsServer) return;
            
            if (killerClientId == player1ClientId || killerClientId == player2ClientId)
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
            return clientId == player1ClientId || clientId == player2ClientId;
        }

        public ulong GetOpponent(ulong clientId)
        {
            if (clientId == player1ClientId) return player2ClientId;
            if (clientId == player2ClientId) return player1ClientId;
            return ulong.MaxValue;
        }

        #endregion
    }
}