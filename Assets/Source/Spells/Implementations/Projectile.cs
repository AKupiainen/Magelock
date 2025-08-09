using UnityEngine;
using System;

namespace MageLock.Spells
{
    public class Projectile : MonoBehaviour
    {
        protected GameObject Caster;
        protected float Damage;
        protected float Speed;

        protected Action<Vector3, GameObject> OnImpactCallback;
        
        public virtual void Initialize(GameObject caster, float damage, float speed, float lifetime, Action<Vector3, GameObject> onImpact = null)
        {
            Caster = caster;
            Damage = damage;
            Speed = speed;
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
            
            Destroy(gameObject);
        }
    }
}