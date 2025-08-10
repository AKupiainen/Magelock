using UnityEngine;
using System;
using MageLock.Gameplay;
using Unity.Netcode;
using MageLock.StatusEffects;

namespace MageLock.Spells
{
    public class FrostboltProjectile : Projectile
    {
        private SlowEffect _frostSlowEffect;
        
        public void SetSlowEffect(SlowEffect slowEffect)
        {
            _frostSlowEffect = slowEffect;
        }
        
        protected override void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == Caster) return;
            
            if (IsServer)
            {
                var health = other.GetComponent<IHealth>();
                health?.TakeDamage(Damage);
                
                var statusHandler = other.GetComponent<StatusEffectHandler>();
                
                if (statusHandler != null && _frostSlowEffect != null)
                {
                    statusHandler.ApplyEffect(_frostSlowEffect);
                }
                
                OnImpactCallback?.Invoke(transform.position, Caster);
                DestroyProjectile();
            }
        }
    }
}