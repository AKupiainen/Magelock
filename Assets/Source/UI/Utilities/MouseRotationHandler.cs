using UnityEngine;
using UnityEngine.EventSystems;

namespace MageLock.UI
{
    public class MouseRotationHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Rotation Settings")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private bool invertHorizontal;
        
        [Header("Smooth Rotation Settings")]
        [SerializeField] private float dampingFactor = 0.95f; 
        [SerializeField] private float minimumVelocity = 0.1f; 
        [SerializeField] private bool enableSmoothRotation = true;
        
        [Header("Auto-Reset Settings")]
        [SerializeField] private bool enableAutoReset = true;
        [SerializeField] private float autoResetDelay = 3f;
        [SerializeField] private float autoResetSpeed = 200f;
        
        [Header("Optional Settings")]
        [SerializeField] private bool resetOnStart;
        
        private bool isDragging;
        private Vector2 lastMousePosition;
        private Vector3 initialRotation;
        private float currentHorizontalRotation;
        
        private float rotationVelocity;
        private bool isDecelerating;
        
        private float lastInputTime;
        private bool isAutoResetting;
        
        private void Start()
        {
            InitializeTarget();
            lastInputTime = Time.time;
        }
        
        private void Update()
        {
            if (enableSmoothRotation)
            {
                HandleSmoothRotation();
            }
            
            if (enableAutoReset)
            {
                HandleAutoReset();
            }
        }
        
        private void HandleSmoothRotation()
        {
            if (!isDragging && isDecelerating && targetTransform != null && !isAutoResetting)
            {
                currentHorizontalRotation += rotationVelocity * Time.deltaTime;
                
                rotationVelocity *= dampingFactor;
                
                if (Mathf.Abs(rotationVelocity) < minimumVelocity)
                {
                    rotationVelocity = 0f;
                    isDecelerating = false;
                }
                
                targetTransform.rotation = Quaternion.Euler(
                    initialRotation.x,
                    currentHorizontalRotation,
                    initialRotation.z
                );
            }
        }
        
        private void HandleAutoReset()
        {
            if (isDragging || isDecelerating || targetTransform == null)
                return;
                
            if (Time.time - lastInputTime >= autoResetDelay)
            {
                float targetY = initialRotation.y;
                float currentY = currentHorizontalRotation;
                
                targetY = NormalizeAngle(targetY);
                currentY = NormalizeAngle(currentY);
                
                float angleDifference = Mathf.DeltaAngle(currentY, targetY);
                
                if (Mathf.Abs(angleDifference) > 0.1f)
                {
                    isAutoResetting = true;
                    
                    float rotationStep = autoResetSpeed * Time.deltaTime * Mathf.Sign(angleDifference);
                    
                    if (Mathf.Abs(rotationStep) > Mathf.Abs(angleDifference))
                    {
                        rotationStep = angleDifference;
                    }
                    
                    currentHorizontalRotation += rotationStep;
                    
                    targetTransform.rotation = Quaternion.Euler(
                        initialRotation.x,
                        currentHorizontalRotation,
                        initialRotation.z
                    );
                }
                else
                {
                    // Snap to exact position when very close
                    currentHorizontalRotation = initialRotation.y;
                    targetTransform.rotation = Quaternion.Euler(initialRotation);
                    isAutoResetting = false;
                }
            }
        }
        
        private float NormalizeAngle(float angle)
        {
            while (angle < 0f)
                angle += 360f;
            while (angle >= 360f)
                angle -= 360f;
            return angle;
        }
        
        private void InitializeTarget()
        {
            if (targetTransform == null)
            {
                Debug.LogWarning("No target transform found for mouse rotation", this);
                return;
            }
            
            initialRotation = targetTransform.eulerAngles;
            currentHorizontalRotation = initialRotation.y;
            
            if (resetOnStart)
            {
                ResetRotation();
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
            isDecelerating = false;
            isAutoResetting = false;
            rotationVelocity = 0f;
            lastMousePosition = eventData.position;
            lastInputTime = Time.time;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            lastInputTime = Time.time;
            
            if (enableSmoothRotation)
            {
                if (Mathf.Abs(rotationVelocity) > minimumVelocity)
                {
                    isDecelerating = true;
                }
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || targetTransform == null)
                return;
            
            Vector2 mouseDelta = eventData.position - lastMousePosition;
            lastMousePosition = eventData.position;
            lastInputTime = Time.time;
            
            float horizontalRotation = mouseDelta.x * rotationSpeed;
            
            if (invertHorizontal)
                horizontalRotation = -horizontalRotation;
            
            if (enableSmoothRotation)
            {
                rotationVelocity = horizontalRotation / Time.deltaTime;
            }
            
            currentHorizontalRotation += horizontalRotation;
            
            targetTransform.rotation = Quaternion.Euler(
                initialRotation.x,
                currentHorizontalRotation,
                initialRotation.z
            );
        }
        
        public void SetTarget(Transform newTarget)
        {
            targetTransform = newTarget;
            InitializeTarget();
            lastInputTime = Time.time;
        }

        private void ResetRotation()
        {
            if (targetTransform != null)
            {
                targetTransform.rotation = Quaternion.Euler(initialRotation);
                currentHorizontalRotation = initialRotation.y;
                rotationVelocity = 0f;
                isDecelerating = false;
                isAutoResetting = false;
                lastInputTime = Time.time;
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (rotationSpeed <= 0)
            {
                Debug.LogWarning("Rotation speed should be greater than 0", this);
            }
            
            if (dampingFactor <= 0 || dampingFactor >= 1)
            {
                Debug.LogWarning("Damping factor should be between 0 and 1 (recommended: 0.9-0.99)", this);
            }
            
            if (autoResetDelay < 0)
            {
                Debug.LogWarning("Auto-reset delay should be greater than or equal to 0", this);
            }
            
            if (autoResetSpeed <= 0)
            {
                Debug.LogWarning("Auto-reset speed should be greater than 0", this);
            }
        }
#endif
    }
}