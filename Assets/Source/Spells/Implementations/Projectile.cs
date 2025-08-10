using UnityEngine;
using System;
using Unity.Netcode;

namespace MageLock.Spells
{
    public class Projectile : MonoBehaviour
    {
        protected GameObject Caster;
        protected float Damage;
        protected float Speed;
        protected float Lifetime;
        
        protected Action<Vector3, GameObject> OnImpactCallback;
        
        public virtual void Initialize(GameObject caster, float damage, float speed, float lifetime, Action<Vector3, GameObject> onImpact = null)
        {
            Caster = caster;
            Damage = damage;
            Speed = speed;
            Lifetime = lifetime;
            OnImpactCallback = onImpact;
            
            Destroy(gameObject, lifetime);
        }
        
        protected virtual void Update()
        {
            transform.position += transform.forward * (Speed * Time.deltaTime);
        }
        
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == Caster) return;
            
            var health = other.GetComponent<IHealth>();
            health?.TakeDamage(Damage);
            
            OnImpactCallback?.Invoke(transform.position, Caster);
            
            DestroyProjectile();
        }
        
        protected virtual void DestroyProjectile()
        {
            NetworkObject netObj = GetComponent<NetworkObject>();
            
            if (netObj != null && NetworkManager.Singleton && NetworkManager.Singleton.IsServer)
            {
                netObj.Despawn();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject == Caster) return;
            
            Vector3 impactPoint = collision.contacts[0].point;
            OnImpactCallback?.Invoke(impactPoint, Caster);
            
            DestroyProjectile();
        }
    }
}