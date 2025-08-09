using UnityEngine;
using MageLock.Events;
using MageLock.Gameplay.Events;

namespace MageLock.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleNetworkController : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private bool rotateTowardsMovement = true;

        [Header("Input Settings")]
        [SerializeField] private InputMode inputMode = InputMode.Both;
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private float deadZone = 0.1f;
        [SerializeField] private bool useEventSystem = true;

        private Rigidbody rb;
        private Animator animator;
        private Vector2 moveInput;
        private Vector2 keyboardInput;
        private Vector2 joystickInput;
        private bool isLocalPlayer = false;
        private InputMode previousInputMode;

        private void Awake()
        {
            InitializeComponents();
            previousInputMode = inputMode;
        }

        private void OnEnable()
        {
            if (useEventSystem)
            {
                EventsBus.Subscribe<MovementInputEvent>(OnMovementInputReceived);
                EventsBus.Subscribe<InputModeChangeEvent>(OnInputModeChanged);
            }
        }

        private void OnDisable()
        {
            if (useEventSystem)
            {
                EventsBus.Unsubscribe<MovementInputEvent>(OnMovementInputReceived);
                EventsBus.Unsubscribe<InputModeChangeEvent>(OnInputModeChanged);
            }
        }

        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.linearDamping = 0f; 
            rb.useGravity = false; 
        }

        public void SetIsLocalPlayer(bool isLocal)
        {
            isLocalPlayer = isLocal;
            
            // Fire event for other systems to react
            if (useEventSystem)
            {
                EventsBus.Trigger(new LocalPlayerStatusEvent(gameObject, isLocal));
            }
            
            Debug.Log($"Player {gameObject.name} local status set to: {isLocal}");
        }

        public void HandleInput()
        {
            if (!isLocalPlayer) return;
            
            if (inputMode == InputMode.Keyboard || inputMode == InputMode.Both)
            {
                float h = GetAxisWithDeadZone(horizontalAxis);
                float v = GetAxisWithDeadZone(verticalAxis);
                keyboardInput = new Vector2(h, v);
                
                if (useEventSystem && keyboardInput.sqrMagnitude > 0.01f)
                {
                    EventsBus.Trigger(new MovementInputEvent(keyboardInput, InputSource.Keyboard));
                }
            }

            switch (inputMode)
            {
                case InputMode.Keyboard:
                    moveInput = keyboardInput;
                    break;
                case InputMode.VirtualJoystick:
                    moveInput = joystickInput;
                    break;
                case InputMode.Both:
                    moveInput = (keyboardInput.sqrMagnitude > joystickInput.sqrMagnitude) ? keyboardInput : joystickInput;
                    break;
            }

            if (moveInput.sqrMagnitude > 1f) 
                moveInput.Normalize();
        }

        private void OnMovementInputReceived(MovementInputEvent e)
        {
            if (!isLocalPlayer) return;
            
            switch (e.Source)
            {
                case InputSource.Keyboard:
                    keyboardInput = e.MoveInput;
                    break;
                case InputSource.VirtualJoystick:
                    joystickInput = e.MoveInput;
                    break;
                case InputSource.Gamepad:
                    break;
                case InputSource.Network:
                    if (!isLocalPlayer)
                    {
                        SetNetworkInput(e.MoveInput);
                    }
                    break;
            }
            
            UpdateCombinedInput();
        }

        private void UpdateCombinedInput()
        {
            switch (inputMode)
            {
                case InputMode.Keyboard:
                    moveInput = keyboardInput;
                    break;
                case InputMode.VirtualJoystick:
                    moveInput = joystickInput;
                    break;
                case InputMode.Both:
                    moveInput = (keyboardInput.sqrMagnitude > joystickInput.sqrMagnitude) ? keyboardInput : joystickInput;
                    break;
            }

            // Normalize if magnitude > 1
            if (moveInput.sqrMagnitude > 1f) 
                moveInput.Normalize();
        }

        private void OnInputModeChanged(InputModeChangeEvent e)
        {
            inputMode = e.NewMode;
            
            if (e.NewMode == InputMode.Keyboard)
            {
                joystickInput = Vector2.zero;
            }
            else if (e.NewMode == InputMode.VirtualJoystick)
            {
                keyboardInput = Vector2.zero;
            }
            
            UpdateCombinedInput();
        }

        private float GetAxisWithDeadZone(string axisName)
        {
            float value = Input.GetAxis(axisName);
            return Mathf.Abs(value) > deadZone ? value : 0f;
        }

        public Vector2 GetMoveInput() => moveInput;

        public void SetNetworkInput(Vector2 networkMoveInput)
        {
            if (!isLocalPlayer)
            {
                moveInput = networkMoveInput;
            }
        }

        public void ProcessMovement()
        {
            Vector3 moveInput3D = new Vector3(moveInput.x, 0f, moveInput.y);
            
            HandleMovement(moveInput3D);
            HandleRotation(moveInput3D);
            
            UpdateAnimations();
        }

        private void HandleMovement(Vector3 moveDirection)
        {
            if (rb.isKinematic)
            {
                // For kinematic bodies, use MovePosition
                Vector3 movement = moveDirection * moveSpeed * Time.fixedDeltaTime;
                rb.MovePosition(rb.position + movement);
            }
            else
            {
                Vector3 targetVelocity = moveDirection * moveSpeed;
                rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
            }
        }

        private void HandleRotation(Vector3 moveDirection)
        {
            if (!rotateTowardsMovement || moveDirection.sqrMagnitude < 0.01f)
                return;

            Vector3 direction = moveDirection.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        private void UpdateAnimations()
        {
            if (animator == null) return;

            float targetSpeed = GetAnimationSpeed();
            animator.SetFloat(SpeedHash, targetSpeed);
        }

        public float GetAnimationSpeed()
        {
            if (rb.isKinematic)
            {
                return moveInput.magnitude * moveSpeed;
            }
            else
            {
                Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                return horizontalVel.magnitude;
            }
        }
        
        public void SetAnimationSpeed(float speed)
        {
            if (animator == null) return;
            animator.SetFloat(SpeedHash, speed);
        }
        
        private void Update()
        {
            if (isLocalPlayer && !useEventSystem)
            {
                HandleInput();
            }
            
            if (animator != null)
            {
                float targetSpeed = GetAnimationSpeed();
                animator.SetFloat(SpeedHash, targetSpeed);
            }
        }

        private void FixedUpdate()
        {
            ProcessMovement();
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, new Vector3(moveInput.x, 0, moveInput.y) * 2f);
            
            if (rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, rb.linearVelocity);
            }
            
            Gizmos.color = Color.green;
            Vector3 animDir = transform.forward * GetAnimationSpeed() * 0.5f;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, animDir);
        }
    }
}