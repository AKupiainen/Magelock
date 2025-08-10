using UnityEngine;
using Unity.Netcode;
using MageLock.Events;
using MageLock.Spells;
using System.Collections.Generic;

namespace MageLock.Gameplay
{
    public class SpellCooldownManager : NetworkBehaviour
    {
        [System.Serializable]
        private struct CooldownData
        {
            public float remainingTime;
            public Spell spell;
            
            public CooldownData(Spell spell, float duration)
            {
                this.spell = spell;
                remainingTime = duration;
            }
        }
        
        private readonly Dictionary<int, CooldownData> _activeCooldowns = new();
        
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            
            EventsBus.Subscribe<SpellCastSuccessEvent>(OnSpellCastSuccess);
            EventsBus.Subscribe<SpellCastRequestEvent>(OnSpellCastRequested);
        }
        
        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            
            EventsBus.Unsubscribe<SpellCastSuccessEvent>(OnSpellCastSuccess);
            EventsBus.Unsubscribe<SpellCastRequestEvent>(OnSpellCastRequested);
        }
        
        private void Update()
        {
            if (!IsOwner) return;
            
            UpdateCooldowns();
        }
        
        private void UpdateCooldowns()
        {
            if (_activeCooldowns.Count == 0) return;
            
            List<int> completedCooldowns = new List<int>();
            float deltaTime = Time.deltaTime;
            
            List<int> keys = new List<int>(_activeCooldowns.Keys);
            
            foreach (int slotIndex in keys)
            {
                if (!_activeCooldowns.TryGetValue(slotIndex, out CooldownData data))
                    continue;
                    
                data.remainingTime -= deltaTime;
                
                if (data.remainingTime <= 0f)
                {
                    completedCooldowns.Add(slotIndex);
                    OnCooldownComplete(slotIndex, data.spell);
                }
                else
                {
                    _activeCooldowns[slotIndex] = data; 
                }
            }
            
            foreach (int slot in completedCooldowns)
            {
                _activeCooldowns.Remove(slot);
            }
        }
        
        private void OnSpellCastRequested(SpellCastRequestEvent evt)
        {
            if (IsSlotOnCooldown(evt.SlotIndex))
            {
                float remaining = GetRemainingCooldown(evt.SlotIndex);
                
                EventsBus.Trigger(new SpellCastFailedEvent(
                    evt.Spell, 
                    evt.SlotIndex, 
                    SpellCastFailedEvent.FailureReason.OnCooldown,
                    $"{remaining:F1}s remaining"
                ));
            }
        }
        
        private void OnSpellCastSuccess(SpellCastSuccessEvent evt)
        {
            if (evt.Caster != gameObject) return; 
            StartCooldown(evt.SlotIndex, evt.Spell);
        }

        private void StartCooldown(int slotIndex, Spell spell)
        {
            if (spell == null) return;
            
            float duration = spell.Cooldown;
            _activeCooldowns[slotIndex] = new CooldownData(spell, duration);
            
            EventsBus.Trigger(new SpellCooldownStartedEvent(slotIndex, duration));
        }
        
        private bool IsSlotOnCooldown(int slotIndex)
        {
            return _activeCooldowns.ContainsKey(slotIndex) && _activeCooldowns[slotIndex].remainingTime > 0f;
        }

        private float GetRemainingCooldown(int slotIndex)
        {
            return _activeCooldowns.TryGetValue(slotIndex, out CooldownData data) ? Mathf.Max(0f, data.remainingTime) : 0f;
        }
        
        [ServerRpc]
        public void ClearCooldownServerRpc(int slotIndex)
        {
            ClearCooldownClientRpc(slotIndex);
        }
        
        [ClientRpc]
        private void ClearCooldownClientRpc(int slotIndex)
        {
            if (_activeCooldowns.Remove(slotIndex, out CooldownData data))
            {
                OnCooldownComplete(slotIndex, data.spell);
            }
        }
        
        private void OnCooldownComplete(int slotIndex, Spell spell)
        {
            Debug.Log($"[CooldownManager] Cooldown complete for {spell.name} in slot {slotIndex}");
            EventsBus.Trigger(new SpellCooldownCompleteEvent(slotIndex, spell));
        }
    }
}