using UnityEngine;
using Unity.Netcode;
using MageLock.Events;
using MageLock.Spells;
using System.Collections.Generic;

namespace MageLock.Gameplay
{
    /// <summary>
    /// Manages spell cooldowns for a player, separate from UI and casting logic
    /// </summary>
    public class SpellCooldownManager : NetworkBehaviour
    {
        [System.Serializable]
        private struct CooldownData
        {
            public float remainingTime;
            public float totalDuration;
            public Spell spell;
            
            public CooldownData(Spell spell, float duration)
            {
                this.spell = spell;
                this.totalDuration = duration;
                this.remainingTime = duration;
            }
        }
        
        // Track cooldowns per slot
        private Dictionary<int, CooldownData> activeCooldowns = new Dictionary<int, CooldownData>();
        
        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;
            
            // Listen for successful casts to start cooldowns
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
            if (activeCooldowns.Count == 0) return;
            
            List<int> completedCooldowns = new List<int>();
            float deltaTime = Time.deltaTime;
            
            // Create a copy of the keys to iterate over
            List<int> keys = new List<int>(activeCooldowns.Keys);
            
            foreach (int slotIndex in keys)
            {
                if (!activeCooldowns.TryGetValue(slotIndex, out CooldownData data))
                    continue;
                    
                data.remainingTime -= deltaTime;
                
                if (data.remainingTime <= 0f)
                {
                    completedCooldowns.Add(slotIndex);
                    OnCooldownComplete(slotIndex, data.spell);
                }
                else
                {
                    activeCooldowns[slotIndex] = data; // Update the struct
                }
            }
            
            // Remove completed cooldowns
            foreach (int slot in completedCooldowns)
            {
                activeCooldowns.Remove(slot);
            }
        }
        
        private void OnSpellCastRequested(SpellCastRequestEvent evt)
        {
            // Check if spell is on cooldown and notify if it is
            if (IsSlotOnCooldown(evt.SlotIndex))
            {
                float remaining = GetRemainingCooldown(evt.SlotIndex);
                Debug.Log($"[CooldownManager] Spell {evt.Spell.name} on cooldown: {remaining:F1}s remaining");
                
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
            if (evt.Caster != gameObject) return; // Only track our own cooldowns
            
            StartCooldown(evt.SlotIndex, evt.Spell);
        }
        
        /// <summary>
        /// Start a cooldown for a spell slot
        /// </summary>
        public void StartCooldown(int slotIndex, Spell spell)
        {
            if (spell == null) return;
            
            float duration = spell.Cooldown;
            activeCooldowns[slotIndex] = new CooldownData(spell, duration);
            
            Debug.Log($"[CooldownManager] Started {duration}s cooldown for {spell.name} in slot {slotIndex}");
            
            // Notify UI to start visual cooldown
            EventsBus.Trigger(new SpellCooldownStartedEvent(slotIndex, duration));
        }
        
        /// <summary>
        /// Check if a slot is currently on cooldown
        /// </summary>
        public bool IsSlotOnCooldown(int slotIndex)
        {
            return activeCooldowns.ContainsKey(slotIndex) && activeCooldowns[slotIndex].remainingTime > 0f;
        }
        
        /// <summary>
        /// Get remaining cooldown time for a slot
        /// </summary>
        public float GetRemainingCooldown(int slotIndex)
        {
            if (activeCooldowns.TryGetValue(slotIndex, out CooldownData data))
            {
                return Mathf.Max(0f, data.remainingTime);
            }
            return 0f;
        }
        
        /// <summary>
        /// Get cooldown progress (0 = ready, 1 = just cast)
        /// </summary>
        public float GetCooldownProgress(int slotIndex)
        {
            if (activeCooldowns.TryGetValue(slotIndex, out CooldownData data))
            {
                return data.remainingTime / data.totalDuration;
            }
            return 0f;
        }
        
        /// <summary>
        /// Force clear a cooldown (for special abilities or items)
        /// </summary>
        [ServerRpc]
        public void ClearCooldownServerRpc(int slotIndex)
        {
            ClearCooldownClientRpc(slotIndex);
        }
        
        [ClientRpc]
        private void ClearCooldownClientRpc(int slotIndex)
        {
            if (activeCooldowns.TryGetValue(slotIndex, out CooldownData data))
            {
                activeCooldowns.Remove(slotIndex);
                OnCooldownComplete(slotIndex, data.spell);
            }
        }
        
        /// <summary>
        /// Clear all cooldowns
        /// </summary>
        public void ClearAllCooldowns()
        {
            List<int> slots = new List<int>(activeCooldowns.Keys);
            foreach (int slot in slots)
            {
                ClearCooldownServerRpc(slot);
            }
        }
        
        private void OnCooldownComplete(int slotIndex, Spell spell)
        {
            Debug.Log($"[CooldownManager] Cooldown complete for {spell.name} in slot {slotIndex}");
            EventsBus.Trigger(new SpellCooldownCompleteEvent(slotIndex, spell));
        }
        
        /// <summary>
        /// Get all active cooldowns (for UI display)
        /// </summary>
        public Dictionary<int, float> GetActiveCooldowns()
        {
            Dictionary<int, float> result = new Dictionary<int, float>();
            foreach (var kvp in activeCooldowns)
            {
                result[kvp.Key] = kvp.Value.remainingTime;
            }
            return result;
        }
        
#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!IsOwner || activeCooldowns.Count == 0) return;
            
            // Debug display of active cooldowns
            GUILayout.BeginArea(new Rect(10, 200, 200, 300));
            GUILayout.Label("Active Cooldowns:");
            
            foreach (var kvp in activeCooldowns)
            {
                string spellName = kvp.Value.spell != null ? kvp.Value.spell.name : "Unknown";
                GUILayout.Label($"Slot {kvp.Key}: {spellName} - {kvp.Value.remainingTime:F1}s");
            }
            
            GUILayout.EndArea();
        }
#endif
    }
}