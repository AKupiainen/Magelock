using UnityEngine;
using Unity.Netcode;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "Fireball", menuName = "MageLock/Spells/Abilities/Fireball")]
    public class Fireball : ProjectileSpell
    {
        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float explosionDamage = 25f;
        [SerializeField] private GameObject explosionPrefab;
        [SerializeField] private bool damageOnImpact = true;
        
        protected override void CastInDirection(GameObject caster, Vector3 origin, Vector3 direction)
        {
            if (!projectilePrefab)
            {
                Debug.LogError($"[{SpellName}] No projectile prefab assigned!");
                return;
            }
            
            if (!NetworkManager.Singleton.IsServer)
            {
                return;
            }
            
            Vector3 spawnPos = origin + direction * 0.5f;
            
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            NetworkObject netObj = projectileObj.GetComponent<NetworkObject>();
            
            netObj.Spawn();
            
            var projectile = projectileObj.GetComponent<FireballProjectile>();
            
            if (projectile != null)
            {
                projectile.Initialize(
                    caster, 
                    damageOnImpact ? damage : 0f,  
                    projectileSpeed, 
                    OnFireballExplode 
                );
                
                projectile.SetExplosionData(explosionRadius, explosionDamage, explosionPrefab);
            }
        }
        
        private void OnFireballExplode(Vector3 position, GameObject caster, float radius, float damage, GameObject effectPrefab)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;
            
            if (effectPrefab)
            {
                GameObject explosion = Instantiate(effectPrefab, position, Quaternion.identity);
                NetworkObject netObj = explosion.GetComponent<NetworkObject>();
                
                if (netObj != null)
                {
                    netObj.Spawn();
                }
            }
            
            var hits = Physics.OverlapSphere(position, radius);
            
            foreach (var hit in hits)
            {
                if (hit.gameObject == caster) continue;
                
                var health = hit.GetComponent<IHealth>();
                if (health != null)
                {
                    float distance = Vector3.Distance(position, hit.transform.position);
                    float falloff = 1f - (distance / radius);
                    float finalDamage = damage * falloff;
                    
                    health.TakeDamage(finalDamage);
                }
            }
        }
    }
}