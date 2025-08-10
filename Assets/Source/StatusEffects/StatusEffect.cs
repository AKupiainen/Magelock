using UnityEngine;

namespace MageLock.StatusEffects
{
    public abstract class StatusEffect : ScriptableObject
    {
        [Header("Base Settings")]
        [SerializeField] protected string effectName = "Status Effect";
        [SerializeField] protected float duration = 3f;
        
        [Header("Stacking")]
        [SerializeField] protected StackingType stackingType = StackingType.Refresh;
        [SerializeField] protected int maxStacks = 1;
        
        public enum StackingType
        {
            Refresh,        
            Stack,          
            Ignore,         
            Independent     
        }
        
        public float Duration => duration;
        public StackingType Stacking => stackingType;
        public int MaxStacks => maxStacks;
        
        public abstract void OnApply(StatusEffectHandler handler, StatusEffectInstance instance);
        public abstract void OnRemove(StatusEffectHandler handler, StatusEffectInstance instance);
        public abstract void OnTick(StatusEffectHandler handler, StatusEffectInstance instance);
    }
    
    [System.Serializable]
    public class StatusEffectInstance
    {
        public StatusEffect effect;
        public float startTime;
        public float duration;
        public int stacks = 1;

        public bool IsExpired => Time.time >= startTime + duration;
        public float RemainingTime => Mathf.Max(0, startTime + duration - Time.time);
        
        public StatusEffectInstance(StatusEffect effect)
        {
            this.effect = effect;

            duration = effect.Duration;
            startTime = Time.time;
        }
    }
}