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
        
        private bool _isDragging;
        private Vector2 _lastMousePosition;
        private Vector3 _initialRotation;
        private float _currentHorizontalRotation;
        
        private float _rotationVelocity;
        private bool _isDecelerating;
        private float _lastInputTime;
        private bool _isAutoResetting;
        
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
            if (!_isDragging && _isDecelerating && targetTransform != null && !_isAutoResetting)
            {
                _currentHorizontalRotation += _rotationVelocity * Time.deltaTime;
                
                _rotationVelocity *= dampingFactor;
                
                if (Mathf.Abs(_rotationVelocity) < minimumVelocity)
                {
                    _rotationVelocity = 0f;
                    _isDecelerating = false;
                }
                
                targetTransform.rotation = Quaternion.Euler(
                    _initialRotation.x,
                    _currentHorizontalRotation,
                    _initialRotation.z
                );
            }
        }
        
        private void HandleAutoReset()
        {
            if (_isDragging || _isDecelerating || targetTransform == null)
                return;
                
            if (Time.time - _lastInputTime >= autoResetDelay)
            {
                float targetY = _initialRotation.y;
                float currentY = _currentHorizontalRotation;
                
                targetY = NormalizeAngle(targetY);
                currentY = NormalizeAngle(currentY);
                
                float angleDifference = Mathf.DeltaAngle(currentY, targetY);
                
                if (Mathf.Abs(angleDifference) > 0.1f)
                {
                    _isAutoResetting = true;
                    
                    float rotationStep = autoResetSpeed * Time.deltaTime * Mathf.Sign(angleDifference);
                    
                    if (Mathf.Abs(rotationStep) > Mathf.Abs(angleDifference))
                    {
                        rotationStep = angleDifference;
                    }
                    
                    _currentHorizontalRotation += rotationStep;
                    
                    targetTransform.rotation = Quaternion.Euler(
                        _initialRotation.x,
                        _currentHorizontalRotation,
                        _initialRotation.z
                    );
                }
                else
                {
                    // Snap to exact position when very close
                    _currentHorizontalRotation = _initialRotation.y;
                    targetTransform.rotation = Quaternion.Euler(_initialRotation);
                    _isAutoResetting = false;
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
            
            _initialRotation = targetTransform.eulerAngles;
            _currentHorizontalRotation = _initialRotation.y;
            
            if (resetOnStart)
            {
                ResetRotation();
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            _isDecelerating = false;
            _isAutoResetting = false;
            _rotationVelocity = 0f;
            _lastMousePosition = eventData.position;
            _lastInputTime = Time.time;
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            _lastInputTime = Time.time;
            
            if (enableSmoothRotation)
            {
                if (Mathf.Abs(_rotationVelocity) > minimumVelocity)
                {
                    _isDecelerating = true;
                }
            }
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || targetTransform == null)
                return;
            
            Vector2 mouseDelta = eventData.position - _lastMousePosition;
            _lastMousePosition = eventData.position;
            _lastInputTime = Time.time;
            
            float horizontalRotation = mouseDelta.x * rotationSpeed;
            
            if (invertHorizontal)
                horizontalRotation = -horizontalRotation;
            
            if (enableSmoothRotation)
            {
                _rotationVelocity = horizontalRotation / Time.deltaTime;
            }
            
            _currentHorizontalRotation += horizontalRotation;
            
            targetTransform.rotation = Quaternion.Euler(
                _initialRotation.x,
                _currentHorizontalRotation,
                _initialRotation.z
            );
        }
        
        public void SetTarget(Transform newTarget)
        {
            targetTransform = newTarget;
            InitializeTarget();
            _lastInputTime = Time.time;
        }

        private void ResetRotation()
        {
            if (targetTransform != null)
            {
                targetTransform.rotation = Quaternion.Euler(_initialRotation);
                _currentHorizontalRotation = _initialRotation.y;
                _rotationVelocity = 0f;
                _isDecelerating = false;
                _isAutoResetting = false;
                _lastInputTime = Time.time;
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