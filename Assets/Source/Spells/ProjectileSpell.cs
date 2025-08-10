using UnityEngine;
using Unity.Netcode;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "ProjectileSpell", menuName = "MageLock/Spells/Projectile")]
    public class ProjectileSpell : Spell
    {
        [Header("Projectile Settings")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected float projectileSpeed = 20f;
        [SerializeField] protected float damage = 50f;
        [SerializeField] protected float lifetime = 5f;
        
        protected override void CastInDirection(GameObject caster, Vector3 origin, Vector3 direction)
        {
            if (!projectilePrefab)
            {
                Debug.LogError($"[{SpellName}] No projectile prefab assigned!");
                return;
            }
            
            Vector3 spawnPos = origin + direction * 0.5f;
            
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            NetworkObject netObj = projectileObj.GetComponent<NetworkObject>();
            
            if (netObj != null && NetworkManager.Singleton.IsServer)
            {
                netObj.Spawn();
            }
            
            var projectile = projectileObj.GetComponent<Projectile>();
            projectile.Initialize(caster, damage, projectileSpeed, lifetime);
        }
    }
}