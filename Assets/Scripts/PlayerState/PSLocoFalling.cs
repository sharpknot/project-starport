using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PSLocoFalling", menuName = "Player State/Locomotion/Falling")]
    public class PSLocoFalling : PlayerStateBase
    {
        private Vector3 _currentLateralVelocity = Vector3.zero;

        public override void EnterState(PlayerStateManager stateManager)
        {
            base.EnterState(stateManager);

            if(MotionController != null)
            {
                Vector3 prevVel = MotionController.PreviousVelocity;
                _currentLateralVelocity = new(prevVel.x, 0f, prevVel.z);
            }
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);

            if(HasGrounded())
            {
                StateManager.ChangeToDefaultLocomotionState();
                return;
            }

            UpdateMotion(deltaTime);
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

        private bool HasGrounded()
        {
            if(MotionController == null) return false;
            return (MotionController.IsGrounded(out _) || MotionController.CharacterControllerGrounded);
        }

        private void OnValidate()
        {

        }
    }
}
