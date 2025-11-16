using Unity.Cinemachine;
using UnityEngine;
using Starport.PlayerState;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Starport.UI;

namespace Starport.Characters
{
    public class PlayerStateManager : MonoBehaviour
    {
        [field: SerializeField] 
        public CinemachineCamera FirstPersonCamera { get; private set; }

        [SerializeField] private Renderer[] _hideableRenderers;
        [SerializeField] private bool _initializeOnAwake = true;

        [SerializeField, BoxGroup("Base States"), ReadOnly]
        private PlayerStateBase _currentBaseState;
        [SerializeField, BoxGroup("Base States")]
        private PlayerStateBase _defaultBaseState, _startBaseState;

        [SerializeField, BoxGroup("Locomotion States"), ReadOnly]
        private PlayerStateBase _currentLocomotionState;
        [SerializeField, BoxGroup("Locomotion States")]
        private PlayerStateBase _defaultLocomotionState, _startLocomotionState;

        private bool _initialized = false;
        private Dictionary<Renderer, ShadowCastingMode> _rendererDefaultShadowCastingMode;

        public PlayerInputManager InputManager { get; private set; }
        [field: SerializeField] public CharacterMotionController MotionController { get; private set; }
        [field: SerializeField] public CharacterPickupHandler PickupHandler { get; private set; }
        [field: SerializeField] public CharacterAnimatorController AnimatorController { get; private set; }
        [field: SerializeField] public CharacterNetworkManager CharacterNetworkManager { get; private set; }
        [field: SerializeField] public CharacterInteractableController InteractableController { get; private set; }

        [field: SerializeField, ReadOnly] 
        public bool HasOpenedOptionsMenu { get; private set; } = false;

        public void ChangeBaseState(PlayerStateBase nextState) => ChangeState(ref _currentBaseState, nextState);
        public void ChangeLocomotionState(PlayerStateBase nextState) => ChangeState(ref _currentLocomotionState, nextState);
        public void ChangeToDefaultBaseState() => ChangeBaseState(_defaultBaseState);
        public void ChangeToDefaultLocomotionState() => ChangeLocomotionState(_defaultLocomotionState);
        public void ChangeToOverallDefaultState()
        {
            ChangeToDefaultBaseState();
            ChangeToDefaultLocomotionState();
        }

        public void InitializeStateManager()
        {
            InputManager = PlayerInputManager.Instance;
            InputManager.InputEnabled = true;

            UIEvents.HiddenOptionsMenu += OnOptionsMenuClosed;

            HideRenderers();

            StartInitialState(ref _currentBaseState, _startBaseState, _defaultBaseState);
            StartInitialState(ref _currentLocomotionState, _startLocomotionState, _defaultLocomotionState);

            if(MotionController != null) 
                MotionController.InitializeMotionController();

            UIEvents.ShowHUD?.Invoke(true);
            _initialized = true;
        }

        public void DisableCamera()
        {
            if(FirstPersonCamera != null) 
                FirstPersonCamera.gameObject.SetActive(false);
        }

        public void EnableAndUseCamera()
        {
            if (FirstPersonCamera == null) return;
            
            FirstPersonCamera.Prioritize();
            FirstPersonCamera.gameObject.SetActive(true);
        }

        public void HideRenderers()
        {
            _rendererDefaultShadowCastingMode ??= GenerateShadowCastingMode();
            foreach(var renderer in _rendererDefaultShadowCastingMode.Keys)
            {
                if (_rendererDefaultShadowCastingMode[renderer] == ShadowCastingMode.Off)
                    continue;
                renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            }
        }

        public void ShowRenderers()
        {
            _rendererDefaultShadowCastingMode ??= GenerateShadowCastingMode();
            foreach (var renderer in _rendererDefaultShadowCastingMode.Keys)
            {
                renderer.shadowCastingMode = _rendererDefaultShadowCastingMode[renderer];
            }
        }

        private void Start()
        {
            if(_initializeOnAwake)
                InitializeStateManager();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            UpdateStates(deltaTime);
        }

        private void OnDestroy()
        {
            StopCurrentState(ref _currentBaseState);
            StopCurrentState(ref _currentLocomotionState);

            UIEvents.HiddenOptionsMenu -= OnOptionsMenuClosed;
        }

        private Dictionary<Renderer, ShadowCastingMode> GenerateShadowCastingMode()
        {
            Dictionary<Renderer, ShadowCastingMode> result = new();
            if(_hideableRenderers == null)
                return result;

            foreach(Renderer renderer in _hideableRenderers)
            {
                if (renderer == null) continue;
                if (result.ContainsKey(renderer)) continue;

                result.Add(renderer, renderer.shadowCastingMode);
            }

            return result;
        }

        private void UpdateStates(float deltaTime)
        {
            if(!_initialized) return;
            if (deltaTime <= 0f) return;

            if(_currentBaseState != null) 
                _currentBaseState.UpdateState(deltaTime);

            if (_currentLocomotionState != null)
                _currentLocomotionState.UpdateState(deltaTime);
        }

        private void StartInitialState(ref PlayerStateBase currentState, PlayerStateBase startState, PlayerStateBase defaultState)
        {
            if (startState != null)
            {
                ChangeState(ref currentState, startState);
                return;
            }

            ChangeState(ref currentState, defaultState);
        }

        private void ChangeState(ref PlayerStateBase currentState, PlayerStateBase nextState)
        {
            StopCurrentState(ref currentState);

            if (nextState == null) return;

            currentState = Instantiate(nextState);
            currentState.EnterState(this);
        }

        private void StopCurrentState(ref PlayerStateBase currentState)
        {
            if (currentState == null) return;
            
            currentState.ExitState();
            PlayerStateBase temp = currentState;
            Destroy(temp);

            currentState = null;
        }

        private void OnOptionsMenuClosed()
        {
            if (!HasOpenedOptionsMenu) return;

            if (InputManager != null)
                InputManager.InputEnabled = true;

            HasOpenedOptionsMenu = false;
            UIEvents.ShowHUD?.Invoke(true);
        }
        public void OpenOptionsMenu()
        {
            if (HasOpenedOptionsMenu) return;

            if (InputManager != null)
                InputManager.InputEnabled = false;

            HasOpenedOptionsMenu = true;
            UIEvents.ShowHUD?.Invoke(false);
            UIEvents.ShowOptionsMenu?.Invoke();
        }
    }
}
