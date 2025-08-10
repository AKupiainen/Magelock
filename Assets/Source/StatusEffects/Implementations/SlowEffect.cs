using UnityEngine;

namespace MageLock.StatusEffects
{
    [CreateAssetMenu(fileName = "SlowEffect", menuName = "MageLock/Status Effects/Slow")]
    public class SlowEffect : StatusEffect
    {
        [Header("Slow Settings")]
        [Range(0f, 0.9f)]
        [SerializeField] private float slowPercentage = 0.5f; 
        
        public float SlowPercentage => slowPercentage;
        
        public override void OnApply(StatusEffectHandler handler, StatusEffectInstance instance)
        {
            handler.RecalculateMovement();
        }
        
        public override void OnRemove(StatusEffectHandler handler, StatusEffectInstance instance)
        {
            handler.RecalculateMovement();
        }
        
        public override void OnTick(StatusEffectHandler handler, StatusEffectInstance instance) { }
    }
}