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

        private Rigidbody _rb;
        private Animator _animator;
        private Vector2 _moveInput;
        private Vector2 _keyboardInput;
        private Vector2 _joystickInput;
        private bool _isLocalPlayer;

        private void Awake()
        {
            InitializeComponents();
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
            _rb = GetComponent<Rigidbody>();
            _animator = GetComponentInChildren<Animator>();
            
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            _rb.linearDamping = 0f; 
            _rb.useGravity = false; 
        }

        public void SetIsLocalPlayer(bool isLocal)
        {
            _isLocalPlayer = isLocal;
            
            if (useEventSystem)
            {
                EventsBus.Trigger(new LocalPlayerStatusEvent(gameObject, isLocal));
            }
            
            Debug.Log($"Player {gameObject.name} local status set to: {isLocal}");
        }

        public void HandleInput()
        {
            if (!_isLocalPlayer) return;
            
            if (inputMode == InputMode.Keyboard || inputMode == InputMode.Both)
            {
                float h = GetAxisWithDeadZone(horizontalAxis);
                float v = GetAxisWithDeadZone(verticalAxis);
                _keyboardInput = new Vector2(h, v);
                
                if (useEventSystem && _keyboardInput.sqrMagnitude > 0.01f)
                {
                    EventsBus.Trigger(new MovementInputEvent(_keyboardInput, InputSource.Keyboard));
                }
            }

            switch (inputMode)
            {
                case InputMode.Keyboard:
                    _moveInput = _keyboardInput;
                    break;
                case InputMode.VirtualJoystick:
                    _moveInput = _joystickInput;
                    break;
                case InputMode.Both:
                    _moveInput = (_keyboardInput.sqrMagnitude > _joystickInput.sqrMagnitude) ? _keyboardInput : _joystickInput;
                    break;
            }

            if (_moveInput.sqrMagnitude > 1f) 
                _moveInput.Normalize();
        }

        private void OnMovementInputReceived(MovementInputEvent e)
        {
            if (!_isLocalPlayer) return;
            
            switch (e.Source)
            {
                case InputSource.Keyboard:
                    _keyboardInput = e.MoveInput;
                    break;
                case InputSource.VirtualJoystick:
                    _joystickInput = e.MoveInput;
                    break;
                case InputSource.Gamepad:
                    break;
                case InputSource.Network:
                    if (!_isLocalPlayer)
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
                    _moveInput = _keyboardInput;
                    break;
                case InputMode.VirtualJoystick:
                    _moveInput = _joystickInput;
                    break;
                case InputMode.Both:
                    _moveInput = (_keyboardInput.sqrMagnitude > _joystickInput.sqrMagnitude) ? _keyboardInput : _joystickInput;
                    break;
            }

            if (_moveInput.sqrMagnitude > 1f) 
                _moveInput.Normalize();
        }

        private void OnInputModeChanged(InputModeChangeEvent e)
        {
            inputMode = e.NewMode;
            
            if (e.NewMode == InputMode.Keyboard)
            {
                _joystickInput = Vector2.zero;
            }
            else if (e.NewMode == InputMode.VirtualJoystick)
            {
                _keyboardInput = Vector2.zero;
            }
            
            UpdateCombinedInput();
        }

        private float GetAxisWithDeadZone(string axisName)
        {
            float value = Input.GetAxis(axisName);
            return Mathf.Abs(value) > deadZone ? value : 0f;
        }

        public Vector2 GetMoveInput() => _moveInput;

        public void SetNetworkInput(Vector2 networkMoveInput)
        {
            if (!_isLocalPlayer)
            {
                _moveInput = networkMoveInput;
            }
        }

        public void ProcessMovement()
        {
            Vector3 moveInput3D = new Vector3(_moveInput.x, 0f, _moveInput.y);
            
            HandleMovement(moveInput3D);
            HandleRotation(moveInput3D);
            
            UpdateAnimations();
        }

        private void HandleMovement(Vector3 moveDirection)
        {
            if (_rb.isKinematic)
            {
                Vector3 movement = moveDirection * (moveSpeed * Time.fixedDeltaTime);
                _rb.MovePosition(_rb.position + movement);
            }
            else
            {
                Vector3 targetVelocity = moveDirection * moveSpeed;
                _rb.linearVelocity = new Vector3(targetVelocity.x, _rb.linearVelocity.y, targetVelocity.z);
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
            if (_animator == null) return;

            float targetSpeed = GetAnimationSpeed();
            _animator.SetFloat(SpeedHash, targetSpeed);
        }

        public float GetAnimationSpeed()
        {
            if (_rb.isKinematic)
            {
                return _moveInput.magnitude * moveSpeed;
            }

            Vector3 horizontalVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            return horizontalVel.magnitude;
        }
        
        public void SetAnimationSpeed(float speed)
        {
            if (_animator == null) return;
            _animator.SetFloat(SpeedHash, speed);
        }
        
        private void Update()
        {
            if (_isLocalPlayer && !useEventSystem)
            {
                HandleInput();
            }
            
            if (_animator != null)
            {
                float targetSpeed = GetAnimationSpeed();
                _animator.SetFloat(SpeedHash, targetSpeed);
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
            Gizmos.DrawRay(transform.position, new Vector3(_moveInput.x, 0, _moveInput.y) * 2f);
            
            if (_rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, _rb.linearVelocity);
            }
            
            Gizmos.color = Color.green;
            Vector3 animDir = transform.forward * GetAnimationSpeed() * 0.5f;
            Gizmos.DrawRay(transform.position + Vector3.up * 0.1f, animDir);
        }
    }
}