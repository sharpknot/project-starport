using NaughtyAttributes;
using UnityEngine;

namespace Starport.Characters
{
    [RequireComponent(typeof(Rigidbody), typeof(CharacterController))]
    public class CharacterMotionController : MonoBehaviour
    {
        protected Rigidbody Rigidbody
        {
            get
            {
                if (_rigidBody == null)
                    _rigidBody = GetComponent<Rigidbody>();
                return _rigidBody;
            }
        }
        private Rigidbody _rigidBody;

        protected CharacterController CharacterController
        {
            get
            {
                if(_characterController == null)
                    _characterController = GetComponent<CharacterController>();
                return _characterController;
            }
        }
        private CharacterController _characterController;

        public float PushForce
        {
            get { return _pushForce; }
            set { _pushForce = Mathf.Max(0f, value); }
        }

        [field: SerializeField, ReadOnly]
        public Vector3 PreviousVelocity { get; private set; } = Vector3.zero;

        [SerializeField]
        private float _pushForce = 1f;

        private Vector3 _inputLateralMotion = Vector3.zero;
        [SerializeField, ReadOnly]
        private bool _initialized = false;
        [SerializeField, ReadOnly] private bool _isGrounded = false;
        private RaycastHit _groundHit;
        [SerializeField, ReadOnly]
        private float _currentSpeed = 0f, _previousVerticalVelocity = 0f, _jumpSpeed = 0f, _inputSpeed = 0f;

        [field: SerializeField, BoxGroup("Movement Parameters")]
        public float MaxAirSpeed { get; private set; } = 6f;
        [field: SerializeField, BoxGroup("Movement Parameters")]
        public float AirControlStrength { get; private set; } = 3f;
        
        public float NormalMoveSpeed
        {
            get { return _normalMoveSpeed; }
            set
            {
                _normalMoveSpeed = Mathf.Max(0f, value);
                _sprintMoveSpeed = Mathf.Max(_normalMoveSpeed, _sprintMoveSpeed);
            }
        }

        public float SprintMoveSpeed
        {
            get { return _sprintMoveSpeed; }
            set { _sprintMoveSpeed = Mathf.Max(_normalMoveSpeed, value); }
        }

        [SerializeField, BoxGroup("Movement Parameters")]
        private float _normalMoveSpeed = 5f;
        [SerializeField, BoxGroup("Movement Parameters")]
        private float _sprintMoveSpeed = 7f;

        [SerializeField, BoxGroup("Ground Parameters")]
        private LayerMask _groundLayer;

        public bool CharacterControllerGrounded => CharacterController.isGrounded;

        public void InitializeMotionController()
        {
            Rigidbody.interpolation = RigidbodyInterpolation.None;
            Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            Rigidbody.isKinematic = true;
            Rigidbody.useGravity = false;

            _inputLateralMotion = Vector3.zero;
            _previousVerticalVelocity = 0f;

            _isGrounded = false;
            _groundHit = new();

            _initialized = true;
        }

        public void SetInputLateralMotion(Vector3 lateralMotion)
        {
            if (!_initialized) return;
            lateralMotion.y = 0f;
            _inputLateralMotion = lateralMotion;
        }

        public bool IsGrounded(out RaycastHit groundContactPoint)
        {
            groundContactPoint = _groundHit;
            return _isGrounded;
        }

        public void Jump(float jumpSpeed) => _jumpSpeed = Mathf.Max(jumpSpeed, 0f);

        void Start()
        {
        
        }

        void Update()
        {
            UpdateGrounded();
            UpdateMotion();

            _currentSpeed = CharacterController.velocity.magnitude;
            _inputLateralMotion = Vector3.zero;
            _jumpSpeed = 0f;

            PreviousVelocity = CharacterController.velocity;
        }

        private void FixedUpdate()
        {
            
        }

        private void OnValidate()
        {
            _pushForce = Mathf.Max(0, _pushForce);
            MaxAirSpeed = Mathf.Max(0f, MaxAirSpeed);
            AirControlStrength = Mathf.Max(0, AirControlStrength);

            _normalMoveSpeed = Mathf.Max(0f, _normalMoveSpeed);
            _sprintMoveSpeed = Mathf.Max(_sprintMoveSpeed, _normalMoveSpeed);
        }

