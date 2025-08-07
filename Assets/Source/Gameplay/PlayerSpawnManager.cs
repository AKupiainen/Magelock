using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using MageLock.Controls;
using MageLock.DependencyInjection;
using MageLock.Networking;
using MageLock.Player;

namespace MageLock.GameModes
{
    public class PlayerSpawnManager
    {
        [Inject] private NetworkManagerCustom networkManagerCustom;
        
        private SpawnPoint[] cachedSpawnPoints;
        private readonly Dictionary<ulong, SpawnedPlayerData> spawnedPlayers = new();
        private readonly List<ulong> playersToRespawnCache = new();

        private struct SpawnedPlayerData
        {
            public NetworkObject PlayerNetworkObject;
            public bool IsAlive;
            public float RespawnTime;
        }

        public PlayerSpawnManager()
        {
            DIContainer.Instance.Inject(this);
        }

        public void Update()
        {
            HandlePendingRespawns();
        }

        public void Cleanup()
        {
            ClearAllPlayers();
        }

        #region Public Methods

        public bool SpawnAllPlayers()
        {
            ClearAllPlayers();

            var allSpawned = true;

            foreach (var clientId in networkManagerCustom.ConnectedClientsIds)
            {
                if (!SpawnPlayerForClient(clientId))
                {
                    allSpawned = false;
                    Debug.LogError($"Failed to spawn player for client {clientId}");
                }
            }

            Debug.Log($"Spawned {spawnedPlayers.Count} players");
            return allSpawned;
        }

        public bool EliminatePlayer(ulong clientId)
        {
            if (!spawnedPlayers.TryGetValue(clientId, out var playerData))
            {
                Debug.LogWarning($"Cannot eliminate player {clientId} - not found");
                return false;
            }

            if (!playerData.IsAlive)
            {
                Debug.LogWarning($"Player {clientId} is already eliminated");
                return false;
            }

            playerData.IsAlive = false;
            spawnedPlayers[clientId] = playerData;

            if (playerData.PlayerNetworkObject != null && playerData.PlayerNetworkObject.IsSpawned)
            {
                playerData.PlayerNetworkObject.Despawn();
            }
            
            int alivePlayerCount = spawnedPlayers.Values.Count(p => p.IsAlive);
            Debug.Log($"Player {clientId} eliminated. Alive players: {alivePlayerCount}");
            
            return true;
        }

        public bool SchedulePlayerRespawn(ulong clientId, float respawnDelay)
        {
            if (!spawnedPlayers.TryGetValue(clientId, out var playerData))
            {
                Debug.LogWarning($"Cannot respawn player {clientId} - not found");
                return false;
            }

            if (playerData.IsAlive)
            {
                Debug.LogWarning($"Player {clientId} is already alive");
                return false;
            }

            playerData.RespawnTime = Time.time + respawnDelay;
            spawnedPlayers[clientId] = playerData;

            Debug.Log($"Scheduled respawn for player {clientId} in {respawnDelay} seconds");
            return true;
        }

        public void ClearAllPlayers()
        {
            foreach (var playerData in spawnedPlayers.Values)
            {
                if (playerData.PlayerNetworkObject != null && playerData.PlayerNetworkObject.IsSpawned)
                {
                    playerData.PlayerNetworkObject.Despawn();
                }
            }

            spawnedPlayers.Clear();
            Debug.Log("Cleared all spawned players");
        }

        public void OnClientDisconnected(ulong clientId)
        {
            if (spawnedPlayers.TryGetValue(clientId, out var playerData))
            {
                Debug.Log($"Cleaning up player data for disconnected client {clientId}");

                if (playerData.PlayerNetworkObject != null && playerData.PlayerNetworkObject.IsSpawned)
                    playerData.PlayerNetworkObject.Despawn();

                spawnedPlayers.Remove(clientId);
            }
        }
        
        public int GetSpawnPointCount()
        {
            return cachedSpawnPoints?.Length ?? 0;
        }

        #endregion

        #region Private Methods

        public void CacheSpawnPoints()
        {
            cachedSpawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Debug.Log($"Cached {cachedSpawnPoints?.Length ?? 0} spawn points");
        }

        private bool SpawnPlayerForClient(ulong clientId)
        {
            var playerPrefab = GetPlayerPrefabForClient(clientId);

            if (playerPrefab == null)
            {
                Debug.LogError($"No valid player prefab for client {clientId}");
                return false;
            }

            var spawnPoint = FindNextAvailableSpawnPoint();
            
            if (spawnPoint == null)
            {
                Debug.LogError($"No available spawn point for client {clientId}");
                return false;
            }

            var spawnedPlayer = Object.Instantiate(
                playerPrefab,
                spawnPoint.Position,
                spawnPoint.Rotation
            );

            var playerNetworkObject = spawnedPlayer.GetComponent<NetworkObject>();
            
            if (playerNetworkObject == null)
            {
                Debug.LogError($"Failed to get NetworkObject from spawned player for client {clientId}");
                Object.Destroy(spawnedPlayer);
                return false;
            }

            playerNetworkObject.SpawnAsPlayerObject(clientId);
            
            var playerData = new SpawnedPlayerData
            {
                PlayerNetworkObject = playerNetworkObject,
                IsAlive = true,
                RespawnTime = 0f
            };

            spawnedPlayers[clientId] = playerData;

            Debug.Log($"Spawned player for client {clientId} at {spawnPoint.Position}");
            return true;
        }

        private GameObject GetPlayerPrefabForClient(ulong clientId)
        {
            var selectedCharacter = PlayerModel.GetSelectedCharacter();
            
            if (selectedCharacter == null)
            {
                Debug.LogError($"No selected character found for client {clientId}");
                return null;
            }

            return selectedCharacter.inGamePrefab;
        }

        private SpawnPoint FindNextAvailableSpawnPoint()
        {
            if (cachedSpawnPoints == null || cachedSpawnPoints.Length == 0)
            {
                Debug.LogError("No spawn points available!");
                return null;
            }

            return cachedSpawnPoints[Random.Range(0, cachedSpawnPoints.Length)];
        }

        private void HandlePendingRespawns()
        {
            playersToRespawnCache.Clear();
    
            float currentTime = Time.time;
    
            foreach (var (key, player) in spawnedPlayers)
            {
                if (!player.IsAlive && 
                    player.RespawnTime > 0f && 
                    currentTime >= player.RespawnTime)
                {
                    playersToRespawnCache.Add(key);
                }
            }
    
            for (int i = 0; i < playersToRespawnCache.Count; i++)
            {
                var clientId = playersToRespawnCache[i];
        
                if (SpawnPlayerForClient(clientId))
                    Debug.Log($"Respawned player {clientId}");
                else
                    Debug.LogError($"Failed to respawn player {clientId}");
            }
        }
        
        #endregion
    }
}