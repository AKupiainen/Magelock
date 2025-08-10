using Unity.Netcode;
using UnityEngine;

namespace MageLock.Spells
{
    public class FireballProjectile : Projectile
    {
        private readonly NetworkVariable<float> _networkExplosionRadius = new();
        private readonly NetworkVariable<float> _networkExplosionDamage = new();
        
        private float _explosionRadius;
        private float _explosionDamage;
        private GameObject _explosionPrefab;
        private System.Action<Vector3, GameObject, float, float, GameObject> _onExplode;
        
        public void Initialize(GameObject caster, float damage, float speed, 
            System.Action<Vector3, GameObject, float, float, GameObject> onExplosion)
        {
            base.Initialize(caster, damage, speed);
            _onExplode = onExplosion;
        }
        
        public void SetExplosionData(float radius, float damage, GameObject prefab)
        {
            _explosionRadius = radius;
            _explosionDamage = damage;
            _explosionPrefab = prefab;
            
            if (IsServer)
            {
                _networkExplosionRadius.Value = radius;
                _networkExplosionDamage.Value = damage;
            }
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsServer)
            {
                _explosionRadius = _networkExplosionRadius.Value;
                _explosionDamage = _networkExplosionDamage.Value;
            }
        }
        
        protected override void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == Caster) return;
            
            if (IsServer)
            {
                if (Damage > 0)
                {
                    var health = other.GetComponent<IHealth>();
                    health?.TakeDamage(Damage);
                }
                
                _onExplode?.Invoke(transform.position, Caster, _explosionRadius, _explosionDamage, _explosionPrefab);
                
                DestroyProjectile();
            }
        }
    }
}