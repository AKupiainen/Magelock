using UnityEngine;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "Fireball", menuName = "MageLock/Spells/Abilities/Fireball")]
    public class Fireball : ProjectileSpell
    {
        [Header("Explosion")]
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float explosionDamage = 25f;
        [SerializeField] private GameObject explosionPrefab;
        
        public override void Cast(GameObject caster, Vector3 direction)
        {
            if (!projectilePrefab) return;
            
            var spawnPos = caster.transform.position + Vector3.up + direction * 0.5f;
            var projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            
            var proj = projectile.GetComponent<Projectile>();
            if (!proj) proj = projectile.AddComponent<Projectile>();
            
            proj.Initialize(caster, damage, projectileSpeed, lifetime, Explode);
        }
        
        private void Explode(Vector3 position, GameObject caster)
        {
            if (explosionPrefab)
                Instantiate(explosionPrefab, position, Quaternion.identity);
            
            var hits = Physics.OverlapSphere(position, explosionRadius);
            
            foreach (var hit in hits)
            {
                if (hit.gameObject == caster) continue;
                hit.GetComponent<IHealth>()?.TakeDamage(explosionDamage);
            }
        }
    }
}