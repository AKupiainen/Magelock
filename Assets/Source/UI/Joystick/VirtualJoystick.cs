using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MageLock.Events;
using MageLock.Gameplay.Events;

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
        [SerializeField] private bool hideWhenInactive = false;
        [SerializeField] private bool resetHandlePosition = true;
        [SerializeField] private bool sendContinuousEvents = true;
        [SerializeField] private float eventSendRate = 0.02f; // Send events every 20ms
        
        [Header("Visual")]
        [SerializeField] private float handleSizeOnPress = 1.2f;
        [SerializeField] private Color activeColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color inactiveColor = new Color(1f, 1f, 1f, 0.5f);
        
        private Vector2 inputVector;
        private Vector2 lastSentInput;
        private Image backgroundImage;
        private Image handleImage;
        private bool isActive = false;
        private Vector2 joystickCenter;
        private Camera cam;
        private float lastEventTime;
        private float interactionStartTime;
        
        public Vector2 InputVector => inputVector;
        public float Horizontal => inputVector.x;
        public float Vertical => inputVector.y;
        public bool IsActive => isActive;
        
        private void Awake()
        {
            InitializeComponents();
            SetupVisuals();
        }
        
        private void OnEnable()
        {
            // Subscribe to input mode changes
            EventsBus.Subscribe<InputModeChangeEvent>(OnInputModeChanged);
            EventsBus.Subscribe<LocalPlayerStatusEvent>(OnLocalPlayerStatusChanged);
        }
        
        private void OnDisable()
        {
            // Clean up subscriptions
            EventsBus.Unsubscribe<InputModeChangeEvent>(OnInputModeChanged);
            EventsBus.Unsubscribe<LocalPlayerStatusEvent>(OnLocalPlayerStatusChanged);
            
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
                
            // Get the camera for the canvas
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
                    cam = canvas.worldCamera;
            }
        }
        
        private void SetupVisuals()
        {
            if (hideWhenInactive)
            {
                SetJoystickVisibility(false);
            }
            else
            {
                SetJoystickAlpha(inactiveColor);
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
            
            if (hideWhenInactive)
                SetJoystickVisibility(true);
            else
                SetJoystickAlpha(activeColor);
                
            // Scale up handle
            if (joystickHandle != null)
                joystickHandle.localScale = Vector3.one * handleSizeOnPress;
            
            // Process initial input
            OnDrag(eventData);
            
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
            
            if (resetHandlePosition && joystickHandle != null)
            {
                joystickHandle.anchoredPosition = Vector2.zero;
            }
            
            if (hideWhenInactive)
                SetJoystickVisibility(false);
            else
                SetJoystickAlpha(inactiveColor);
                
            // Reset handle scale
            if (joystickHandle != null)
                joystickHandle.localScale = Vector3.one;
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
        
        private void SetJoystickVisibility(bool visible)
        {
            if (backgroundImage != null)
                backgroundImage.enabled = visible;
            if (handleImage != null)
                handleImage.enabled = visible;
        }
        
        private void SetJoystickAlpha(Color color)
        {
            if (backgroundImage != null)
                backgroundImage.color = color;
            if (handleImage != null)
            {
                Color handleColor = color;
                handleColor.a = Mathf.Min(1f, color.a * 1.5f); // Handle slightly more opaque
                handleImage.color = handleColor;
            }
        }
        
        // Public methods for external control
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
    }
}