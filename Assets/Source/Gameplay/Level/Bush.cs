using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MageLock.Gameplay
{
    public class Bush : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float revealDistance = 2f;
        
        private static readonly HashSet<NetworkObject> PlayersInAnyBush = new HashSet<NetworkObject>();
        private static readonly Dictionary<NetworkObject, HashSet<Bush>> playerBushes = new Dictionary<NetworkObject, HashSet<Bush>>();
        private static readonly Dictionary<NetworkObject, Renderer[]> playerRenderers = new Dictionary<NetworkObject, Renderer[]>();
        
        private readonly HashSet<NetworkObject> _playersInThisBush = new HashSet<NetworkObject>();
        
        private void Awake()
        {
            var bushCollider = GetComponent<Collider>();
            if (bushCollider) bushCollider.isTrigger = true;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var networkObject = other.GetComponent<NetworkObject>();
            if (!networkObject || !networkObject.IsSpawned) return;
            
            _playersInThisBush.Add(networkObject);
            PlayersInAnyBush.Add(networkObject);
            
            if (!playerBushes.ContainsKey(networkObject))
                playerBushes[networkObject] = new HashSet<Bush>();
            playerBushes[networkObject].Add(this);
            
            if (!playerRenderers.ContainsKey(networkObject))
                playerRenderers[networkObject] = other.GetComponentsInChildren<Renderer>();
        }
        
        private void OnTriggerExit(Collider other)
        {
            var networkObject = other.GetComponent<NetworkObject>();
            if (!networkObject) return;
            
            _playersInThisBush.Remove(networkObject);
            
            if (playerBushes.ContainsKey(networkObject))
            {
                playerBushes[networkObject].Remove(this);
                if (playerBushes[networkObject].Count == 0)
                {
                    playerBushes.Remove(networkObject);
                    PlayersInAnyBush.Remove(networkObject);
                    
                    if (playerRenderers.ContainsKey(networkObject))
                    {
                        SetRenderersEnabled(playerRenderers[networkObject], true);
                        playerRenderers.Remove(networkObject);
                    }
                }
            }
        }
        
        private void Update()
        {
            UpdateVisibility();
        }
        
        private void UpdateVisibility()
        {
            var localPlayer = GetLocalPlayer();
            if (!localPlayer) return;
            
            var toRemove = new List<NetworkObject>();
            
            foreach (var kvp in playerRenderers)
            {
                var player = kvp.Key;
                var renderers = kvp.Value;
                
                if (!player)
                {
                    toRemove.Add(player);
                    continue;
                }
                
                bool shouldBeVisible = ShouldBeVisible(player, localPlayer);
                SetRenderersEnabled(renderers, shouldBeVisible);
            }
            
            foreach (var player in toRemove)
            {
                playerRenderers.Remove(player);
                playerBushes.Remove(player);
                PlayersInAnyBush.Remove(player);
            }
        }
        
        private bool ShouldBeVisible(NetworkObject player, NetworkObject localPlayer)
        {
            if (player == localPlayer)
                return true;
            
            bool playerInBush = PlayersInAnyBush.Contains(player);
            
            if (!playerInBush)
                return true;
            
            bool localInBush = PlayersInAnyBush.Contains(localPlayer);
            
            if (localInBush && SharesBush(player, localPlayer))
                return true;
            
            if (Vector3.Distance(player.transform.position, localPlayer.transform.position) < revealDistance)
                return true;
            
            return false;
        }
        
        private static bool SharesBush(NetworkObject p1, NetworkObject p2)
        {
            if (!playerBushes.ContainsKey(p1) || !playerBushes.TryGetValue(p2, out var playerBush))
                return false;
            
            foreach (var bush in playerBushes[p1])
                if (playerBush.Contains(bush))
                    return true;
            
            return false;
        }
        
        private static void SetRenderersEnabled(Renderer[] renderers, bool enabled)
        {
            foreach (var renderer in renderers)
                if (renderer) renderer.enabled = enabled;
        }
        
        private static NetworkObject GetLocalPlayer()
        {
            var players = FindObjectsOfType<NetworkObject>();
            foreach (var player in players)
                if (player && player.IsLocalPlayer && player.IsPlayerObject)
                    return player;
            return null;
        }
        
        private void OnDestroy()
        {
            _playersInThisBush.Clear();
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            var bushCollider = GetComponent<Collider>();
            if (!bushCollider) return;
            
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            
            if (bushCollider is BoxCollider box)
                Gizmos.DrawCube(box.center, box.size);
            else if (bushCollider is SphereCollider sphere)
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            
            Gizmos.matrix = oldMatrix;
            
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(transform.position, revealDistance);
        }
    }
}