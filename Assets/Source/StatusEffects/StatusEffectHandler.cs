using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using MageLock.Gameplay;

namespace MageLock.StatusEffects
{
    public class StatusEffectHandler : NetworkBehaviour
    {
        private float _currentMovementSpeed;
        private readonly List<StatusEffectInstance> activeEffects = new();
        private IMovement _movementComponent;
        
        private float _baseMovementSpeed;
        
        private void Awake()
        {
            _movementComponent = GetComponent<IMovement>();
            
            if (_movementComponent != null)
            {
                _baseMovementSpeed = _movementComponent.GetBaseSpeed();
                _currentMovementSpeed = _baseMovementSpeed;
            }
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
        
        public void ApplyEffect(StatusEffect effect, GameObject source = null)
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
                SyncMovementSpeedClientRpc(_currentMovementSpeed);
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
            
            _currentMovementSpeed = Mathf.Max(_baseMovementSpeed * 0.1f, _baseMovementSpeed * movementMultiplier);
            UpdateMovementController();
        }
        
        private void UpdateMovementController()
        {
            if (_movementComponent != null)
            {
                _movementComponent.SetSpeed(_currentMovementSpeed);
            }
        }
        
        [ClientRpc]
        private void SyncMovementSpeedClientRpc(float speed)
        {
            if (IsServer) return;
            
            _currentMovementSpeed = speed;
            UpdateMovementController();
        }
    }
}