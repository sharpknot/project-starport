using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Starport
{
    public class PlayerInputManager : MonoBehaviour
    {
        public static PlayerInputManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayerInputManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("PlayerInputManager");
                        _instance = go.AddComponent<PlayerInputManager>();
                    }
                }
                return _instance;
            }
        }
        private static PlayerInputManager _instance;

        protected PlayerInputActions InputActions
        {
            get
            {
                if(_inputActions == null)
                {
                    _inputActions = new PlayerInputActions();
                    _inputActions.Enable();
                    _inputEnabled = true;

                    InitializeInputEvents();
                }
                return _inputActions;
            }
        }
        private PlayerInputActions _inputActions;

        public bool InputEnabled
        {
            get { return _inputEnabled; }
            set
            {
                if(value)
                    InputActions.Enable();
                else
                    InputActions.Disable();

                _inputEnabled = value;
            }
        }
        private bool _inputEnabled = true;

        public Vector2 MovementInput { get; private set; } = Vector2.zero;
        public Vector2 LookDeltaInput { get; private set; } = Vector2.zero;

        public event UnityAction OnJumpInput;

        public Vector3 GetWorldFlatMoveDirection(Camera camera)
        {
            if(camera == null) return Vector3.zero;
            return GetWorldFlatMoveDirectionTransform(camera.transform);
        }

        public Vector3 GetWorldFlatMoveDirection(CinemachineCamera camera)
        {
            if(camera==null) return Vector3.zero;
            return GetWorldFlatMoveDirectionTransform(camera.transform);
        }

        private void Awake()
        {
            // If an instance already exists and it's not this, destroy the duplicate
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Assign and make persistent
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            UpdateLookDelta();
            UpdateMoveInput();
        }

        private void OnDestroy()
        {
            ClearInputEvents();
        }

        private void UpdateLookDelta()
        {
            LookDeltaInput = InputActions.Main.Look.ReadValue<Vector2>();
        }

        private void UpdateMoveInput()
        {
            Vector2 input = InputActions.Main.Movement.ReadValue<Vector2>();
            MovementInput = Vector2.ClampMagnitude(input, 1f);
        }

        private Vector3 GetWorldFlatMoveDirectionTransform(Transform transform)
        {
            if(transform == null)
                return Vector3.zero;
            if(MovementInput == Vector2.zero) 
                return Vector3.zero;

            Vector3 flatFwd = transform.forward;
            flatFwd.y = 0f;
            Vector3 flatRight = transform.right;
            flatRight.y = 0f;

            if (flatFwd == Vector3.zero || flatRight == Vector3.zero)
                return Vector3.zero;

            return (flatRight.normalized * MovementInput.x) + (flatFwd.normalized * MovementInput.y);
        }

        private void InitializeInputEvents()
        {
            if (InputActions == null)
                return;

            InputActions.Main.Jump.performed += OnJump;
        }

        private void ClearInputEvents()
        {
            if (InputActions == null)
                return;

            InputActions.Main.Jump.performed -= OnJump;
        }

        private void OnJump(InputAction.CallbackContext ctx) => OnJumpInput?.Invoke();
    }
}
