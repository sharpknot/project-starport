using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PSLocoJump", menuName = "Player State/Locomotion/Jumping")]
    public class PSLocoJump : PSLoco
    {
        [SerializeField] private float _minJumpDuration = 0.25f;
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private PlayerStateBase _fallingState;
        [SerializeField] private int _layer;

        private float _currentJumpDuration = 0f;
        private Vector3 _currentLateralVelocity = Vector3.zero;

        public override void EnterState(PlayerStateManager stateManager)
        {
            base.EnterState(stateManager);

            float jumpSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * _jumpHeight);
            _currentJumpDuration = 0f;

            _currentLateralVelocity = Vector3.zero;
            if(InputManager != null && MotionController != null)
                _currentLateralVelocity = InputManager.GetWorldFlatMoveDirection(StateManager.FirstPersonCamera) * MotionController.MaxAirSpeed;

            if(MotionController != null)
                MotionController.Jump(jumpSpeed);

            if(AnimatorController != null)
            {
                AnimatorController.SetLayerWeight(_layer, 1f, 0.1f);
            }
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);

            if(_currentJumpDuration >= _minJumpDuration)
            {
                if(MotionController.PreviousVelocity.y <= 0f)
                {
                    if(_fallingState != null) 
                        StateManager.ChangeLocomotionState(_fallingState);
                    else
                        StateManager.ChangeToDefaultLocomotionState();
                    return;
                }
            }
            else
            {
                _currentJumpDuration += deltaTime;
            }

            UpdateMotion(deltaTime);
        }

        public override void ExitState()
        {
            if (AnimatorController != null)
                AnimatorController.SetLayerWeight(_layer, 0f, 0.1f);

            base.ExitState();
        }

        private void OnValidate()
        {
            _jumpHeight = Mathf.Max(_jumpHeight, 0.1f);
        }

        private void UpdateMotion(float deltaTime)
        {
            if (StateManager == null || InputManager == null || MotionController == null)
                return;

            // Get desired input direction (from camera-relative movement)
            Vector3 desiredInput = StateManager.InputManager.GetWorldFlatMoveDirection(StateManager.FirstPersonCamera) * MotionController.MaxAirSpeed;

            // Gradually steer toward input direction
            _currentLateralVelocity = Vector3.Lerp(
                _currentLateralVelocity,
                desiredInput,
                MotionController.AirControlStrength * deltaTime
            );

            // Apply lateral motion
            MotionController.SetInputLateralMotion(_currentLateralVelocity * deltaTime);
        }
    }
}
