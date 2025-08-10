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
        
        [Header("Effect Cleanup")]
        [SerializeField] private float explosionEffectDuration = 2f;
        
        protected override void InitializeProjectile(GameObject projectileObj, GameObject caster)
        {
            var fireballProjectile = projectileObj.GetComponent<FireballProjectile>();
            
            if (fireballProjectile != null)
            {
                float impactDamage = damageOnImpact ? damage : 0f;
                fireballProjectile.Initialize(caster, impactDamage, projectileSpeed, OnFireballExplode);
                fireballProjectile.SetExplosionData(explosionRadius, explosionDamage, explosionPrefab);
            }
        }
        
        private void OnFireballExplode(Vector3 position, GameObject caster, float radius, float damage, GameObject effectPrefab)
        {
            if (!NetworkManager.Singleton.IsServer)
                return;
            
            SpawnNetworkEffect(effectPrefab, position, Quaternion.identity, explosionEffectDuration);
            ApplyAreaDamage(position, caster, radius, damage);
        }
    }
}