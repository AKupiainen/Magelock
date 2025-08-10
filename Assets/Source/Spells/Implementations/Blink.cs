using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace MageLock.Spells
{
    [CreateAssetMenu(fileName = "Blink", menuName = "MageLock/Spells/Abilities/Blink")]
    public class Blink : Spell
    {
        [Header("Teleport Settings")]
        [SerializeField] private float blinkDistance = 10f;
        [SerializeField] private GameObject blinkStartEffect;
        [SerializeField] private GameObject blinkEndEffect;
        [SerializeField] private bool checkForObstacles = true;
        
        protected override void CastInDirection(GameObject caster, Vector3 origin, Vector3 direction)
        {
            Vector3 blinkDirection = caster.transform.forward;
            Vector3 targetPosition = CalculateBlinkPosition(caster, caster.transform.position, blinkDirection);
            
            if (blinkStartEffect)
            {
                SpawnEffect(blinkStartEffect, caster.transform.position);
            }
            
            TeleportCaster(caster, targetPosition);
            
            if (blinkEndEffect)
            {
                SpawnEffect(blinkEndEffect, targetPosition);
            }
        }
        
        private Vector3 CalculateBlinkPosition(GameObject caster, Vector3 origin, Vector3 direction)
        {
            direction.y = 0;
            direction.Normalize();
            
            float actualDistance = Mathf.Min(blinkDistance, Range);
            Vector3 targetPos = caster.transform.position + direction * actualDistance;
            
            if (checkForObstacles)
            {
                if (Physics.Raycast(caster.transform.position, direction, out RaycastHit hit, actualDistance, targetLayers))
                {
                    targetPos = caster.transform.position + direction * (hit.distance - 0.5f);
                }
                
                if (Physics.Raycast(targetPos + Vector3.up * 2f, Vector3.down, out RaycastHit groundHit, 10f))
                {
                    targetPos.y = groundHit.point.y;
                }
            }
            
            return targetPos;
        }
        
        private void TeleportCaster(GameObject caster, Vector3 targetPosition)
        {
            var networkTransform = caster.GetComponent<NetworkTransform>();
            
            if (networkTransform != null && NetworkManager.Singleton.IsServer)
            {
                networkTransform.Teleport(targetPosition, caster.transform.rotation, caster.transform.localScale);
            }
            else
            {
                caster.transform.position = targetPosition;
            }
        }
        
        private void SpawnEffect(GameObject effectPrefab, Vector3 position)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            
            NetworkObject netObj = effect.GetComponent<NetworkObject>();
            
            if (netObj != null && NetworkManager.Singleton.IsServer)
            {
                netObj.Spawn();
            }
        }
    }
}