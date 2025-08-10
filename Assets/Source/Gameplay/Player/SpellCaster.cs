using UnityEngine;
using Unity.Netcode;
using MageLock.Events;
using MageLock.Spells;
using MageLock.DependencyInjection;
using MageLock.Utilities;

namespace MageLock.Gameplay
{
    public class SpellCaster : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkedAnimationPointTracker castPoint;
        
        [Inject] private SpellDatabase _spellDatabase;
        
        private Camera _playerCamera;
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            DIContainer.Instance.Inject(this);
            
            if (!IsOwner) return;
            
            EventsBus.Subscribe<SpellCastRequestEvent>(OnSpellCastRequested);
            
            if (_playerCamera == null)
                _playerCamera = GetComponentInChildren<Camera>();
        }
        
        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            
            EventsBus.Unsubscribe<SpellCastRequestEvent>(OnSpellCastRequested);
        }
        
        private void OnSpellCastRequested(SpellCastRequestEvent evt)
        {
            if (!IsOwner) return;
            
            Vector3 castPos = GetCastPoint();
            Vector3 castDir = GetAimDirection();
            
            RequestSpellCastServerRpc(evt.SlotIndex, evt.Spell.SpellId, castPos, castDir);
        }
        
        [ServerRpc]
        private void RequestSpellCastServerRpc(int slotIndex, int spellId, Vector3 castPosition, Vector3 castDirection)
        {
            if (_spellDatabase == null)
            {
                Debug.LogError($"[SpellCaster] SpellDatabase is null on server! Make sure DI is setup.");
                return;
            }
            
            Spell spell = _spellDatabase.GetSpell(spellId);
            
            if (spell == null)
            {
                Debug.LogWarning($"[SpellCaster] Invalid spell ID: {spellId}");
                return;
            }
            
            spell.Cast(gameObject, castDirection);
            
            SpellCastExecutedClientRpc(slotIndex, spellId, castPosition);
        }
        
        [ClientRpc]
        private void SpellCastExecutedClientRpc(int slotIndex, int spellId, Vector3 castPosition)
        {
            Spell spell = _spellDatabase.GetSpell(spellId);
            if (spell == null) return;
            
            EventsBus.Trigger(new SpellCastSuccessEvent(spell, slotIndex, gameObject));
        }
        
        private Vector3 GetAimDirection()
        {
            return transform.forward;
        }
        
        public Vector3 GetCastPoint()
        {
            return castPoint.GetPointPosition("CastPoint");
        }
        
        public Camera GetPlayerCamera()
        {
            return _playerCamera;
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (castPoint == null) return;
            
            Vector3 castPos = GetCastPoint();
            Vector3 aimDir = GetAimDirection();
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(castPos, 0.15f);
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(castPos, aimDir * 2f);
            
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawLine(transform.position, castPos);
            
            UnityEditor.Handles.Label(castPos + Vector3.up * 0.2f, "Cast Point");
        }
#endif
    }
}