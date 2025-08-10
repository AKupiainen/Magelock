using UnityEngine;
using Unity.Netcode;
using MageLock.Events;
using MageLock.Spells;
using MageLock.DependencyInjection;

namespace MageLock.Gameplay
{
    public class SpellCaster : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform castPoint;
        
        [Inject] private SpellDatabase _spellDatabase;
        
        private Camera _playerCamera;
        
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
            
            RequestSpellCastServerRpc(evt.SlotIndex, evt.Spell.SpellId);
        }
        
        [ServerRpc]
        private void RequestSpellCastServerRpc(int slotIndex, int spellId)
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
            
            Vector3 castDirection = GetAimDirection();
            spell.Cast(gameObject, castDirection);
            
            SpellCastExecutedClientRpc(slotIndex, spellId);
        }
        
        [ClientRpc]
        private void SpellCastExecutedClientRpc(int slotIndex, int spellId)
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
            return castPoint.position;
        }
        
        public Camera GetPlayerCamera()
        {
            return _playerCamera;
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