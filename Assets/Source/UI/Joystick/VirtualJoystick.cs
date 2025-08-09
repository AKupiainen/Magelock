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
        [SerializeField] private Vector2 defaultPosition = new Vector2(150, 150);
        [SerializeField] private float returnAnimationDuration = 0.3f;
        [SerializeField] private float screenEdgePadding = 100f;
        [SerializeField] private float maxDragDistance = 150f; 
        [SerializeField] private bool smoothFollowing = true; 
        [SerializeField] private float followSpeed = 15f; 
        
        [Header("Visual Settings")]
        [SerializeField] private float handleSizeOnPress = 1.2f;
        [SerializeField] private float colorFadeDuration = 0.2f;
        
        [Header("Inactive State Colors")]
        [SerializeField] private Color inactiveBackgroundColor = new Color(120f/255f, 160f/255f, 180f/255f, 60f/255f);
        [SerializeField] private Color inactiveHandleColor = new Color(200f/255f, 200f/255f, 200f/255f, 80f/255f);
        
        [Header("Active State Colors")]
        [SerializeField] private Color activeBackgroundColor = new Color(0f, 200f/255f, 255f/255f, 120f/255f);
        [SerializeField] private Color activeHandleColor = new Color(255f/255f, 255f/255f, 255f/255f, 220f/255f);
        
        [Header("Effects")]
        [SerializeField] private bool enableGlowEffect = true;
        [SerializeField] private bool enablePulseAnimation = true;
        [SerializeField] private float pulseSpeed = 1f;
        [SerializeField] private float pulseIntensity = 0.1f;
        
        private Vector2 inputVector;
        private Vector2 lastSentInput;
        private Image backgroundImage;
        private Image handleImage;
        private bool isActive = false;
        private Camera cam;
        private float lastEventTime;
        private float interactionStartTime;
        private RectTransform canvasRect;
        private Shadow glowEffect;
        private Vector2 touchStartPosition; 
        private Vector2 joystickStartPosition; 
        
        private Sequence pulseSequence;
        private Tweener returnTween;
        private Tweener bgColorTween;
        private Tweener handleColorTween;
        private Tweener handleScaleTween;
        
        public Vector2 InputVector => inputVector;
        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;
        public bool IsActive => isActive;
        
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
            
            if (enablePulseAnimation && !isActive)
            {
                StartPulseAnimation();
            }
        }
        
        private void OnDisable()
        {
            EventsBus.Unsubscribe<InputModeChangeEvent>(OnInputModeChanged);
            EventsBus.Unsubscribe<LocalPlayerStatusEvent>(OnLocalPlayerStatusChanged);
            
            DOTween.Kill(this);
            
            if (isActive)
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
            if (canvas != null)
            {
                canvasRect = canvas.GetComponent<RectTransform>();
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    cam = canvas.worldCamera;
            }
            
            if (joystickBackground != null)
                defaultPosition = joystickBackground.anchoredPosition;
        }
        
        private void SetupEffects()
        {
            if (enableGlowEffect && joystickHandle != null)
            {
                glowEffect = joystickHandle.GetComponent<Shadow>();
                if (glowEffect == null)
                {
                    glowEffect = joystickHandle.gameObject.AddComponent<Shadow>();
                    glowEffect.effectColor = new Color(1f, 1f, 1f, 0.5f);
                    glowEffect.effectDistance = new Vector2(0, 0);
                    glowEffect.useGraphicAlpha = true;
                }
                glowEffect.enabled = false;
            }
        }
        
        private void SetupVisuals()
        {
            if (backgroundImage != null)
                backgroundImage.color = inactiveBackgroundColor;
            if (handleImage != null)
                handleImage.color = inactiveHandleColor;
        }
        
        private void Update()
        {
            if (isActive && Time.time - lastEventTime >= eventSendRate)
            {
                SendMovementEvent(inputVector);
                lastEventTime = Time.time;
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            returnTween?.Kill();
            
            isActive = true;
            interactionStartTime = Time.time;
            
            touchStartPosition = eventData.position;
            
            RectTransform parentRect = joystickBackground.parent as RectTransform ?? canvasRect;
            Vector2 localTouchPos;
            Camera eventCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventCamera,
                out localTouchPos);
            
            Vector2 constrainedPos = ConstrainToScreen(localTouchPos);
            joystickBackground.anchoredPosition = constrainedPos;
            
            joystickStartPosition = constrainedPos;
            
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }
            
            inputVector = Vector2.zero;
            
            AnimateToActiveState();
            
            EventsBus.Trigger(new JoystickStartEvent(inputVector));
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            float duration = Time.time - interactionStartTime;
            Vector2 finalInput = inputVector;
            
            isActive = false;
            inputVector = Vector2.zero;
            
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
            RectTransform parentRect = joystickBackground.parent as RectTransform ?? canvasRect;
            Camera eventCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
            
            Vector2 currentTouchLocalPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                eventData.position,
                eventCamera,
                out currentTouchLocalPos);
            
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
                    Vector2 offsetFromStart = targetJoystickPos - joystickStartPosition;
                    if (offsetFromStart.magnitude > maxDragDistance)
                    {
                        targetJoystickPos = joystickStartPosition + (offsetFromStart.normalized * maxDragDistance);
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
                
                inputVector = direction;
                
                if (joystickHandle != null)
                {
                    Vector2 handlePos = direction * joystickRadius * joystickRange / 100f;
                    joystickHandle.anchoredPosition = handlePos;
                }
            }
            else
            {
                inputVector = joystickToTouch / joystickRadius;
                
                // Apply dead zone
                if (inputVector.magnitude < deadZone)
                {
                    inputVector = Vector2.zero;
                }
                else
                {
                    float magnitude = (inputVector.magnitude - deadZone) / (1 - deadZone);
                    inputVector = inputVector.normalized * magnitude;
                }
                
                if (joystickHandle != null)
                {
                    Vector2 handlePos = joystickToTouch * joystickRange / 100f;
                    handlePos = Vector2.ClampMagnitude(handlePos, joystickRadius * joystickRange / 100f);
                    joystickHandle.anchoredPosition = handlePos;
                }
            }
            
            if (Vector2.Distance(inputVector, lastSentInput) > 0.01f)
            {
                SendMovementEvent(inputVector);
            }
        }
        
        private Vector2 ConstrainToScreen(Vector2 position)
        {
            RectTransform parentRect = joystickBackground.parent as RectTransform ?? canvasRect;
            
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
            pulseSequence?.Kill();
            bgColorTween?.Kill();
            handleColorTween?.Kill();
            handleScaleTween?.Kill();
            
            if (backgroundImage != null)
            {
                bgColorTween = backgroundImage.DOColor(activeBackgroundColor, colorFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
            }
                
            if (handleImage != null)
            {
                handleColorTween = handleImage.DOColor(activeHandleColor, colorFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
                    
                handleScaleTween = joystickHandle.DOScale(handleSizeOnPress, 0.15f)
                    .SetEase(Ease.OutBack)
                    .SetId(this);
            }
            
            if (glowEffect != null)
                glowEffect.enabled = true;
        }
        
        private void AnimateToInactiveState()
        {
            bgColorTween?.Kill();
            handleColorTween?.Kill();
            handleScaleTween?.Kill();
            
            if (backgroundImage != null)
            {
                bgColorTween = backgroundImage.DOColor(inactiveBackgroundColor, colorFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
            }
                
            if (handleImage != null)
            {
                handleColorTween = handleImage.DOColor(inactiveHandleColor, colorFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
                    
                handleScaleTween = joystickHandle.DOScale(1f, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
            }
            
            if (glowEffect != null)
                glowEffect.enabled = false;
            
            if (enablePulseAnimation)
            {
                StartPulseAnimation();
            }
        }
        
        private void AnimateToDefaultPosition()
        {
            returnTween?.Kill();
            returnTween = joystickBackground.DOAnchorPos(defaultPosition, returnAnimationDuration)
                .SetEase(Ease.OutQuad)
                .SetId(this);
        }
        
        private void StartPulseAnimation()
        {
            if (handleImage == null || !enablePulseAnimation) return;
            
            pulseSequence?.Kill();
            
            pulseSequence = DOTween.Sequence()
                .SetId(this)
                .SetLoops(-1) 
                .Append(handleImage.DOFade(inactiveHandleColor.a * (1f + pulseIntensity), pulseSpeed)
                    .SetEase(Ease.InOutSine))
                .Append(handleImage.DOFade(inactiveHandleColor.a, pulseSpeed)
                    .SetEase(Ease.InOutSine));
        }
        
        private void SendMovementEvent(Vector2 input)
        {
            EventsBus.Trigger(new MovementInputEvent(input, InputSource.VirtualJoystick));
            lastSentInput = input;
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
        
        public void ResetJoystick()
        {
            inputVector = Vector2.zero;
            
            if (joystickHandle != null)
            {
                joystickHandle.DOAnchorPos(Vector2.zero, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetId(this);
            }
            
            SendMovementEvent(Vector2.zero);
        }
        
        public void SetDefaultPosition(Vector2 position)
        {
            defaultPosition = position;
            if (!isActive)
            {
                joystickBackground.anchoredPosition = defaultPosition;
            }
        }
    }
}