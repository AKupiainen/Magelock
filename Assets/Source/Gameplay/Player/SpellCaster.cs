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
            
            if (!IsOwner) return;
            
            DIContainer.Instance.Inject(this);
            
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
            
            Debug.Log($"[SpellCaster] Cast {spell.name} successfully");
        }
        
        public Vector3 GetAimDirection()
        {
            if (_playerCamera == null)
                return transform.forward;
            
            Ray ray = _playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                return (hit.point - castPoint.position).normalized;
            }
            
            return _playerCamera.transform.forward;
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