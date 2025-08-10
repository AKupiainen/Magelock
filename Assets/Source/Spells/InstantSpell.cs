using UnityEngine;
using Unity.Netcode;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "InstantSpell", menuName = "MageLock/Spells/Instant")]
    public class InstantSpell : Spell
    {
        [Header("Instant Effect Settings")]
        [SerializeField] protected float damage = 75f;
        [SerializeField] protected float radius = 5f;
        [SerializeField] protected GameObject effectPrefab;
        [SerializeField] protected float effectDuration = 2f;
        
        protected override void CastAtPosition(GameObject caster, Vector3 origin, Vector3 targetPosition)
        {
            Vector3 toTarget = targetPosition - origin;
            
            if (toTarget.magnitude > Range)
            {
                targetPosition = origin + toTarget.normalized * Range;
            }
            
            ApplyEffect(caster, targetPosition);
        }
        
        protected override void CastInDirection(GameObject caster, Vector3 origin, Vector3 direction)
        {
            Vector3 targetPosition = origin + direction * Range;
            ApplyEffect(caster, targetPosition);
        }
        
        protected virtual void ApplyEffect(GameObject caster, Vector3 position)
        {
            if (effectPrefab)
            {
                GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
                NetworkObject netObj = effect.GetComponent<NetworkObject>();
                
                if (netObj != null && NetworkManager.Singleton.IsServer)
                {
                    netObj.Spawn();
                }
                
                if (NetworkManager.Singleton.IsServer || !netObj)
                {
                    Destroy(effect, effectDuration);
                }
            }
            
            if (!NetworkManager.Singleton || NetworkManager.Singleton.IsServer)
            {
                var hits = Physics.OverlapSphere(position, radius, targetLayers);
                
                foreach (var hit in hits)
                {
                    if (hit.gameObject == caster) continue;
                    
                    var health = hit.GetComponent<IHealth>();
                    health?.TakeDamage(damage);
                }
                
                Debug.Log($"[{SpellName}] Applied {damage} damage in {radius}m radius at {position}");
            }
        }
    }
}