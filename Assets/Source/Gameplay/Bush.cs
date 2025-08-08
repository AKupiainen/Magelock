using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MageLock.Gameplay
{
    public class Bush : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float revealDistance = 2f;
        
        private static readonly HashSet<NetworkObject> playersInAnyBush = new HashSet<NetworkObject>();
        private static readonly Dictionary<NetworkObject, HashSet<Bush>> playerBushes = new Dictionary<NetworkObject, HashSet<Bush>>();
        private static readonly Dictionary<NetworkObject, Renderer[]> playerRenderers = new Dictionary<NetworkObject, Renderer[]>();
        
        private readonly HashSet<NetworkObject> playersInThisBush = new HashSet<NetworkObject>();
        
        private void Awake()
        {
            var collider = GetComponent<Collider>();
            if (collider) collider.isTrigger = true;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var networkObject = other.GetComponent<NetworkObject>();
            if (!networkObject || !networkObject.IsSpawned) return;
            
            playersInThisBush.Add(networkObject);
            playersInAnyBush.Add(networkObject);
            
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
            
            playersInThisBush.Remove(networkObject);
            
            if (playerBushes.ContainsKey(networkObject))
            {
                playerBushes[networkObject].Remove(this);
                if (playerBushes[networkObject].Count == 0)
                {
                    playerBushes.Remove(networkObject);
                    playersInAnyBush.Remove(networkObject);
                    
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
                playersInAnyBush.Remove(player);
            }
        }
        
        private bool ShouldBeVisible(NetworkObject player, NetworkObject localPlayer)
        {
            if (player == localPlayer)
                return true;
            
            bool playerInBush = playersInAnyBush.Contains(player);
            
            if (!playerInBush)
                return true;
            
            bool localInBush = playersInAnyBush.Contains(localPlayer);
            
            if (localInBush && SharesBush(player, localPlayer))
                return true;
            
            if (Vector3.Distance(player.transform.position, localPlayer.transform.position) < revealDistance)
                return true;
            
            return false;
        }
        
        private static bool SharesBush(NetworkObject p1, NetworkObject p2)
        {
            if (!playerBushes.ContainsKey(p1) || !playerBushes.ContainsKey(p2))
                return false;
            
            foreach (var bush in playerBushes[p1])
                if (playerBushes[p2].Contains(bush))
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
            var players = Object.FindObjectsOfType<NetworkObject>();
            foreach (var player in players)
                if (player && player.IsLocalPlayer && player.IsPlayerObject)
                    return player;
            return null;
        }
        
        private void OnDestroy()
        {
            playersInThisBush.Clear();
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            var collider = GetComponent<Collider>();
            if (!collider) return;
            
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            
            if (collider is BoxCollider box)
                Gizmos.DrawCube(box.center, box.size);
            else if (collider is SphereCollider sphere)
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            
            Gizmos.matrix = oldMatrix;
            
            // Draw reveal radius
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.DrawWireSphere(transform.position, revealDistance);
        }
    }
}