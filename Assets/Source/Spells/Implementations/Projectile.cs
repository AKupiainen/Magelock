using UnityEngine;
using System;

namespace MageLock.Spells
{
    public class Projectile : MonoBehaviour
    {
        protected GameObject caster;
        protected float damage;
        protected float speed;
        protected float lifetime;
        
        protected Action<Vector3, GameObject> onImpactCallback;
        
        public virtual void Initialize(GameObject caster, float damage, float speed, float lifetime, Action<Vector3, GameObject> onImpact = null)
        {
            this.caster = caster;
            this.damage = damage;
            this.speed = speed;
            this.lifetime = lifetime;
            this.onImpactCallback = onImpact;
            
            Destroy(gameObject, lifetime);
        }
        
        protected virtual void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
        
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == caster) return;
            
            var health = other.GetComponent<IHealth>();
            health?.TakeDamage(damage);
            
            onImpactCallback?.Invoke(transform.position, caster);
            
            Destroy(gameObject);
        }
    }
}