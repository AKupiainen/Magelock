using System;
using System.Collections.Generic;
using System.Linq;
using BrawlLine.DependencyInjection;
using BrawlLine.Events;
using BrawlLine.Networking;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace BrawlLine.GameModes
{
    public class GameStateManager : NetworkBehaviour
    {
        [Inject] private MiniGameDatabase miniGameDatabase;
        [Inject] private NetworkManagerCustom networkManagerCustom;

        public NetworkVariable<int> playersAlive = new();
        public NetworkVariable<int> playersFinished = new();
        public NetworkVariable<bool> isGameActive = new();
        public NetworkVariable<int> currentPhase = new(1);
        public NetworkVariable<FixedString128Bytes> currentMiniGameId = new("");

        private GameObject currentLevelInstance;
        private int totalPlayers;
        private readonly HashSet<ulong> finishedPlayers = new();
        private readonly HashSet<ulong> eliminatedPlayers = new();
        private MiniGameConfig currentMiniGame;
        private PhaseConfig currentPhaseConfig;
        private int qualifyingPlayersForCurrentPhase;
        private IGameModeLogic currentGameModeLogic;
        
        private PlayerSpawnManager playerSpawnManager;

        #region Unity Lifecycle

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            DIContainer.Instance.Inject(this);

            if (IsServer)
            {
                playerSpawnManager = new PlayerSpawnManager();
                
                InitializeGame();
                RegisterServerEvents();
            }

            RegisterNetworkCallbacks();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            UnregisterNetworkCallbacks();

            if (IsServer) 
            {
                UnregisterServerEvents();
                playerSpawnManager?.Cleanup();
            }
        }

        private void Update()
        {
            if (!IsServer) return;
            
            currentGameModeLogic?.Update();
            playerSpawnManager?.Update();
        }

        #endregion

        #region Initialization

        private void RegisterNetworkCallbacks()
        {
            isGameActive.OnValueChanged += OnGameActiveChanged;
            playersFinished.OnValueChanged += OnPlayersFinishedChanged;
            playersAlive.OnValueChanged += OnPlayersAliveChanged;
            currentMiniGameId.OnValueChanged += OnCurrentMiniGameChanged;
            currentPhase.OnValueChanged += OnCurrentPhaseChanged;
        }

        private void UnregisterNetworkCallbacks()
        {
            isGameActive.OnValueChanged -= OnGameActiveChanged;
            playersFinished.OnValueChanged -= OnPlayersFinishedChanged;
            playersAlive.OnValueChanged -= OnPlayersAliveChanged;
            currentMiniGameId.OnValueChanged -= OnCurrentMiniGameChanged;
            currentPhase.OnValueChanged -= OnCurrentPhaseChanged;
        }

        private void RegisterServerEvents()
        {
            if (networkManagerCustom != null) 
                networkManagerCustom.OnClientDisconnectCallback += OnClientDisconnected;
        }

        private void UnregisterServerEvents()
        {
            if (networkManagerCustom != null) 
                networkManagerCustom.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        #endregion

        #region Server Logic

        private void InitializeGame()
        {
            if (miniGameDatabase == null)
            {
                Debug.LogError("MiniGameDatabase is not assigned!");
                EndGameWithError("MiniGameDatabase missing");
                return;
            }

            var playerCount = networkManagerCustom.ConnectedClients.Count;
            
            totalPlayers = playerCount;
            playersAlive.Value = playerCount;
            playersFinished.Value = 0;
            currentPhase.Value = 1;

            Debug.Log($"Initializing game for {playerCount} players");

            finishedPlayers.Clear();
            eliminatedPlayers.Clear();

            SelectAndStartMiniGame();
        }

        private void SelectAndStartMiniGame()
        {
            currentMiniGame = miniGameDatabase.SelectRandomMiniGame();

            if (currentMiniGame == null)
            {
                Debug.LogError("No mini-games available in database!");
                EndGameWithError("No mini-games available");
                return;
            }

            currentMiniGameId.Value = currentMiniGame.miniGameId;
            Debug.Log($"Selected mini-game: {currentMiniGame.miniGameId}");

            currentGameModeLogic = currentMiniGame.GameModeLogic;
            currentMiniGame.InitializeGameModeLogic(this, playerSpawnManager);

            StartCurrentPhase();
        }

        private void StartCurrentPhase()
        {
            if (!IsServer)
            {
                Debug.LogError("Cannot start phase - invalid state");
                return;
            }

            currentPhaseConfig = currentMiniGame.GetPhase(currentPhase.Value);
            
            if (currentPhaseConfig == null)
            {
                Debug.LogError($"No phase config found for phase {currentPhase.Value}");
                EndGameWithError($"Phase {currentPhase.Value} configuration missing");
                return;
            }

            qualifyingPlayersForCurrentPhase = currentMiniGame.GetQualifyingPlayersForPhase(
                currentPhase.Value,
                playersAlive.Value,
                networkManagerCustom.MaxPlayers
            );

            Debug.Log(
                $"Starting phase {currentPhase.Value} - Need {qualifyingPlayersForCurrentPhase} to qualify from {playersAlive.Value} players");

            isGameActive.Value = true;

            finishedPlayers.Clear();
            playersFinished.Value = 0;

            if (!SpawnCurrentLevel())
            {
                Debug.LogError("Failed to spawn level");
                EndGameWithError("Level spawning failed");
                return;
            }

            if (!SpawnAllPlayers())
            {
                Debug.LogError("Failed to spawn players");
                EndGameWithError("Player spawning failed");
                return;
            }

            currentGameModeLogic?.OnPhaseStart(currentPhase.Value, playersAlive.Value, qualifyingPlayersForCurrentPhase);

            Debug.Log($"Phase {currentPhase.Value} started successfully");

            EventsBus.Trigger(new RaceStartedEvent
            {
                PlayersTotal = playersAlive.Value,
                QualifyingPlayers = qualifyingPlayersForCurrentPhase
            });
        }

        private bool SpawnCurrentLevel()
        {
            if (currentLevelInstance != null)
            {
                if (currentLevelInstance.TryGetComponent<NetworkObject>(out var oldNetworkObj))
                    oldNetworkObj.Despawn(); 
                else
                    Destroy(currentLevelInstance); 
            }

            try
            {
                currentLevelInstance = Instantiate(currentPhaseConfig.levelPrefab);

                if (currentLevelInstance.TryGetComponent<NetworkObject>(out var newNetworkObj))
                    newNetworkObj.Spawn();
                else
                    Debug.LogWarning("Spawned level prefab does not have a NetworkObject component.");

                Debug.Log($"Spawned level: {currentPhaseConfig.levelPrefab.name}");
                
                playerSpawnManager?.CacheSpawnPoints();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to spawn level: {ex.Message}");
                return false;
            }
        }

        private bool SpawnAllPlayers()
        {
            var result = playerSpawnManager.SpawnAllPlayers();
            
            if (result)
            {
                Debug.Log($"Successfully spawned all players. Spawn points available: {playerSpawnManager.GetSpawnPointCount()}");
            }
            
            return result;
        }

        #endregion

        #region Game Management

        private void CheckPhaseCompletion()
        {
            if (!IsServer) return;

            var shouldEnd = currentGameModeLogic?.ShouldEndPhase(
                finishedPlayers.Count, 
                qualifyingPlayersForCurrentPhase, 
                playersAlive.Value
            ) ?? finishedPlayers.Count >= qualifyingPlayersForCurrentPhase;

            if (shouldEnd)
            {
                Debug.Log(
                    $"Phase completion criteria met. Finished: {finishedPlayers.Count}, Required: {qualifyingPlayersForCurrentPhase}");
                EndCurrentPhase();
            }
        }

        private void EndCurrentPhase()
        {
            if (!IsServer) return;

            isGameActive.Value = false;

            var connectedClients = NetworkManager.ConnectedClientsIds.ToList();
            
            var playersToEliminate = currentGameModeLogic?.GetPlayersToEliminate(
                finishedPlayers.ToList(), 
                qualifyingPlayersForCurrentPhase, 
                connectedClients
            ) ?? GetDefaultPlayersToEliminate(connectedClients);

            var qualifiedPlayers = connectedClients.Where(p => !playersToEliminate.Contains(p)).ToList();

            EliminateNonQualifiedPlayers(playersToEliminate);

            Debug.Log($"Phase {currentPhase.Value} ended! {qualifiedPlayers.Count} players qualified");

            if (currentLevelInstance != null)
            {
                if (currentLevelInstance.TryGetComponent<NetworkObject>(out var networkObj))
                    networkObj.Despawn();
                else
                    Destroy(currentLevelInstance);
                currentLevelInstance = null;
            }

            playerSpawnManager?.ClearAllPlayers();

            currentGameModeLogic?.OnPhaseEnd(currentPhase.Value, qualifiedPlayers, playersToEliminate);

            EventsBus.Trigger(new RaceEndedEvent
            {
                QualifiedPlayers = qualifiedPlayers,
                TotalFinished = finishedPlayers.Count,
                TotalEliminated = eliminatedPlayers.Count
            });

            var shouldEndGame = currentGameModeLogic?.ShouldEndGame(
                playersAlive.Value, 
                currentPhase.Value, 
                currentMiniGame.GetTotalPhases()
            ) ?? playersAlive.Value <= 1;

            if (shouldEndGame)
            {
                EndGame();
            }
            else if (currentPhase.Value < currentMiniGame.GetTotalPhases())
            {
                currentPhase.Value++;
                StartCurrentPhase();
            }
        }

        private List<ulong> GetDefaultPlayersToEliminate(List<ulong> connectedClients)
        {
            var qualifiedPlayers = finishedPlayers.Take(qualifyingPlayersForCurrentPhase).ToList();
            return connectedClients.Where(p => !qualifiedPlayers.Contains(p) && !eliminatedPlayers.Contains(p)).ToList();
        }

        private void EliminateNonQualifiedPlayers(List<ulong> playersToEliminate)
        {
            foreach (var clientId in playersToEliminate)
                if (!eliminatedPlayers.Contains(clientId))
                    EliminatePlayer(clientId, "Did not qualify");

            Debug.Log($"Eliminated {playersToEliminate.Count} players who didn't qualify");
        }

        private void EndGame()
        {
            if (!IsServer) return;

            var winner = NetworkManager.ConnectedClientsIds.FirstOrDefault(id => !eliminatedPlayers.Contains(id));

            Debug.Log($"Game ended! Winner: {winner}");

            currentGameModeLogic?.OnGameEnd(winner);

            EventsBus.Trigger(new GameEndedEvent
            {
                WinnerClientId = winner,
                TotalEliminated = eliminatedPlayers.Count
            });
        }

        private void EndGameWithError(string errorMessage)
        {
            Debug.LogError($"Game ended with error: {errorMessage}");

            isGameActive.Value = false;
            playerSpawnManager?.ClearAllPlayers();

            EventsBus.Trigger(new RaceErrorEvent
            {
                ErrorMessage = errorMessage
            });
        }

        #endregion

        #region Server RPCs - Player Management

        [ServerRpc(RequireOwnership = false)]
        public void PlayerFinishedPhaseServerRpc(ulong clientId)
        {
            if (!IsServer) return;
            PlayerFinishedPhase(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayerEliminatedServerRpc(ulong clientId, string reason = "")
        {
            if (!IsServer) return;
            EliminatePlayer(clientId, reason);
        }

        [ServerRpc(RequireOwnership = false)]
        public void PlayerRespawnRequestServerRpc(ulong clientId)
        {
            if (!IsServer) return;

            var defaultDelay = currentPhaseConfig?.respawnDelay ?? 2f;
            var customDelay = currentGameModeLogic?.GetRespawnDelay(clientId, defaultDelay) ?? defaultDelay;
            
            playerSpawnManager?.SchedulePlayerRespawn(clientId, customDelay);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ForceEliminatePlayerServerRpc(ulong clientId)
        {
            if (!IsServer) return;
            playerSpawnManager?.EliminatePlayer(clientId);
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestGameStateServerRpc(ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            SendGameStateToClientClientRpc(
                playersAlive.Value,
                playersFinished.Value,
                isGameActive.Value,
                currentPhase.Value,
                currentMiniGameId.Value
            );
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void SendGameStateToClientClientRpc(
            int aliveCount, 
            int finishedCount, 
            bool gameActive, 
            int phase, 
            FixedString128Bytes miniGameId)
        {
            Debug.Log($"[Client] Received game state - Alive: {aliveCount}, Finished: {finishedCount}, Active: {gameActive}, Phase: {phase}, Game: {miniGameId}");
        }

        [ClientRpc]
        public void NotifyPhaseStartClientRpc(int phaseNumber, int totalPlayers, int qualifyingPlayers)
        {
            Debug.Log($"[Client] Phase {phaseNumber} started with {totalPlayers} players, {qualifyingPlayers} qualifying");
            
            if (!IsServer && currentMiniGame != null)
            {
                currentGameModeLogic?.OnPhaseStart(phaseNumber, totalPlayers, qualifyingPlayers);
            }
        }

        [ClientRpc]
        public void NotifyPhaseEndClientRpc(int phaseNumber, int qualifiedCount, int eliminatedCount)
        {
            Debug.Log($"[Client] Phase {phaseNumber} ended - Qualified: {qualifiedCount}, Eliminated: {eliminatedCount}");
            
            if (!IsServer && currentMiniGame != null)
            {
                currentGameModeLogic?.OnPhaseEnd(phaseNumber, new List<ulong>(), new List<ulong>());
            }
        }

        [ClientRpc]
        public void NotifyGameEndClientRpc(ulong winnerClientId)
        {
            Debug.Log($"[Client] Game ended! Winner: {winnerClientId}");
            
            if (!IsServer && currentMiniGame != null)
            {
                currentGameModeLogic?.OnGameEnd(winnerClientId);
            }
        }

        #endregion

        #region Player Management (Server-only)

        private void PlayerFinishedPhase(ulong clientId)
        {
            if (finishedPlayers.Contains(clientId))
            {
                Debug.LogWarning($"Player {clientId} already finished the phase");
                return;
            }

            if (eliminatedPlayers.Contains(clientId))
            {
                Debug.LogWarning($"Player {clientId} is eliminated and cannot finish");
                return;
            }

            finishedPlayers.Add(clientId);
            playersFinished.Value = finishedPlayers.Count;

            var position = finishedPlayers.Count;
            var isQualified = position <= qualifyingPlayersForCurrentPhase;

            Debug.Log($"Player {clientId} finished in position {position} (Qualified: {isQualified})");

            currentGameModeLogic?.OnPlayerFinished(clientId, position);

            EventsBus.Trigger(new PlayerFinishedRaceEvent
            {
                ClientId = clientId,
                Position = position,
                IsQualified = isQualified
            });

            CheckPhaseCompletion();
        }

        private void EliminatePlayer(ulong clientId, string reason)
        {
            if (eliminatedPlayers.Contains(clientId))
            {
                Debug.LogWarning($"Player {clientId} is already eliminated");
                return;
            }

            if (finishedPlayers.Contains(clientId))
            {
                Debug.LogWarning($"Player {clientId} already finished and cannot be eliminated");
                return;
            }

            eliminatedPlayers.Add(clientId);

            playerSpawnManager?.EliminatePlayer(clientId);
            playersAlive.Value = totalPlayers - eliminatedPlayers.Count;

            Debug.Log($"Player {clientId} eliminated. Reason: {reason}. Players alive: {playersAlive.Value}");

            currentGameModeLogic?.OnPlayerEliminated(clientId, reason);

            EventsBus.Trigger(new PlayerEliminatedEvent
            {
                ClientId = clientId,
                Reason = reason,
                PlayersRemaining = playersAlive.Value
            });

            CheckPhaseCompletion();
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (networkManagerCustom.ShutdownInProgress)
            {
                return;
            }
            
            if (!IsServer) return;

            Debug.Log($"Player {clientId} disconnected");

            finishedPlayers.Remove(clientId);
            eliminatedPlayers.Remove(clientId);

            totalPlayers = Mathf.Max(0, totalPlayers - 1);
            playersAlive.Value = totalPlayers - eliminatedPlayers.Count;
            playersFinished.Value = finishedPlayers.Count;

            playerSpawnManager?.OnClientDisconnected(clientId);
            currentGameModeLogic?.OnPlayerDisconnected(clientId);

            CheckPhaseCompletion();
        }

        #endregion

        #region Network Variable Callbacks

        private void OnGameActiveChanged(bool previousValue, bool newValue)
        {
            Debug.Log($"Game active changed: {newValue}");
        }

        private void OnCurrentMiniGameChanged(FixedString128Bytes previousValue, FixedString128Bytes newValue)
        {
            Debug.Log($"Current mini-game changed to: {newValue}");

            if (!IsServer)
            {
                currentMiniGame = miniGameDatabase?.GetMiniGame(newValue.ToString());
                if (currentMiniGame != null)
                {
                    currentGameModeLogic = currentMiniGame.GameModeLogic;
                    currentMiniGame.InitializeGameModeLogic(this, null);
                }
            }
        }

        private void OnCurrentPhaseChanged(int previousValue, int newValue)
        {
            Debug.Log($"Current phase changed from {previousValue} to {newValue}");
        }

        private void OnPlayersFinishedChanged(int previousValue, int newValue)
        {
            Debug.Log($"Players finished changed from {previousValue} to {newValue}");

            EventsBus.Trigger(new PlayersFinishedChangedEvent
            {
                PreviousCount = previousValue,
                CurrentCount = newValue,
                QualifyingPlayers = qualifyingPlayersForCurrentPhase
            });
        }

        private void OnPlayersAliveChanged(int previousValue, int newValue)
        {
            Debug.Log($"Players alive changed from {previousValue} to {newValue}");

            EventsBus.Trigger(new PlayersAliveChangedEvent
            {
                PreviousCount = previousValue,
                CurrentCount = newValue
            });
        }

        #endregion
    }
}