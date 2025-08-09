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
            base.Cast(caster, direction);
        }
        
        public void OnImpact(Vector3 position, GameObject caster)
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