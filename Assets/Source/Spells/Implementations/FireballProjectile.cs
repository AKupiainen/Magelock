using UnityEngine;

namespace MageLock.Spells
{
    public class FireballProjectile : MonoBehaviour
    {
        private GameObject caster;
        private float damage;
        private float speed;
        private Fireball fireballSpell;
        
        public void Initialize(GameObject caster, float damage, float speed, float lifetime, Fireball spell)
        {
            this.caster = caster;
            this.damage = damage;
            this.speed = speed;
            this.fireballSpell = spell;
            Destroy(gameObject, lifetime);
        }
        
        void Update()
        {
            transform.position += transform.forward * speed * Time.deltaTime;
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == caster) return;
            
            var health = other.GetComponent<IHealth>();
            health?.TakeDamage(damage);
            
            fireballSpell.Explode(transform.position, caster);
            Destroy(gameObject);
        }
    }
}