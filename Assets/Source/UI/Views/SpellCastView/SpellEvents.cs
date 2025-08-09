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