        private void UpdateMotion()
        {
            if (!_initialized) return;

            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            Vector3 surfaceInputMotion = GetSurfaceInputMotion(deltaTime);

            float netDownMotion = 0f;
            if(!CharacterController.isGrounded)
            {
                if (!_isGrounded || _previousVerticalVelocity > 0f)
                {
                    netDownMotion = _previousVerticalVelocity * deltaTime;
                    float gravVelocity = Physics.gravity.y * deltaTime;
                    netDownMotion += (gravVelocity * deltaTime);
                }
            }

            if(_jumpSpeed > 0f)
                netDownMotion = _jumpSpeed * deltaTime;

            _previousVerticalVelocity = netDownMotion / deltaTime;
            _inputSpeed = surfaceInputMotion.magnitude / deltaTime;
            CharacterController.Move(surfaceInputMotion + new Vector3(0f, netDownMotion, 0f));
        }

        private Vector3 GetSurfaceInputMotion(float deltaTime)
        {
            if(_inputLateralMotion == Vector3.zero)
                return _inputLateralMotion;

            if(_groundHit.normal == Vector3.zero)
                return _inputLateralMotion;

            Vector3 projMotion = Vector3.ProjectOnPlane(_inputLateralMotion, _groundHit.normal);
            float dotMotion = Vector3.Dot(_inputLateralMotion, _groundHit.normal);
            
            // Moving uphill
            if(dotMotion < 0f)
                return GetInputMotionOnUpSlope(projMotion);

            // Downhill, go faster
            //DebugExtension.DebugArrow(_groundHit.point, projMotion.normalized * 0.5f, Color.green, 5f);

            // Add downward force
            projMotion += (deltaTime * deltaTime * Physics.gravity);
            return projMotion;
        }

        private Vector3 GetInputMotionOnUpSlope(Vector3 projectedMotion)
        {
            float slopeAngle = Vector3.Angle(_groundHit.normal, Vector3.up);
            
            // Within the slope limit
            if(slopeAngle < CharacterController.slopeLimit)
            {
                // Clamp the projected motion to the lateral motion's magnitude
                Vector3 toFlat = Vector3.ClampMagnitude(projectedMotion, _inputLateralMotion.magnitude);

                // Only take the lateral motion
                return new(toFlat.x, 0f, toFlat.z);
            }

            return _inputLateralMotion;
        }

        private void UpdateGrounded()
        {
            _isGrounded = false;
            _groundHit = new();

            if (!_initialized) return;

            bool foundGround = HasGround(CharacterController.skinWidth * 2f, out RaycastHit highestHit);

            _isGrounded = foundGround;
            _groundHit = highestHit;
        }

        public bool HasGround(float distanceBelowCollider, out RaycastHit groundHit)
        {
            groundHit = new();
            distanceBelowCollider = Mathf.Max(0f, distanceBelowCollider);

            Vector3 startPos = transform.position + CharacterController.center;
            float lowestPoint = (CharacterController.height * 0.5f) + distanceBelowCollider;
            float downCenterDistance = lowestPoint - CharacterController.radius;

            RaycastHit[] hits = new RaycastHit[256];
            int hitCount = Physics.SphereCastNonAlloc(startPos, CharacterController.radius, Vector3.down, hits, downCenterDistance, _groundLayer, QueryTriggerInteraction.Ignore);

            //DebugExtension.DebugCapsule(startPos, startPos+ (Vector3.down * lowestPoint), Color.yellow, CharacterController.radius);

            if (hitCount <= 0)
                return false;

            bool foundGround = false;
            float highestPos = startPos.y - lowestPoint;
            RaycastHit highestHit = new();

            //Debug.DrawLine(startPos, new Vector3(startPos.x, highestPos, startPos.z), Color.blue);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.transform == null)
                    continue;
                if (hit.transform == transform)
                    continue;

                if (hit.normal == Vector3.zero)
                    continue;

                float normalAngle = Vector3.Angle(hit.normal, Vector3.up);
                if (normalAngle >= 90f)
                    continue;

                // lower than lowest position
                if (hit.point.y <= highestPos)
                    continue;

                highestPos = hit.point.y;
                highestHit = hit;
                foundGround = true;
            }

            // No ground found
            if (!foundGround)
                return false;

            groundHit = highestHit;

            //DebugExtension.DebugArrow(groundHit.point, groundHit.normal * 0.25f, Color.magenta);

            return true;
        }

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Make sure we hit a Rigidbody
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic)
                return;

            // Don’t push below the character (like the ground)
            if (hit.moveDirection.y < -0.3f)
                return;

            // Calculate push direction (ignore Y)
            Vector3 pushDir = new(hit.moveDirection.x, 0, hit.moveDirection.z);

            // Apply impulse
            body.AddForce(pushDir * PushForce, ForceMode.Impulse);
        }
    }
}
