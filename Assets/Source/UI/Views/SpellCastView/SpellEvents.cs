using MageLock.Spells;

namespace MageLock.Events
{
    /// <summary>
    /// Event fired when a spell cast is requested from UI
    /// </summary>
    public class SpellCastRequestEvent : IEventData
    {
        public Spell Spell { get; }
        public int SlotIndex { get; }
        
        public SpellCastRequestEvent(Spell spell, int slotIndex)
        {
            Spell = spell;
            SlotIndex = slotIndex;
        }
    }
    
    /// <summary>
    /// Event fired when a spell starts its cooldown
    /// </summary>
    public class SpellCooldownStartedEvent : IEventData
    {
        public int SlotIndex { get; }
        public float CooldownDuration { get; }
        
        public SpellCooldownStartedEvent(int slotIndex, float cooldownDuration)
        {
            SlotIndex = slotIndex;
            CooldownDuration = cooldownDuration;
        }
    }
    
    /// <summary>
    /// Event fired when a spell is assigned to a slot
    /// </summary>
    public class SpellAssignedEvent : IEventData
    {
        public int SlotIndex { get; }
        public Spell Spell { get; }
        
        public SpellAssignedEvent(int slotIndex, Spell spell)
        {
            SlotIndex = slotIndex;
            Spell = spell;
        }
    }
    
    /// <summary>
    /// Event fired when a spell is successfully cast
    /// </summary>
    public class SpellCastSuccessEvent : IEventData
    {
        public Spell Spell { get; }
        public int SlotIndex { get; }
        public UnityEngine.GameObject Caster { get; }
        
        public SpellCastSuccessEvent(Spell spell, int slotIndex, UnityEngine.GameObject caster)
        {
            Spell = spell;
            SlotIndex = slotIndex;
            Caster = caster;
        }
    }
    
    /// <summary>
    /// Event fired when a spell cast fails
    /// </summary>
    public class SpellCastFailedEvent : IEventData
    {
        public enum FailureReason
        {
            InsufficientMana,
            OnCooldown,
            OutOfRange,
            InvalidTarget,
            Interrupted,
            NotLearned
        }
        
        public Spell Spell { get; }
        public int SlotIndex { get; }
        public FailureReason Reason { get; }
        public string Message { get; }
        
        public SpellCastFailedEvent(Spell spell, int slotIndex, FailureReason reason, string message = "")
        {
            Spell = spell;
            SlotIndex = slotIndex;
            Reason = reason;
            Message = message;
        }
    }
    
    /// <summary>
    /// Event fired when a spell cooldown is complete
    /// </summary>
    public class SpellCooldownCompleteEvent : IEventData
    {
        public int SlotIndex { get; }
        public Spell Spell { get; }
        
        public SpellCooldownCompleteEvent(int slotIndex, Spell spell)
        {
            SlotIndex = slotIndex;
            Spell = spell;
        }
    }
    
    /// <summary>
    /// Event fired when spell slots are cleared
    /// </summary>
    public class SpellSlotsClearedEvent : IEventData
    {
        public int[] ClearedSlots { get; }
        
        public SpellSlotsClearedEvent(params int[] clearedSlots)
        {
            ClearedSlots = clearedSlots;
        }
    }
}