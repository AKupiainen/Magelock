using UnityEngine;
using System;
using MageLock.Gameplay;
using Unity.Netcode;

namespace MageLock.Spells
{
    public class Projectile : NetworkBehaviour
    {
        protected readonly NetworkVariable<float> NetworkSpeed = new();
        protected readonly NetworkVariable<float> NetworkDamage = new();
        protected readonly NetworkVariable<NetworkObjectReference> NetworkCaster = new();
        
        protected GameObject Caster;
        protected float Damage;
        protected float Speed;
        protected Action<Vector3, GameObject> OnImpactCallback;
        
        public virtual void Initialize(GameObject caster, float damage, float speed, Action<Vector3, GameObject> onImpact = null)
        {
            Caster = caster;
            Damage = damage;
            Speed = speed;
            OnImpactCallback = onImpact;
            
            if (IsServer)
            {
                NetworkSpeed.Value = speed;
                NetworkDamage.Value = damage;
                
                NetworkObject casterNetObj = caster.GetComponent<NetworkObject>();
                if (casterNetObj != null)
                {
                    NetworkCaster.Value = casterNetObj;
                }
            }
        }
        
        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                Speed = NetworkSpeed.Value;
                Damage = NetworkDamage.Value;
                
                if (NetworkCaster.Value.TryGet(out NetworkObject casterNetObj))
                {
                    Caster = casterNetObj.gameObject;
                }
            }
        }
        
        protected virtual void Update()
        {
            float moveSpeed = IsServer ? Speed : NetworkSpeed.Value;
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);
        }
        
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (other.gameObject == Caster) return;
            
            if (IsServer)
            {
                var health = other.GetComponent<IHealth>();
                health?.TakeDamage(Damage);
                
                OnImpactCallback?.Invoke(transform.position, Caster);
                DestroyProjectile();
            }
        }
        
        protected virtual void DestroyProjectile()
        {
            if (IsServer)
            {
                NetworkObject netObj = GetComponent<NetworkObject>();
                
                if (netObj != null && netObj.IsSpawned)
                {
                    netObj.Despawn();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
        
        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject == Caster) return;
            
            if (IsServer)
            {
                Vector3 impactPoint = collision.contacts[0].point;
                OnImpactCallback?.Invoke(impactPoint, Caster);
                
                DestroyProjectile();
            }
        }
    }
}