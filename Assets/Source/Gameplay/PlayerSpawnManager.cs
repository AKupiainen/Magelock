using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using MageLock.DependencyInjection;
using MageLock.Networking;
using MageLock.Player;

namespace MageLock.GameModes
{
    public class PlayerSpawnManager
    {
        [Inject] private NetworkManagerCustom networkManagerCustom;
        
        private readonly Dictionary<ulong, NetworkObject> spawnedPlayers = new();

        public PlayerSpawnManager()
        {
            DIContainer.Instance.Inject(this);
        }

        public void Cleanup()
        {
            ClearAllPlayers();
        }

        public bool SpawnAllPlayers()
        {
            ClearAllPlayers();

            var allSpawned = true;
            int spawnIndex = 0;

            foreach (var clientId in networkManagerCustom.ConnectedClientsIds)
            {
                if (!SpawnPlayerForClient(clientId, spawnIndex))
                {
                    allSpawned = false;
                    Debug.LogError($"Failed to spawn player for client {clientId}");
                }
                spawnIndex++;
            }

            Debug.Log($"Spawned {spawnedPlayers.Count} players");
            return allSpawned;
        }

        public void ClearAllPlayers()
        {
            foreach (var playerNetworkObject in spawnedPlayers.Values)
            {
                if (playerNetworkObject != null && playerNetworkObject.IsSpawned)
                {
                    playerNetworkObject.Despawn();
                }
            }

            spawnedPlayers.Clear();
            Debug.Log("Cleared all spawned players");
        }

        public void OnClientDisconnected(ulong clientId)
        {
            if (spawnedPlayers.TryGetValue(clientId, out var playerNetworkObject))
            {
                Debug.Log($"Cleaning up player data for disconnected client {clientId}");

                if (playerNetworkObject != null && playerNetworkObject.IsSpawned)
                    playerNetworkObject.Despawn();

                spawnedPlayers.Remove(clientId);
            }
        }

        private bool SpawnPlayerForClient(ulong clientId, int spawnIndex)
        {
            var playerPrefab = GetPlayerPrefabForClient(clientId);

            if (playerPrefab == null)
            {
                Debug.LogError($"No valid player prefab for client {clientId}");
                return false;
            }

            var spawnPoint = GetSpawnPointByIndex(spawnIndex);
            
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
            spawnedPlayers[clientId] = playerNetworkObject;

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

        private SpawnPoint GetSpawnPointByIndex(int index)
        {
            var spawnPoints = Object.FindObjectsByType<SpawnPoint>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("No spawn points available!");
                return null;
            }

            if (index >= spawnPoints.Length)
            {
                index = spawnPoints.Length - 1;
            }

            return spawnPoints[index];
        }
    }
}