using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MageLock.Events;
using MageLock.Gameplay.Events;
using System.Collections;

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
        [SerializeField] private bool resetHandlePosition = true;
        [SerializeField] private bool sendContinuousEvents = true;
        [SerializeField] private float eventSendRate = 0.02f; // Send events every 20ms
        
        [Header("Dynamic Positioning")]
        [SerializeField] private bool dynamicPositioning = true;
        [SerializeField] private DynamicMode dynamicMode = DynamicMode.MoveOnTouch;
        [SerializeField] private bool returnToDefaultPosition = true;
        [SerializeField] private Vector2 defaultPosition = new Vector2(150, 150);
        [SerializeField] private float maxDistanceFromDefault = 300f;
        [SerializeField] private bool constrainToScreen = true;
        [SerializeField] private float screenEdgePadding = 100f;
        
        public enum DynamicMode
        {
            MoveOnTouch,        // Joystick moves to exact touch position
            MoveWithinRange,    // Only moves if touch is outside current range
            FixedPosition       // Never moves (traditional joystick)
        }
        
        [Header("Visual - Brawl Stars Style")]
        [SerializeField] private bool useBrawlStarsStyle = true;
        [SerializeField] private float handleSizeOnPress = 1.2f;
        [SerializeField] private float fadeTransitionSpeed = 8f;
        
        // Brawl Stars Colors
        [Header("Inactive State Colors")]
        [SerializeField] private Color inactiveBackgroundColor = new Color(120f/255f, 160f/255f, 180f/255f, 60f/255f);
        [SerializeField] private Color inactiveHandleColor = new Color(200f/255f, 200f/255f, 200f/255f, 80f/255f);
        
        [Header("Active State Colors")]
        [SerializeField] private Color activeBackgroundColor = new Color(0f, 200f/255f, 255f/255f, 120f/255f);
        [SerializeField] private Color activeHandleColor = new Color(255f/255f, 255f/255f, 255f/255f, 220f/255f);
        
        [Header("Effects")]
        [SerializeField] private bool enableGlowEffect = true;
        [SerializeField] private bool enablePulseAnimation = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseIntensity = 0.1f;
        
        private Vector2 inputVector;
        private Vector2 lastSentInput;
        private Image backgroundImage;
        private Image handleImage;
        private bool isActive = false;
        private Vector2 joystickCenter;
        private Camera cam;
        private float lastEventTime;
        private float interactionStartTime;
        private Vector2 touchStartPosition;
        private RectTransform canvasRect;
        private Coroutine fadeCoroutine;
        private Coroutine pulseCoroutine;
        private Shadow glowEffect;
        
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
            // Subscribe to input mode changes
            EventsBus.Subscribe<InputModeChangeEvent>(OnInputModeChanged);
            EventsBus.Subscribe<LocalPlayerStatusEvent>(OnLocalPlayerStatusChanged);
            
            // Start pulse animation if enabled
            if (enablePulseAnimation && !isActive)
            {
                pulseCoroutine = StartCoroutine(PulseAnimation());
            }
        }
        
        private void OnDisable()
        {
            // Clean up subscriptions
            EventsBus.Unsubscribe<InputModeChangeEvent>(OnInputModeChanged);
            EventsBus.Unsubscribe<LocalPlayerStatusEvent>(OnLocalPlayerStatusChanged);
            
            // Stop coroutines
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            
            // Send final zero input if we were active
            if (isActive)
            {
                SendMovementEvent(Vector2.zero);
            }
        }
        
        private void InitializeComponents()
        {
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();
                
            if (joystickBackground == null)
                joystickBackground = GetComponent<RectTransform>();
                
            if (joystickHandle == null && transform.childCount > 0)
                joystickHandle = transform.GetChild(0).GetComponent<RectTransform>();
                
            backgroundImage = joystickBackground.GetComponent<Image>();
            if (joystickHandle != null)
                handleImage = joystickHandle.GetComponent<Image>();
                
            // Get canvas rect for bounds checking
            if (canvas != null)
            {
                canvasRect = canvas.GetComponent<RectTransform>();
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    cam = canvas.worldCamera;
            }
            
            // Store default position
            if (joystickBackground != null)
                defaultPosition = joystickBackground.anchoredPosition;
        }
        
        private void SetupEffects()
        {
            // Add glow effect (shadow component used as glow)
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
                glowEffect.enabled = false; // Start disabled
            }
        }
        
        private void SetupVisuals()
        {
            // Set initial colors to inactive state
            if (useBrawlStarsStyle)
            {
                SetJoystickColors(inactiveBackgroundColor, inactiveHandleColor);
            }
        }
        
        private void Start()
        {
            joystickCenter = RectTransformUtility.WorldToScreenPoint(cam, joystickBackground.position);
        }
        
        private void Update()
        {
            // Send continuous events while active if enabled
            if (isActive && sendContinuousEvents)
            {
                if (Time.time - lastEventTime >= eventSendRate)
                {
                    SendMovementEvent(inputVector);
                    lastEventTime = Time.time;
                }
            }
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            isActive = true;
            interactionStartTime = Time.time;
            touchStartPosition = eventData.position;
            
            // Dynamic positioning based on mode
            if (dynamicPositioning && dynamicMode != DynamicMode.FixedPosition)
            {
                bool shouldMove = false;
                
                if (dynamicMode == DynamicMode.MoveOnTouch)
                {
                    shouldMove = true;
                }
                else if (dynamicMode == DynamicMode.MoveWithinRange)
                {
                    // Check if touch is outside current joystick area
                    Vector2 localPoint;
                    Camera eventCamera = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : cam;
                    
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        joystickBackground,
                        eventData.position,
                        eventCamera,
                        out localPoint))
                    {
                        // Check if touch is outside joystick bounds
                        float distance = localPoint.magnitude;
                        float joystickRadius = joystickBackground.sizeDelta.x / 2;
                        shouldMove = distance > joystickRadius;
                    }
                }
                
                if (shouldMove)
                {
                    MoveJoystickToPosition(eventData.position);
                    
                    // Reset handle to center after moving joystick
                    if (joystickHandle != null)
                    {
                        joystickHandle.anchoredPosition = Vector2.zero;
                    }
                    
                    // Start with zero input since we're at center
                    inputVector = Vector2.zero;
                }
                else
                {
                    // Process input normally if not moving joystick
                    OnDrag(eventData);
                }
            }
            else
            {
                // Process input normally for fixed position
                OnDrag(eventData);
            }
            
            // Visual feedback
            if (useBrawlStarsStyle)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeToColors(activeBackgroundColor, activeHandleColor));
            }
            
            // Enable glow effect
            if (glowEffect != null)
                glowEffect.enabled = true;
            
            // Stop pulse animation
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
            
            // Scale up handle
            if (joystickHandle != null)
                joystickHandle.localScale = Vector3.one * handleSizeOnPress;
            
            // Fire joystick start event
            EventsBus.Trigger(new JoystickStartEvent(inputVector));
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            // Calculate interaction duration
            float duration = Time.time - interactionStartTime;
            Vector2 finalInput = inputVector;
            
            isActive = false;
            inputVector = Vector2.zero;
            
            // Send zero input event
            SendMovementEvent(Vector2.zero);
            
            // Fire joystick end event
            EventsBus.Trigger(new JoystickEndEvent(finalInput, duration));
            
            // Return to default position if enabled
            if (dynamicPositioning && returnToDefaultPosition)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(ReturnToDefaultPosition());
            }
            
            // Visual feedback
            if (useBrawlStarsStyle)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeToColors(inactiveBackgroundColor, inactiveHandleColor));
            }
            
            // Disable glow effect
            if (glowEffect != null)
                glowEffect.enabled = false;
            
            // Restart pulse animation
            if (enablePulseAnimation)
            {
                pulseCoroutine = StartCoroutine(PulseAnimation());
            }
            
            if (resetHandlePosition && joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }
            
            // Reset handle scale with smooth animation
            if (joystickHandle != null)
                StartCoroutine(SmoothScale(joystickHandle, Vector3.one, 0.15f));
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            Vector2 position;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground,
                eventData.position,
                eventData.pressEventCamera,
                out position))
            {
                // Calculate input relative to joystick size
                position.x = (position.x / joystickBackground.sizeDelta.x) * 2;
                position.y = (position.y / joystickBackground.sizeDelta.y) * 2;
                
                inputVector = new Vector2(position.x, position.y);
                inputVector = (inputVector.magnitude > 1.0f) ? inputVector.normalized : inputVector;
                
                // Apply dead zone
                if (inputVector.magnitude < deadZone)
                {
                    inputVector = Vector2.zero;
                }
                else
                {
                    // Remap the input vector to exclude dead zone
                    float magnitude = (inputVector.magnitude - deadZone) / (1 - deadZone);
                    inputVector = inputVector.normalized * magnitude;
                }
                
                // Move the handle
                if (joystickHandle != null)
                {
                    joystickHandle.anchoredPosition = new Vector2(
                        inputVector.x * (joystickBackground.sizeDelta.x / 2) * joystickRange / 100f,
                        inputVector.y * (joystickBackground.sizeDelta.y / 2) * joystickRange / 100f);
                }
                
                // Send event if input changed significantly
                if (!sendContinuousEvents || Vector2.Distance(inputVector, lastSentInput) > 0.01f)
                {
                    SendMovementEvent(inputVector);
                }
            }
        }
        
        private void MoveJoystickToPosition(Vector2 screenPosition)
        {
            // Get the parent rect (usually Canvas) for proper positioning
            RectTransform parentRect = joystickBackground.parent as RectTransform;
            if (parentRect == null) parentRect = canvasRect;
            
            Vector2 localPoint;
            
            // Convert screen point to local point in parent's space
            Camera eventCamera = cam;
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                eventCamera = null;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                screenPosition,
                eventCamera,
                out localPoint))
            {
                Vector2 targetPosition = localPoint;
                
                // Constrain distance from default position if needed
                if (maxDistanceFromDefault > 0 && !returnToDefaultPosition)
                {
                    Vector2 offset = targetPosition - defaultPosition;
                    if (offset.magnitude > maxDistanceFromDefault)
                    {
                        offset = offset.normalized * maxDistanceFromDefault;
                        targetPosition = defaultPosition + offset;
                    }
                }
                
                // Constrain to screen bounds if needed
                if (constrainToScreen)
                {
                    float halfWidth = joystickBackground.sizeDelta.x / 2;
                    float halfHeight = joystickBackground.sizeDelta.y / 2;
                    
                    Rect parentBounds = parentRect.rect;
                    
                    float minX = parentBounds.xMin + halfWidth + screenEdgePadding;
                    float maxX = parentBounds.xMax - halfWidth - screenEdgePadding;
                    float minY = parentBounds.yMin + halfHeight + screenEdgePadding;
                    float maxY = parentBounds.yMax - halfHeight - screenEdgePadding;
                    
                    targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                    targetPosition.y = Mathf.Clamp(targetPosition.y, minY, maxY);
                }
                
                // Apply the new position
                joystickBackground.anchoredPosition = targetPosition;
                
                // Force update the joystick center immediately
                Canvas.ForceUpdateCanvases();
                joystickCenter = RectTransformUtility.WorldToScreenPoint(eventCamera, joystickBackground.position);
            }
        }
        
        private IEnumerator ReturnToDefaultPosition()
        {
            Vector2 startPos = joystickBackground.anchoredPosition;
            float elapsed = 0f;
            float duration = 0.3f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t);
                
                joystickBackground.anchoredPosition = Vector2.Lerp(startPos, defaultPosition, t);
                yield return null;
            }
            
            joystickBackground.anchoredPosition = defaultPosition;
            joystickCenter = RectTransformUtility.WorldToScreenPoint(cam, joystickBackground.position);
        }
        
        private IEnumerator FadeToColors(Color targetBgColor, Color targetHandleColor)
        {
            Color startBgColor = backgroundImage.color;
            Color startHandleColor = handleImage != null ? handleImage.color : targetHandleColor;
            
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime * fadeTransitionSpeed;
                float t = Mathf.Clamp01(elapsed);
                
                if (backgroundImage != null)
                    backgroundImage.color = Color.Lerp(startBgColor, targetBgColor, t);
                    
                if (handleImage != null)
                    handleImage.color = Color.Lerp(startHandleColor, targetHandleColor, t);
                    
                yield return null;
            }
        }
        
        private IEnumerator SmoothScale(RectTransform target, Vector3 targetScale, float duration)
        {
            Vector3 startScale = target.localScale;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t);
                
                target.localScale = Vector3.Lerp(startScale, targetScale, t);
                yield return null;
            }
            
            target.localScale = targetScale;
        }
        
        private IEnumerator PulseAnimation()
        {
            float time = 0f;
            Color baseColor = inactiveHandleColor;
            
            while (!isActive)
            {
                time += Time.deltaTime * pulseSpeed;
                float pulse = (Mathf.Sin(time) + 1f) / 2f; // 0 to 1
                
                if (handleImage != null)
                {
                    Color pulsedColor = baseColor;
                    pulsedColor.a = baseColor.a * (1f + pulse * pulseIntensity);
                    handleImage.color = pulsedColor;
                }
                
                yield return null;
            }
        }
        
        private void SetJoystickColors(Color bgColor, Color handleColor)
        {
            if (backgroundImage != null)
                backgroundImage.color = bgColor;
            if (handleImage != null)
                handleImage.color = handleColor;
        }
        
        private void SendMovementEvent(Vector2 input)
        {
            EventsBus.Trigger(new MovementInputEvent(input, InputSource.VirtualJoystick));
            lastSentInput = input;
        }
        
        private void OnInputModeChanged(InputModeChangeEvent e)
        {
            // Show/hide joystick based on new input mode
            bool shouldShow = (e.NewMode == InputMode.VirtualJoystick || e.NewMode == InputMode.Both);
            
            if (shouldShow != gameObject.activeSelf)
            {
                gameObject.SetActive(shouldShow);
                Debug.Log($"Virtual Joystick visibility changed to: {shouldShow} (Mode: {e.NewMode})");
            }
        }
        
        private void OnLocalPlayerStatusChanged(LocalPlayerStatusEvent e)
        {
            // Only show joystick for local player
            if (!e.IsLocalPlayer && e.Player != null)
            {
                // Check if this joystick belongs to the player
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
                joystickHandle.anchoredPosition = Vector2.zero;
            SendMovementEvent(Vector2.zero);
        }
        
        public void SetInputVector(Vector2 input)
        {
            inputVector = Vector2.ClampMagnitude(input, 1f);
            
            if (joystickHandle != null)
            {
                joystickHandle.anchoredPosition = new Vector2(
                    inputVector.x * (joystickBackground.sizeDelta.x / 2) * joystickRange / 100f,
                    inputVector.y * (joystickBackground.sizeDelta.y / 2) * joystickRange / 100f);
            }
            
            SendMovementEvent(inputVector);
        }
        
        public void SetDynamicPositioning(bool enabled)
        {
            dynamicPositioning = enabled;
        }
        
        public void SetDefaultPosition(Vector2 position)
        {
            defaultPosition = position;
            if (!isActive && returnToDefaultPosition)
            {
                joystickBackground.anchoredPosition = defaultPosition;
            }
        }
    }
}