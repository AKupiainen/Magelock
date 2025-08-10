using UnityEngine;
using Unity.Netcode;
using MageLock.Events;
using MageLock.Spells;
using MageLock.DependencyInjection;
using System.Collections.Generic;

namespace MageLock.Gameplay
{
    /// <summary>
    /// Handles networked spell casting for a player
    /// </summary>
    public class SpellCaster : NetworkBehaviour
    {
        [Header("Spell Settings")]
        [SerializeField] private Transform castPoint; // Where spells originate from
        [SerializeField] private LayerMask targetingLayers = -1;
        [SerializeField] private float maxTargetingRange = 100f;
        
        [Inject] private SpellDatabase spellDatabase;
        
        // Cache components
        private Camera playerCamera;
        
        private void Awake()
        {
            if (castPoint == null)
                castPoint = transform;
        }
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            DIContainer.Instance.Inject(this);
            
            if (!IsOwner) return;
            
            // Subscribe to local UI events only for the local player
            EventsBus.Subscribe<SpellCastRequestEvent>(OnSpellCastRequested);
            
            // Get camera reference
            playerCamera = Camera.main;
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
        }
        
        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            
            EventsBus.Unsubscribe<SpellCastRequestEvent>(OnSpellCastRequested);
        }
        
        private void OnSpellCastRequested(SpellCastRequestEvent evt)
        {
            if (!IsOwner) return;
            
            // Note: Cooldown validation is now handled by SpellCooldownManager
            // It will trigger a SpellCastFailedEvent if on cooldown
            
            // Get targeting info
            Vector3 castDirection = GetCastDirection();
            Vector3 targetPosition = GetTargetPosition();
            
            // Request the cast on the server
            RequestSpellCastServerRpc(evt.SlotIndex, evt.Spell.SpellId, castDirection, targetPosition);
        }
        
        [ServerRpc]
        private void RequestSpellCastServerRpc(int slotIndex, int spellId, Vector3 direction, Vector3 targetPosition)
        {
            // Server validates the cast
            Spell spell = spellDatabase.GetSpell(spellId);
            if (spell == null)
            {
                Debug.LogWarning($"[SpellCaster] Invalid spell ID: {spellId}");
                return;
            }
            
            // Execute the spell on server
            ExecuteSpellCast(slotIndex, spell, direction, targetPosition);
            
            // Notify all clients
            SpellCastExecutedClientRpc(slotIndex, spellId, direction, targetPosition);
        }
        
        [ClientRpc]
        private void SpellCastExecutedClientRpc(int slotIndex, int spellId, Vector3 direction, Vector3 targetPosition)
        {
            Spell spell = spellDatabase.GetSpell(spellId);
            if (spell == null) return;
            
            // Trigger success event (cooldown manager will handle this)
            EventsBus.Trigger(new SpellCastSuccessEvent(spell, slotIndex, gameObject));
            
            // Visual/audio feedback could be handled here
            PlayCastEffects(spell, direction, targetPosition);
        }
        
        [ClientRpc]
        private void CastFailedClientRpc(int slotIndex, int spellId, SpellCastFailedEvent.FailureReason reason)
        {
            if (!IsOwner) return;
            
            Spell spell = spellDatabase.GetSpell(spellId);
            EventsBus.Trigger(new SpellCastFailedEvent(spell, slotIndex, reason));
        }
        
        private void ExecuteSpellCast(int slotIndex, Spell spell, Vector3 direction, Vector3 targetPosition)
        {
            // This is where the actual spell effect happens on the server
            // The spell's Cast method should handle networked instantiation
            
            switch (spell)
            {
                case ProjectileSpell projectileSpell:
                    CastProjectileSpell(projectileSpell, direction);
                    break;
                    
                case InstantSpell instantSpell:
                    CastInstantSpell(instantSpell, targetPosition);
                    break;
                    
                case Blink blinkSpell:
                    CastBlinkSpell(blinkSpell, direction);
                    break;
                    
                default:
                    // Fallback to the spell's own Cast method
                    spell.Cast(gameObject, direction);
                    break;
            }
        }
        
        private void CastProjectileSpell(ProjectileSpell spell, Vector3 direction)
        {
            // Server spawns networked projectile
            // This would use NetworkObject spawn
            spell.Cast(gameObject, direction);
        }
        
        private void CastInstantSpell(InstantSpell spell, Vector3 targetPosition)
        {
            // Calculate actual cast position based on range
            Vector3 origin = castPoint.position;
            Vector3 toTarget = targetPosition - origin;
            
            if (toTarget.magnitude > spell.Range)
            {
                targetPosition = origin + toTarget.normalized * spell.Range;
            }
            
            spell.Cast(gameObject, (targetPosition - origin).normalized);
        }
        
        private void CastBlinkSpell(Blink spell, Vector3 direction)
        {
            // Special handling for teleport spells
            spell.Cast(gameObject, direction);
        }
        
        private Vector3 GetCastDirection()
        {
            if (playerCamera == null)
                return transform.forward;
            
            // Cast from camera center
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            
            // See if we hit something
            if (Physics.Raycast(ray, out RaycastHit hit, maxTargetingRange, targetingLayers))
            {
                Vector3 direction = (hit.point - castPoint.position).normalized;
                return direction;
            }
            
            // No hit, use camera forward
            return playerCamera.transform.forward;
        }
        
        private Vector3 GetTargetPosition()
        {
            if (playerCamera == null)
                return transform.position + transform.forward * 10f;
            
            Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxTargetingRange, targetingLayers))
            {
                return hit.point;
            }
            
            return ray.GetPoint(maxTargetingRange);
        }
        
        private void PlayCastEffects(Spell spell, Vector3 direction, Vector3 targetPosition)
        {
            // Handle visual/audio feedback
            // This would be implemented based on your effect system
            Debug.Log($"[SpellCaster] Playing effects for {spell.name}");
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (castPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(castPoint.position, 0.2f);
                Gizmos.DrawRay(castPoint.position, transform.forward * 2f);
            }
        }
#endif
    }
}