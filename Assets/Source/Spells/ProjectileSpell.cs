using MageLock.Gameplay;
using UnityEngine;
using Unity.Netcode;
using MageLock.Networking;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "ProjectileSpell", menuName = "MageLock/Spells/Projectile")]
    public class ProjectileSpell : Spell
    {
        [Header("Projectile Settings")]
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected float projectileSpeed = 20f;
        [SerializeField] protected float damage = 50f;
        [SerializeField] protected float spawnOffset = 0.5f;
        
        protected override void CastInDirection(GameObject caster, Vector3 origin, Vector3 direction)
        {
            if (!ValidateCast(caster, origin, direction))
                return;
            
            if (!NetworkManager.Singleton.IsServer)
                return;
            
            GameObject projectileObj = SpawnProjectile(origin, direction);
            
            if (projectileObj != null)
            {
                InitializeProjectile(projectileObj, caster);
            }
        }
        
        protected virtual bool ValidateCast(GameObject caster, Vector3 origin, Vector3 direction)
        {
            if (!projectilePrefab)
            {
                Debug.LogError($"[{SpellName}] No projectile prefab assigned!");
                return false;
            }
            
            return true;
        }
        
        protected virtual GameObject SpawnProjectile(Vector3 origin, Vector3 direction)
        {
            Vector3 spawnPos = CalculateSpawnPosition(origin, direction);
            Quaternion spawnRot = CalculateSpawnRotation(direction);
            
            GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, spawnRot);
            
            NetworkObject netObj = projectileObj.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                netObj.Spawn();
            }
            
            return projectileObj;
        }
        
        protected virtual Vector3 CalculateSpawnPosition(Vector3 origin, Vector3 direction)
        {
            return origin + direction * spawnOffset;
        }
        
        protected virtual Quaternion CalculateSpawnRotation(Vector3 direction)
        {
            return Quaternion.LookRotation(direction);
        }
        
        protected virtual void InitializeProjectile(GameObject projectileObj, GameObject caster)
        {
            var projectile = projectileObj.GetComponent<Projectile>();
            
            if (projectile != null)
            {
                projectile.Initialize(caster, damage, projectileSpeed);
                OnProjectileInitialized(projectile, caster);
            }
        }
        
        protected virtual void OnProjectileInitialized(Projectile projectile, GameObject caster) { }
        
        protected void SpawnNetworkEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation, float duration)
        {
            if (!effectPrefab || !NetworkManager.Singleton.IsServer)
                return;
            
            GameObject effect = Instantiate(effectPrefab, position, rotation);
            NetworkObject netObj = effect.GetComponent<NetworkObject>();
            
            if (netObj != null)
            {
                netObj.Spawn();
                
                var autoDespawner = effect.AddComponent<NetworkDespawner>();
                autoDespawner.Initialize(duration);
            }
            else
            {
                Destroy(effect, duration);
            }
        }
        
        protected void ApplyAreaDamage(Vector3 position, GameObject caster, float radius, float damageAmount)
        {
            var hits = Physics.OverlapSphere(position, radius, targetLayers);
            
            foreach (var hit in hits)
            {
                if (hit.gameObject == caster) continue;
                
                var health = hit.GetComponent<IHealth>();
                if (health != null)
                {
                    float distance = Vector3.Distance(position, hit.transform.position);
                    float falloff = Mathf.Clamp01(1f - distance / radius);
                    float finalDamage = damageAmount * falloff;
                    
                    health.TakeDamage(finalDamage);
                }
            }
        }
    }
}