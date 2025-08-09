using UnityEngine;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "Blink", menuName = "MageLock/Spells/Abilities/Blink")]
    public class Blink : Spell
    {
        [Header("Teleport")]
        [SerializeField] private float maxBlinkDistance = 10f;
        [SerializeField] private GameObject blinkStartEffect;
        [SerializeField] private GameObject blinkEndEffect;
        [SerializeField] private LayerMask obstacleLayer = -1;
        
        public override void Cast(GameObject caster, Vector3 direction)
        {
            var targetPosition = CalculateBlinkPosition(caster.transform.position, direction);
            
            if (blinkStartEffect)
                Instantiate(blinkStartEffect, caster.transform.position, Quaternion.identity);
            
            TeleportCaster(caster, targetPosition);
            
            if (blinkEndEffect)
                Instantiate(blinkEndEffect, targetPosition, Quaternion.identity);
        }
        
        private Vector3 CalculateBlinkPosition(Vector3 origin, Vector3 direction)
        {
            direction.y = 0;
            direction.Normalize();
            
            float actualDistance = Mathf.Min(maxBlinkDistance, Range);
            
            if (Physics.Raycast(origin, direction, out RaycastHit hit, actualDistance, obstacleLayer))
            {
                return origin + direction * (hit.distance - 0.5f);
            }
            
            return origin + direction * actualDistance;
        }
        
        private void TeleportCaster(GameObject caster, Vector3 targetPosition)
        {
            caster.transform.position = targetPosition;
            var networkTransform = caster.GetComponent<Unity.Netcode.Components.NetworkTransform>();
            
            if (networkTransform)
            {
                networkTransform.Teleport(targetPosition, caster.transform.rotation, caster.transform.localScale);
            }
        }
    }
}