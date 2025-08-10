using MageLock.Spells;

namespace MageLock.Events
{
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
    
    public class SpellCastFailedEvent : IEventData
    {
        public enum FailureReason
        {
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

    public class SpellSlotsClearedEvent : IEventData
    {
        public int[] ClearedSlots { get; }
        
        public SpellSlotsClearedEvent(params int[] clearedSlots)
        {
            ClearedSlots = clearedSlots;
        }
    }
}