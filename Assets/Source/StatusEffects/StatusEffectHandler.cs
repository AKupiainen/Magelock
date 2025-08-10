using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;

namespace MageLock.StatusEffects
{
    public class StatusEffectHandler : NetworkBehaviour
    {
        [Header("Base Stats")]
        [SerializeField] private float baseMovementSpeed = 5f;
        
        [Header("Current Stats")]
        [SerializeField] private float currentMovementSpeed;
        
        [SerializeField] private List<StatusEffectInstance> activeEffects = new();
        
        private void Awake()
        {
            currentMovementSpeed = baseMovementSpeed;
        }
        
        private void Update()
        {
            if (!IsServer) return;
            
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                var instance = activeEffects[i];
                
                if (instance.IsExpired)
                {
                    RemoveEffect(instance);
                    continue;
                }
                
                instance.effect.OnTick(this, instance);
            }
        }
        

        public void ApplyEffect(StatusEffect effect)
        {
            if (!IsServer) return;
            if (effect == null) return;
            
            var existingEffect = activeEffects.FirstOrDefault(e => e.effect == effect);
            
            if (existingEffect != null)
            {
                switch (effect.Stacking)
                {
                    case StatusEffect.StackingType.Refresh:
                        existingEffect.startTime = Time.time;
                        existingEffect.duration = effect.Duration;
                        break;
                        
                    case StatusEffect.StackingType.Stack:
                        if (existingEffect.stacks < effect.MaxStacks)
                        {
                            existingEffect.stacks++;
                            existingEffect.startTime = Time.time;
                        }
                        break;
                        
                    case StatusEffect.StackingType.Ignore:
                        return;
                        
                    case StatusEffect.StackingType.Independent:
                        AddNewEffect(effect);
                        break;
                }
            }
            else
            {
                AddNewEffect(effect);
            }
            
            if (effect is SlowEffect)
            {
                SyncMovementSpeedClientRpc(currentMovementSpeed);
            }
        }
        
        private void AddNewEffect(StatusEffect effect)
        {
            var instance = new StatusEffectInstance(effect);
            activeEffects.Add(instance);
            effect.OnApply(this, instance);
        }
        
        private void RemoveEffect(StatusEffectInstance instance)
        {
            instance.effect.OnRemove(this, instance);
            activeEffects.Remove(instance);
            
            if (instance.effect is SlowEffect)
            {
                RecalculateMovement();
            }
        }
        
        public void RecalculateMovement()
        {
            float movementMultiplier = 1f;
            
            var slowEffects = activeEffects.Where(e => e.effect is SlowEffect);
            
            foreach (var instance in slowEffects)
            {
                var slow = instance.effect as SlowEffect;
                if (slow != null) movementMultiplier *= (1f - slow.SlowPercentage);
            }
            
            currentMovementSpeed = Mathf.Max(baseMovementSpeed * 0.1f, baseMovementSpeed * movementMultiplier);
            UpdateMovementController();
        }
        
        private void UpdateMovementController()
        {
            var movement = GetComponent<IMovement>();
            movement?.SetSpeed(currentMovementSpeed);
        }
        
        [ClientRpc]
        private void SyncMovementSpeedClientRpc(float speed)
        {
            if (IsServer) return;
            
            currentMovementSpeed = speed;
            UpdateMovementController();
        }
    }
    
    public interface IMovement
    {
        void SetSpeed(float speed);
    }
}