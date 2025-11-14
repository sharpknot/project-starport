using UnityEngine;
using Starport.Characters;
using NaughtyAttributes;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PSLocoGroundMove", menuName = "Player State/Locomotion/Move on Ground")]
    public class PSLocoGroundMove : PlayerStateBase
    {
        [SerializeField, BoxGroup("Falling")] private PlayerStateBase _fallingLocomotionState, _jumpLocomotionState;
        [SerializeField, BoxGroup("Falling")] private float _maxCoyoteDuration = 0.5f;
        private float _currentCoyoteDuration = 0f;

        public override void EnterState(PlayerStateManager stateManager)
        {
            base.EnterState(stateManager);

            AddInputEvents();
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);

            if (CanChangeToFallingState(deltaTime))
            {
                StateManager.ChangeLocomotionState(_fallingLocomotionState);
                return;
            }

            if(MotionController != null)
            UpdateInputMovement(MotionController.NormalMoveSpeed, deltaTime);
        }

        public override void ExitState()
        {
            ClearInputEvents();
            base.ExitState();
        }

        private void OnValidate()
        {
            _maxCoyoteDuration = Mathf.Max(0f, _maxCoyoteDuration);
        }

        private bool CanChangeToFallingState(float deltaTime)
        {
            if(StateManager == null) return false;
            if(_fallingLocomotionState == null) return false;
            if(MotionController == null) return false;
            if(MotionController.IsGrounded(out _))
            {
                _currentCoyoteDuration = 0f;
                return false;
            }

            if(_currentCoyoteDuration > _maxCoyoteDuration)
            {
                return true;
            }

            _currentCoyoteDuration += deltaTime;
            return false;
        }

        private void AddInputEvents()
        {
            if (StateManager == null) return;
            if (StateManager.InputManager == null) return;

            StateManager.InputManager.OnJumpInput += OnJumpInput;
        }

        private void ClearInputEvents()
        {
            if (StateManager == null) return;
            if (StateManager.InputManager == null) return;

            StateManager.InputManager.OnJumpInput -= OnJumpInput;
        }

        private void OnJumpInput()
        {
            if (StateManager == null) return;
            if (StateManager.HasOpenedOptionsMenu) return;
            if (_jumpLocomotionState != null) 
                StateManager.ChangeLocomotionState(_jumpLocomotionState);            
        }
    }
}
