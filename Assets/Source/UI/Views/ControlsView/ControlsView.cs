using UnityEngine;
using MageLock.Events;
using MageLock.Spells;
using MageLock.DependencyInjection;
using System.Collections.Generic;

namespace MageLock.UI
{
    public class ControlsView : BaseView
    {
        [Header("Spell Buttons")]
        [SerializeField] private List<SpellButtonComponent> spellButtons = new List<SpellButtonComponent>();
        
        [Header("Assigned Spells")]
        [SerializeField] private List<Spell> assignedSpells = new();
        
        [Inject] private SpellDatabase _spellDatabase;
        
        protected override void Initialize()
        {
            InitializeButtons();
            RefreshAllSlots();
        }
        
        protected override void SubscribeToEvents()
        {
            EventsBus.Subscribe<SpellAssignedEvent>(OnSpellAssigned);
            EventsBus.Subscribe<SpellCooldownStartedEvent>(OnSpellCooldownStarted);
            EventsBus.Subscribe<SpellSlotsClearedEvent>(OnSlotsCleared);
        }
        
        protected override void UnsubscribeFromEvents()
        {
            EventsBus.Unsubscribe<SpellAssignedEvent>(OnSpellAssigned);
            EventsBus.Unsubscribe<SpellCooldownStartedEvent>(OnSpellCooldownStarted);
            EventsBus.Unsubscribe<SpellSlotsClearedEvent>(OnSlotsCleared);
        }
        
        private void InitializeButtons()
        {
            for (int i = 0; i < spellButtons.Count; i++)
            {
                if (spellButtons[i] != null)
                {
                    spellButtons[i].Initialize(i, HandleSpellButtonPressed);
                }
            }
        }
        
        private void HandleSpellButtonPressed(int slotIndex)
        {
            Spell spell = GetSpellAt(slotIndex);
            
            if (spell == null)
            {
                Debug.Log($"[SpellCastView] No spell in slot {slotIndex}");
                return;
            }
            
            EventsBus.Trigger(new SpellCastRequestEvent(spell, slotIndex));
        }
        
        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            foreach (var button in spellButtons)
            {
                button?.UpdateCooldown(deltaTime);
            }
        }

        private void AssignSpell(int slotIndex, Spell spell)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                Debug.LogWarning($"[SpellCastView] Invalid slot index: {slotIndex}");
                return;
            }
            
            while (assignedSpells.Count <= slotIndex)
            {
                assignedSpells.Add(null);
            }
            
            assignedSpells[slotIndex] = spell;
            RefreshSlot(slotIndex);
            
            Debug.Log($"[SpellCastView] Assigned {(spell != null ? spell.name : "null")} to slot {slotIndex}");
        }
        
        public void ClearSlot(int slotIndex)
        {
            AssignSpell(slotIndex, null);
        }
        
        public void ClearAllSlots()
        {
            for (int i = 0; i < assignedSpells.Count; i++)
            {
                assignedSpells[i] = null;
            }
            RefreshAllSlots();
        }
        
        private void RefreshSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex)) return;
            
            var button = spellButtons[slotIndex];
            Spell spell = GetSpellAt(slotIndex);
            
            if (spell != null)
            {
                button.SetInteractable(true);
                button.SetIcon(spell.Icon);
            }
            else
            {
                button.Clear();
            }
        }
        
        private void RefreshAllSlots()
        {
            for (int i = 0; i < spellButtons.Count; i++)
            {
                RefreshSlot(i);
            }
        }
        
        private Spell GetSpellAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= assignedSpells.Count)
                return null;
            return assignedSpells[slotIndex];
        }
        
        private bool IsValidSlotIndex(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < spellButtons.Count && spellButtons[slotIndex] != null;
        }
        
        private void OnSpellAssigned(SpellAssignedEvent evt)
        {
            AssignSpell(evt.SlotIndex, evt.Spell);
        }
        
        private void OnSpellCooldownStarted(SpellCooldownStartedEvent evt)
        {
            if (!IsValidSlotIndex(evt.SlotIndex)) return;
            
            var button = spellButtons[evt.SlotIndex];
            button.StartCooldown(evt.CooldownDuration);
        }
        
        private void OnSlotsCleared(SpellSlotsClearedEvent evt)
        {
            if (evt.ClearedSlots == null || evt.ClearedSlots.Length == 0)
            {
                ClearAllSlots();
            }
            else
            {
                foreach (int slotIndex in evt.ClearedSlots)
                {
                    ClearSlot(slotIndex);
                }
            }
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            
            while (assignedSpells.Count < spellButtons.Count)
            {
                assignedSpells.Add(null);
            }
            
            if (assignedSpells.Count > spellButtons.Count)
            {
                assignedSpells.RemoveRange(spellButtons.Count, assignedSpells.Count - spellButtons.Count);
            }
        }
#endif
    }
}