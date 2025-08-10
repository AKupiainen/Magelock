using UnityEngine;

namespace MageLock.Gameplay
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target Settings")]
        public Transform target;
        
        [Header("Camera Position")]
        public Vector3 fixedOffset = new(0f, 5f, -8f);
        
        [Header("Look Settings")]
        public Vector3 lookDirection = new(0f, -0.5f, 1f);
        
        [Header("Camera Shake")]
        public bool enableShake;
        public float shakeIntensity;
        
        private float _shakeTimer;
        
        private void LateUpdate()
        {
            if (!target) return;
            
            UpdateCameraPosition();
            UpdateCameraRotation();
            ApplyCameraShake();
        }
        
        private void UpdateCameraPosition()
        {
            transform.position = target.position + fixedOffset;
        }
        
        private void UpdateCameraRotation()
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
        }
        
        private void ApplyCameraShake()
        {
            if (!enableShake || _shakeTimer <= 0f) return;
            
            _shakeTimer -= Time.deltaTime;
            
            Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
            transform.position += shakeOffset;
            
            if (_shakeTimer <= 0f)
            {
                enableShake = false;
                shakeIntensity = 0f;
            }
        }
        
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
        
        public void Shake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            _shakeTimer = duration;
            enableShake = true;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (target == null) return;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position + fixedOffset, 0.5f);
            Gizmos.DrawLine(target.position, target.position + fixedOffset);
            
            Gizmos.color = Color.blue;
            Vector3 cameraPos = target.position + fixedOffset;
            Gizmos.DrawRay(cameraPos, lookDirection.normalized * 3f);
        }
    }
}