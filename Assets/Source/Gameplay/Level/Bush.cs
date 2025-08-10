using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace MageLock.Gameplay
{
    public class Bush : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float revealDistance = 2f;
        
        private static readonly HashSet<NetworkObject> PlayersInAnyBush = new();
        private static readonly Dictionary<NetworkObject, HashSet<Bush>> PlayerBushes = new();
        private static readonly Dictionary<NetworkObject, Renderer[]> PlayerRenderers = new();

        private readonly HashSet<NetworkObject> _playersInThisBush = new();
        
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
            
            if (!PlayerBushes.ContainsKey(networkObject))
                PlayerBushes[networkObject] = new HashSet<Bush>();
            PlayerBushes[networkObject].Add(this);
            
            if (!PlayerRenderers.ContainsKey(networkObject))
                PlayerRenderers[networkObject] = other.GetComponentsInChildren<Renderer>();
        }
        
        private void OnTriggerExit(Collider other)
        {
            var networkObject = other.GetComponent<NetworkObject>();
            if (!networkObject) return;
            
            _playersInThisBush.Remove(networkObject);
            
            if (PlayerBushes.ContainsKey(networkObject))
            {
                PlayerBushes[networkObject].Remove(this);
                if (PlayerBushes[networkObject].Count == 0)
                {
                    PlayerBushes.Remove(networkObject);
                    PlayersInAnyBush.Remove(networkObject);
                    
                    if (PlayerRenderers.ContainsKey(networkObject))
                    {
                        SetRenderersEnabled(PlayerRenderers[networkObject], true);
                        PlayerRenderers.Remove(networkObject);
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
            
            foreach (var kvp in PlayerRenderers)
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
                PlayerRenderers.Remove(player);
                PlayerBushes.Remove(player);
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
            if (!PlayerBushes.ContainsKey(p1) || !PlayerBushes.TryGetValue(p2, out var playerBush))
                return false;
            
            foreach (var bush in PlayerBushes[p1])
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