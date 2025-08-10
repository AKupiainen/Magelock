using UnityEngine;
using Unity.Netcode;

namespace MageLock.Spells
{
    public class FireballProjectile : Projectile
    {
        private float _explosionRadius;
        private float _explosionDamage;
        private GameObject _explosionPrefab;

        private System.Action<Vector3, GameObject, float, float, GameObject> _onExplode;
        
        public void Initialize(GameObject caster, float damage, float speed, float lifetime, 
            System.Action<Vector3, GameObject, float, float, GameObject> onExplosion)
        {
            base.Initialize(caster, damage, speed, lifetime);
            _onExplode = onExplosion;
        }
        
        public void SetExplosionData(float radius, float damage, GameObject prefab)
        {
            _explosionRadius = radius;
            _explosionDamage = damage;
            _explosionPrefab = prefab;
        }
        
        protected override void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == Caster) return;
            
            if (Damage > 0)
            {
                var health = other.GetComponent<IHealth>();
                health?.TakeDamage(Damage);
            }
            
            _onExplode?.Invoke(transform.position, Caster, _explosionRadius, _explosionDamage, _explosionPrefab);
            
            NetworkObject netObj = GetComponent<NetworkObject>();
            
            if (netObj != null && NetworkManager.Singleton.IsServer)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}