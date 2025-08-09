using UnityEngine;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "InstantSpell", menuName = "MageLock/Spells/Instant")]
    public class InstantSpell : Spell
    {
        [SerializeField] protected float damage = 75f;
        [SerializeField] protected float radius = 5f;
        [SerializeField] protected GameObject effectPrefab;
        [SerializeField] protected LayerMask targetLayers = -1;
        
        public override void Cast(GameObject caster, Vector3 direction)
        {
            var position = caster.transform.position + direction * Range;
            ApplyEffect(caster, position);
        }
        
        protected virtual void ApplyEffect(GameObject caster, Vector3 position)
        {
            if (effectPrefab)
                Instantiate(effectPrefab, position, Quaternion.identity);
            
            var hits = Physics.OverlapSphere(position, radius, targetLayers);
            foreach (var hit in hits)
            {
                if (hit.gameObject == caster) continue;
                
                var health = hit.GetComponent<IHealth>();
                health?.TakeDamage(damage);
            }
        }
    }
}