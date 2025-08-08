using UnityEngine;

namespace MageLock.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    public class SimpleNetworkController : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 50f;
        [SerializeField] private float deceleration = 30f;
        [SerializeField] private float maxSpeed = 10f;

        [Header("Physics")]
        [SerializeField] private float drag = 5f;

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

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponentInChildren<Animator>();
            
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            rb.linearDamping = drag;
            rb.useGravity = false; 
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
        }

        private void HandleMovement(Vector3 moveDirection)
        {
            Vector3 targetVelocity = moveDirection * moveSpeed;
            
            Vector3 currentVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 deltaVel = targetVelocity - currentVel;

            float rate = targetVelocity.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            Vector3 movementForce = deltaVel * (rate * Time.fixedDeltaTime);

            Vector3 newVel = currentVel + movementForce;
            
            if (newVel.sqrMagnitude > maxSpeed * maxSpeed)
            {
                newVel = Vector3.ClampMagnitude(newVel, maxSpeed);
                movementForce = newVel - currentVel;
            }

            rb.AddForce(movementForce, ForceMode.Force);
        }

        private void HandleRotation(Vector3 moveDirection)
        {
            if (!rotateTowardsMovement || moveDirection.sqrMagnitude < 0.01f)
                return;

            Vector3 direction = moveDirection.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
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
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, new Vector3(moveInput.x, 0, moveInput.y) * 2f);
            
            if (rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, rb.linearVelocity);
            }
        }
    }
}