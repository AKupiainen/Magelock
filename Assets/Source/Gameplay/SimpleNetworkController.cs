using UnityEngine;

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

        [Header("Input")]
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private float deadZone = 0.1f;

        private Rigidbody rb;
        private Animator animator;
        private Vector2 moveInput;
        private bool isLocalPlayer = false;

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.linearDamping = 0f; // No drag for instant stop
            rb.useGravity = false; 
        }

        public void SetIsLocalPlayer(bool isLocal)
        {
            isLocalPlayer = isLocal;
        }

        public void HandleInput()
        {
            float h = GetAxisWithDeadZone(horizontalAxis);
            float v = GetAxisWithDeadZone(verticalAxis);

            moveInput = new Vector2(h, v);
            if (moveInput.sqrMagnitude > 1f) 
                moveInput.Normalize();
        }

        private float GetAxisWithDeadZone(string axisName)
        {
            float value = Input.GetAxis(axisName);
            return Mathf.Abs(value) > deadZone ? value : 0f;
        }

        public Vector2 GetMoveInput() => moveInput;

        public void SetNetworkInput(Vector2 networkMoveInput)
        {
            moveInput = networkMoveInput;
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
                // For non-kinematic bodies, set velocity directly
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
                // For kinematic bodies, use input magnitude directly
                return moveInput.magnitude * moveSpeed;
            }
            else
            {
                // For non-kinematic bodies, use actual velocity
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
            // Update animations in Update for all players (local and remote)
            if (animator != null)
            {
                float targetSpeed = GetAnimationSpeed();
                animator.SetFloat(SpeedHash, targetSpeed);
            }
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