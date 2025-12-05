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

        public event UnityAction OnJumpInput, OnOptionsMenuInput;
        public event UnityAction OnPrimaryInput, OnSecondaryInput, OnInteractInput;
        public event UnityAction<int> OnEquipToolInput;
        public event UnityAction OnNextToolInput, OnPreviousToolInput;

        public bool IsPrimaryPressed { get; private set; } = false;
        public bool IsSecondaryPressed { get; private set; } = false;
        public bool IsSprintPressed {  get; private set; } = false;

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
            UpdatePressed();
            UpdateCycleEquipment();
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

        private void UpdateCycleEquipment()
        {
            float val = InputActions.Main.CycleTool.ReadValue<float>();
            if (Mathf.Abs(val) <= 0.5f) return;

            if(val > 0f)
            {
                Debug.Log("[PlayerInputManager] Previous tool");
                OnPreviousToolInput?.Invoke();
                return;
            }

            Debug.Log("[PlayerInputManager] Next tool");
            OnNextToolInput?.Invoke();
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
            InputActions.Main.OptionsMenu.performed += OnOptionsMenu;
            InputActions.Main.PrimaryAction.performed += OnPrimary;
            InputActions.Main.SecondaryAction.performed += OnSecondary;
            InputActions.Main.Interact.performed += OnInteract;

            InputActions.Main.UseTool0.performed += OnEquipTool0;
            InputActions.Main.UseTool1.performed += OnEquipTool1;
            InputActions.Main.UseTool2.performed += OnEquipTool2;
            InputActions.Main.UseTool3.performed += OnEquipTool3;
            InputActions.Main.UseTool4.performed += OnEquipTool4;
        }

        private void ClearInputEvents()
        {
            if (InputActions == null)
                return;

            InputActions.Main.Jump.performed -= OnJump;
            InputActions.Main.OptionsMenu.performed -= OnOptionsMenu;
            InputActions.Main.PrimaryAction.performed -= OnPrimary;
            InputActions.Main.SecondaryAction.performed -= OnSecondary;
            InputActions.Main.Interact.performed -= OnInteract;

            InputActions.Main.UseTool0.performed -= OnEquipTool0;
            InputActions.Main.UseTool1.performed -= OnEquipTool1;
            InputActions.Main.UseTool2.performed -= OnEquipTool2;
            InputActions.Main.UseTool3.performed -= OnEquipTool3;
            InputActions.Main.UseTool4.performed -= OnEquipTool4;
        }

        private void UpdatePressed()
        {
            if (InputActions == null)
            {
                IsPrimaryPressed = false;
                IsSecondaryPressed = false;
                IsSprintPressed = false;
                return;
            }

            IsPrimaryPressed = InputActions.Main.PrimaryAction.IsPressed();
            IsSecondaryPressed = InputActions.Main.SecondaryAction.IsPressed();
            IsSprintPressed = InputActions.Main.SprintHold.IsPressed();
        }

        

        private void OnJump(InputAction.CallbackContext ctx) => OnJumpInput?.Invoke();
        private void OnOptionsMenu(InputAction.CallbackContext ctx) => OnOptionsMenuInput?.Invoke();
        private void OnPrimary(InputAction.CallbackContext ctx) => OnPrimaryInput?.Invoke();
        private void OnSecondary(InputAction.CallbackContext ctx) => OnSecondaryInput?.Invoke();
        private void OnInteract(InputAction.CallbackContext ctx) => OnInteractInput?.Invoke();

        private void OnEquipTool0(InputAction.CallbackContext ctx) => OnEquipToolInput?.Invoke(0);
        private void OnEquipTool1(InputAction.CallbackContext ctx) => OnEquipToolInput?.Invoke(1);
        private void OnEquipTool2(InputAction.CallbackContext ctx) => OnEquipToolInput?.Invoke(2);
        private void OnEquipTool3(InputAction.CallbackContext ctx) => OnEquipToolInput?.Invoke(3);
        private void OnEquipTool4(InputAction.CallbackContext ctx) => OnEquipToolInput?.Invoke(4);

    }
}
