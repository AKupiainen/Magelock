using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MageLock.Events;
using MageLock.Gameplay.Events;
using DG.Tweening;

namespace MageLock.UI
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("Joystick Components")]
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform joystickHandle;
        [SerializeField] private Canvas canvas;
        
        [Header("Settings")]
        [SerializeField] private float joystickRange = 100f;
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private float eventSendRate = 0.02f;
        
        [Header("Dynamic Positioning")]
        [SerializeField] private Vector2 defaultPosition = new(150, 150);
        [SerializeField] private float returnAnimationDuration = 0.3f;
        [SerializeField] private float screenEdgePadding = 100f;
        [SerializeField] private float maxDragDistance = 150f; 
        [SerializeField] private bool smoothFollowing = true; 
        [SerializeField] private float followSpeed = 15f; 
        
        [Header("Visual Settings")]
        [SerializeField] private float handleSizeOnPress = 1.2f;
        [SerializeField] private float colorFadeDuration = 0.2f;
        
        [Header("Inactive State Colors")]
        [SerializeField] private Color inactiveBackgroundColor = new(120f/255f, 160f/255f, 180f/255f, 60f/255f);
        [SerializeField] private Color inactiveHandleColor = new(200f/255f, 200f/255f, 200f/255f, 80f/255f);
        
        [Header("Active State Colors")]
        [SerializeField] private Color activeBackgroundColor = new(0f, 200f/255f, 255f/255f, 120f/255f);
        [SerializeField] private Color activeHandleColor = new(255f/255f, 255f/255f, 255f/255f, 220f/255f);
        
        [Header("Effects")]
        [SerializeField] private bool enableGlowEffect = true;
        [SerializeField] private bool enablePulseAnimation = true;
        [SerializeField] private float pulseSpeed = 1f;
        [SerializeField] private float pulseIntensity = 0.1f;
        
        private Vector2 _inputVector;
        private Vector2 _lastSentInput;
        private Image _backgroundImage;
        private Image _handleImage;
        private bool _isActive;
        private Camera _cam;
        private float _lastEventTime;
        private float _interactionStartTime;
        private RectTransform _canvasRect;
        private Shadow _glowEffect;
        private Vector2 _joystickStartPosition; 
        
        private Sequence _pulseSequence;
        private Tweener _returnTween;
        private Tweener _bgColorTween;
        private Tweener _handleColorTween;
        private Tweener _handleScaleTween;
        
        private void Awake()
        {
            InitializeComponents();
            SetupVisuals();
            SetupEffects();
        }
        
        private void OnEnable()
        {
            EventsBus.Subscribe<InputModeChangeEvent>(OnInputModeChanged);
            EventsBus.Subscribe<LocalPlayerStatusEvent>(OnLocalPlayerStatusChanged);
            
            if (enablePulseAnimation && !_isActive)
            {
                StartPulseAnimation();
            }
        }
        
        private void OnDisable()
        {
            EventsBus.Unsubscribe<InputModeChangeEvent>(OnInputModeChanged);
            EventsBus.Unsubscribe<LocalPlayerStatusEvent>(OnLocalPlayerStatusChanged);
            
            DOTween.Kill(this);
            
            if (_isActive)
            {
                SendMovementEvent(Vector2.zero);
            }
        }
        
        private void OnDestroy()
        {
            DOTween.Kill(this);
        }
        
        private void InitializeComponents()
        {
            if (joystickBackground != null)
            {
                _backgroundImage = joystickBackground.GetComponent<Image>();
                if (_backgroundImage == null)
                {
                    _backgroundImage = joystickBackground.gameObject.AddComponent<Image>();
                }
            }
            
            if (joystickHandle != null)
            {
                _handleImage = joystickHandle.GetComponent<Image>();
                if (_handleImage == null)
                {
                    _handleImage = joystickHandle.gameObject.AddComponent<Image>();
                }
            }
            
            if (canvas != null)
            {
                _canvasRect = canvas.GetComponent<RectTransform>();
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    _cam = canvas.worldCamera;
            }
            
            if (joystickBackground != null)
                defaultPosition = joystickBackground.anchoredPosition;
        }
        
        private void SetupEffects()
        {
            if (enableGlowEffect && joystickHandle != null)
            {
                _glowEffect = joystickHandle.GetComponent<Shadow>();
                if (_glowEffect == null)
                {
                    _glowEffect = joystickHandle.gameObject.AddComponent<Shadow>();
                    _glowEffect.effectColor = new Color(1f, 1f, 1f, 0.5f);
                    _glowEffect.effectDistance = new Vector2(0, 0);
                    _glowEffect.useGraphicAlpha = true;
                }
                _glowEffect.enabled = false;
            }
        }
        
        private void SetupVisuals()
        {
            if (_backgroundImage != null)
                _backgroundImage.color = inactiveBackgroundColor;
            if (_handleImage != null)
                _handleImage.color = inactiveHandleColor;
        }
        
        private void Update()
        {
            if (_isActive && Time.time - _lastEventTime >= eventSendRate)
            {
                SendMovementEvent(_inputVector);
                _lastEventTime = Time.time;
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            _returnTween?.Kill();
            
            _isActive = true;
            _interactionStartTime = Time.time;

            RectTransform parentRect = joystickBackground.parent as RectTransform ?? _canvasRect;
            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _cam;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventCamera,
                out var localTouchPos);
            
            Vector2 constrainedPos = ConstrainToScreen(localTouchPos);
            joystickBackground.anchoredPosition = constrainedPos;
            
            _joystickStartPosition = constrainedPos;
            
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }
            
            _inputVector = Vector2.zero;
            
            AnimateToActiveState();
            
            EventsBus.Trigger(new JoystickStartEvent(_inputVector));
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            float duration = Time.time - _interactionStartTime;
            Vector2 finalInput = _inputVector;
            
            _isActive = false;
            _inputVector = Vector2.zero;
            
            if (joystickHandle != null)
            {
                joystickHandle.DOAnchorPos(Vector2.zero, 0.15f).SetEase(Ease.OutQuad).SetId(this);
            }
            
            SendMovementEvent(Vector2.zero);
            
            EventsBus.Trigger(new JoystickEndEvent(finalInput, duration));
            
            AnimateToDefaultPosition();
            AnimateToInactiveState();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            RectTransform parentRect = joystickBackground.parent as RectTransform ?? _canvasRect;
            Camera eventCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _cam;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventCamera,
                out var currentTouchLocalPos);
            
            Vector2 currentJoystickPos = joystickBackground.anchoredPosition;
            
            Vector2 joystickToTouch = currentTouchLocalPos - currentJoystickPos;
            float distanceToTouch = joystickToTouch.magnitude;
            float joystickRadius = joystickBackground.sizeDelta.x / 2f;
            
            if (distanceToTouch > joystickRadius)
            {
                Vector2 direction = joystickToTouch.normalized;
                Vector2 targetJoystickPos = currentTouchLocalPos - (direction * joystickRadius);
                
                if (maxDragDistance > 0)
                {
                    Vector2 offsetFromStart = targetJoystickPos - _joystickStartPosition;
                    if (offsetFromStart.magnitude > maxDragDistance)
                    {
                        targetJoystickPos = _joystickStartPosition + (offsetFromStart.normalized * maxDragDistance);
                    }
                }
                
                targetJoystickPos = ConstrainToScreen(targetJoystickPos);
                
                if (smoothFollowing)
                {
                    Vector2 newPos = Vector2.Lerp(currentJoystickPos, targetJoystickPos, followSpeed * Time.deltaTime);
                    joystickBackground.anchoredPosition = newPos;
                    
                    joystickToTouch = currentTouchLocalPos - newPos;
                    direction = joystickToTouch.normalized;
                }
                else
                {
                    joystickBackground.anchoredPosition = targetJoystickPos;
                }
                
                _inputVector = direction;
                
                if (joystickHandle != null)
                {
                    Vector2 handlePos = direction * joystickRadius * joystickRange / 100f;
                    joystickHandle.anchoredPosition = handlePos;
                }
            }
            else
            {
                _inputVector = joystickToTouch / joystickRadius;
                
                if (_inputVector.magnitude < deadZone)
                {
                    _inputVector = Vector2.zero;
                }
                else
                {
                    float magnitude = (_inputVector.magnitude - deadZone) / (1 - deadZone);
                    _inputVector = _inputVector.normalized * magnitude;
                }
                
                if (joystickHandle != null)
                {
                    Vector2 handlePos = joystickToTouch * joystickRange / 100f;
                    handlePos = Vector2.ClampMagnitude(handlePos, joystickRadius * joystickRange / 100f);
                    joystickHandle.anchoredPosition = handlePos;
                }
            }
            
            if (Vector2.Distance(_inputVector, _lastSentInput) > 0.01f)
            {
                SendMovementEvent(_inputVector);
            }
        }
        
        private Vector2 ConstrainToScreen(Vector2 position)
        {
            RectTransform parentRect = joystickBackground.parent as RectTransform ?? _canvasRect;
            
            if (parentRect != null)
            {
                float halfWidth = joystickBackground.sizeDelta.x / 2;
                float halfHeight = joystickBackground.sizeDelta.y / 2;
                
                Rect parentBounds = parentRect.rect;
                
                float minX = parentBounds.xMin + halfWidth + screenEdgePadding;
                float maxX = parentBounds.xMax - halfWidth - screenEdgePadding;
                float minY = parentBounds.yMin + halfHeight + screenEdgePadding;
                float maxY = parentBounds.yMax - halfHeight - screenEdgePadding;
                
                position.x = Mathf.Clamp(position.x, minX, maxX);
                position.y = Mathf.Clamp(position.y, minY, maxY);
            }
            
            return position;
        }
        
        private void AnimateToActiveState()
        {
            _pulseSequence?.Kill();
            _bgColorTween?.Kill();
            _handleColorTween?.Kill();
            _handleScaleTween?.Kill();
            
            if (_backgroundImage != null)
            {
                _bgColorTween = _backgroundImage.DOColor(activeBackgroundColor, colorFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
            }
                
            if (_handleImage != null)
            {
                _handleColorTween = _handleImage.DOColor(activeHandleColor, colorFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
                    
                _handleScaleTween = joystickHandle.DOScale(handleSizeOnPress, 0.15f)
                    .SetEase(Ease.OutBack)
                    .SetId(this);
            }
            
            if (_glowEffect != null)
                _glowEffect.enabled = true;
        }
        
        private void AnimateToInactiveState()
        {
            _bgColorTween?.Kill();
            _handleColorTween?.Kill();
            _handleScaleTween?.Kill();
            
            if (_backgroundImage != null)
            {
                _bgColorTween = _backgroundImage.DOColor(inactiveBackgroundColor, colorFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
            }
                
            if (_handleImage != null)
            {
                _handleColorTween = _handleImage.DOColor(inactiveHandleColor, colorFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
                    
                _handleScaleTween = joystickHandle.DOScale(1f, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
            }
            
            if (_glowEffect != null)
                _glowEffect.enabled = false;
            
            if (enablePulseAnimation)
            {
                StartPulseAnimation();
            }
        }
        
        private void AnimateToDefaultPosition()
        {
            _returnTween?.Kill();
            _returnTween = joystickBackground.DOAnchorPos(defaultPosition, returnAnimationDuration)
                .SetEase(Ease.OutQuad)
                .SetId(this);
        }
        
        private void StartPulseAnimation()
        {
            if (_handleImage == null || !enablePulseAnimation) return;
            
            _pulseSequence?.Kill();
            
            _pulseSequence = DOTween.Sequence()
                .SetId(this)
                .SetLoops(-1) 
                .Append(_handleImage.DOFade(inactiveHandleColor.a * (1f + pulseIntensity), pulseSpeed)
                    .SetEase(Ease.InOutSine))
                .Append(_handleImage.DOFade(inactiveHandleColor.a, pulseSpeed)
                    .SetEase(Ease.InOutSine));
        }
        
        private void SendMovementEvent(Vector2 input)
        {
            EventsBus.Trigger(new MovementInputEvent(input, InputSource.VirtualJoystick));
            _lastSentInput = input;
        }
        
        private void OnInputModeChanged(InputModeChangeEvent e)
        {
            bool shouldShow = (e.NewMode == InputMode.VirtualJoystick || e.NewMode == InputMode.Both);
            
            if (shouldShow != gameObject.activeSelf)
            {
                gameObject.SetActive(shouldShow);
                Debug.Log($"Virtual Joystick visibility changed to: {shouldShow} (Mode: {e.NewMode})");
            }
        }
        
        private void OnLocalPlayerStatusChanged(LocalPlayerStatusEvent e)
        {
            if (!e.IsLocalPlayer && e.Player != null)
            {
                Transform parent = transform.parent;
                while (parent != null)
                {
                    if (parent.gameObject == e.Player)
                    {
                        gameObject.SetActive(false);
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }
    }
}