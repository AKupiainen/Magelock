using UnityEngine;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "ProjectileSpell", menuName = "MageLock/Spells/Projectile")]
    public class ProjectileSpell : Spell
    {
        [SerializeField] protected GameObject projectilePrefab;
        [SerializeField] protected float projectileSpeed = 20f;
        [SerializeField] protected float damage = 50f;
        [SerializeField] protected float lifetime = 5f;
        
        public override void Cast(GameObject caster, Vector3 direction)
        {
            if (!projectilePrefab) return;
            
            var spawnPos = caster.transform.position + Vector3.up + direction * 0.5f;
            var projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
            
            var proj = projectile.GetComponent<Projectile>();
            if (!proj) proj = projectile.AddComponent<Projectile>();
            
            proj.Initialize(caster, damage, projectileSpeed, lifetime);
        }
    }
}