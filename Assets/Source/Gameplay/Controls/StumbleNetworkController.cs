using UnityEngine;
using BrawlLine.Utilies;

namespace BrawlLine.Controls
{
    [RequireComponent(typeof(Rigidbody))]
    public class StumbleNetworkController : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 30f;
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float airControl = 0.5f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 15f;

        [Header("Physics")]
        [SerializeField] private float groundDrag = 5f;
        [SerializeField] private float airDrag = 1f;
        [SerializeField] private float gravityMultiplier = 2f;

        [Header("Rotation")]
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private bool rotateTowardsMovement = true;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckRadius = 0.3f;
        [SerializeField] private float groundCheckDistance = 0.2f;
        [SerializeField] private int groundRayPoints = 5;
        [SerializeField] private float maxCrackWidth = 0.5f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Input")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private KeyCode jumpButton = KeyCode.Joystick1Button0;
        [SerializeField] private float deadZone = 0.1f;

        private Rigidbody rb;
        private Animator animator;
        
        private struct InputState
        {
            public Vector2 MoveInput;
            public float JumpInput;
        }

        private InputState input;
        private GroundResult currentGroundResult;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            Physics.gravity = Vector3.down * (9.81f * gravityMultiplier);
        }

        private void Update()
        {
            UpdateDrag();
        }

        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        public void HandleInput()
        {
            float h = GetAxisWithDeadZone(horizontalAxis);
            float v = GetAxisWithDeadZone(verticalAxis);

            input.MoveInput = new Vector2(h, v);
            if (input.MoveInput.sqrMagnitude > 1f) 
                input.MoveInput.Normalize();

            input.JumpInput = Input.GetKeyDown(jumpButton) ? jumpForce : 0f;
        }

        private float GetAxisWithDeadZone(string axisName)
        {
            float value = Input.GetAxis(axisName);
            return Mathf.Abs(value) > deadZone ? value : 0f;
        }

        public Vector2 GetMoveInput() => input.MoveInput;
        public float GetJumpInput() => input.JumpInput;

        public void SetNetworkInput(Vector2 networkMoveInput, float networkJumpInput)
        {
            input.MoveInput = networkMoveInput;
            input.JumpInput = networkJumpInput;
        }

        public void ProcessMovement()
        {
            CheckGround();
            
            Vector3 moveInput3D = new Vector3(input.MoveInput.x, 0f, input.MoveInput.y);
            
            HandleMovement(moveInput3D);
            HandleRotation(moveInput3D);
            
            if (input.JumpInput > 0f)
            {
                HandleJump(input.JumpInput);
            }
        }

        private void CheckGround()
        {
            currentGroundResult = FastGroundDetection.CheckGround(
                transform.position, 
                groundCheckRadius, 
                groundCheckDistance, 
                groundLayer, 
                groundRayPoints, 
                maxCrackWidth
            );
        }

        private bool IsGrounded => currentGroundResult.IsGrounded;

        private void HandleJump(float jumpVelocity)
        {
            if (IsGrounded)
            {
                Vector3 currentVel = rb.linearVelocity;
                rb.linearVelocity = new Vector3(currentVel.x, jumpVelocity, currentVel.z);
                
                Debug.Log($"Jump performed with velocity {jumpVelocity}! IsServer: {GetComponent<Unity.Netcode.NetworkBehaviour>()?.IsServer}");
            }
        }

        private void HandleMovement(Vector3 moveInput)
        {
            float controlMultiplier = IsGrounded ? 1f : airControl;
            Vector3 targetVelocity = moveInput * (moveSpeed * controlMultiplier);
            
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 deltaVel = targetVelocity - horizontalVel;

            float rate = targetVelocity.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            Vector3 movementForce = deltaVel * (rate * Time.fixedDeltaTime);

            Vector3 newVel = horizontalVel + movementForce;
            
            if (newVel.sqrMagnitude > maxSpeed * maxSpeed)
            {
                newVel = Vector3.ClampMagnitude(newVel, maxSpeed);
                movementForce = newVel - horizontalVel;
            }

            rb.AddForce(new Vector3(movementForce.x, 0f, movementForce.z), ForceMode.Force);
        }

        private void HandleRotation(Vector3 moveInput)
        {
            if (!rotateTowardsMovement || moveInput.sqrMagnitude < 0.01f)
                return;

            Vector3 direction = moveInput.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        private void UpdateDrag()
        {
            rb.linearDamping = IsGrounded ? groundDrag : airDrag;
        }

        public float GetAnimationSpeed()
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            return horizontalVel.magnitude;
        }

        public void UpdateNetworkAnimations(float networkSpeed)
        {
            if (animator == null) return;
            animator.SetFloat(SpeedHash, networkSpeed);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying) return;
            
            Vector3 center = transform.position + Vector3.up * (groundCheckRadius + 0.02f);
            
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(center, groundCheckRadius);
            
            DrawGroundCheckRays(center);
            
            if (IsGrounded)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(currentGroundResult.Point, currentGroundResult.Normal * 2f);
            }
        }

        private void DrawGroundCheckRays(Vector3 center)
        {
            Gizmos.color = Color.yellow;
            float angleStep = 360f / groundRayPoints * Mathf.Deg2Rad;
            
            for (int i = 0; i < groundRayPoints; i++)
            {
                float angle = angleStep * i;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * groundCheckRadius;
                Vector3 rayStart = center + offset;
                Gizmos.DrawRay(rayStart, Vector3.down * groundCheckDistance);
            }
        }
    }
}