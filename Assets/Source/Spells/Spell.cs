using MageLock.Gameplay;
using UnityEngine;

namespace MageLock.Spells
{
    public abstract class Spell : ScriptableObject
    {
        [Header("Base Settings")]
        [SerializeField] private int spellId;
        [SerializeField] private string spellName;
        [SerializeField] private Sprite icon;
        
        [Header("Casting")]
        [SerializeField] private float cooldown = 1f;
        [SerializeField] private float range = 10f;
        
        [Header("Targeting")]
        [SerializeField] private TargetingType targetingType = TargetingType.Direction;
        [SerializeField] protected LayerMask targetLayers = -1;
        
        public int SpellId => spellId;
        public string SpellName => spellName;
        public Sprite Icon => icon;
        public float Cooldown => cooldown;
        public float Range => range;
        
        public enum TargetingType
        {
            Direction,      
            GroundTarget,   
            SelfCast,       
            NoTarget        
        }
        
        public virtual void Cast(GameObject caster, Vector3 direction)
        {
            Vector3 castPosition = GetCastPosition(caster);
            
            switch (targetingType)
            {
                case TargetingType.Direction:
                    CastInDirection(caster, castPosition, direction);
                    break;
                    
                case TargetingType.GroundTarget:
                    Vector3 targetPos = GetGroundTarget(caster, direction);
                    CastAtPosition(caster, castPosition, targetPos);
                    break;
                    
                case TargetingType.SelfCast:
                    CastOnSelf(caster);
                    break;
                    
                case TargetingType.NoTarget:
                    CastNoTarget(caster);
                    break;
            }
        }
        
        protected virtual Vector3 GetCastPosition(GameObject caster)
        {
            var spellCaster = caster.GetComponent<SpellCaster>();
            
            if (spellCaster != null)
            {
                return spellCaster.GetCastPoint();
            }
            
            return caster.transform.position + Vector3.up;
        }
        
        protected virtual Vector3 GetGroundTarget(GameObject caster, Vector3 direction)
        {
            var spellCaster = caster.GetComponent<SpellCaster>();
            
            if (spellCaster != null)
            {
                Camera cam = spellCaster.GetPlayerCamera();
                
                if (cam != null)
                {
                    Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
                    if (Physics.Raycast(ray, out RaycastHit hit, range, targetLayers))
                    {
                        return hit.point;
                    }
                    return ray.GetPoint(range);
                }
            }
            
            return caster.transform.position + direction * range;
        }
        
        protected virtual void CastInDirection(GameObject caster, Vector3 origin, Vector3 direction)
        {
            Debug.LogWarning($"[{spellName}] CastInDirection not implemented");
        }
        
        protected virtual void CastAtPosition(GameObject caster, Vector3 origin, Vector3 targetPosition)
        {
            Debug.LogWarning($"[{spellName}] CastAtPosition not implemented");
        }
        
        protected virtual void CastOnSelf(GameObject caster)
        {
            Debug.LogWarning($"[{spellName}] CastOnSelf not implemented");
        }
        
        protected virtual void CastNoTarget(GameObject caster)
        {
            Debug.LogWarning($"[{spellName}] CastNoTarget not implemented");
        }
        
        public virtual bool CanCast(GameObject caster)
        {
            return true;
        }
        
#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (spellId == 0)
            {
                spellId = GetInstanceID();
            }
            
            if (string.IsNullOrEmpty(spellName))
            {
                spellName = name;
            }
        }
#endif
    }
}