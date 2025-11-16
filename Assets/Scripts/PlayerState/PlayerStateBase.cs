using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    public class PlayerStateBase : ScriptableObject
    {
        protected PlayerStateManager StateManager { get; private set; }
        protected CharacterMotionController MotionController { get; private set; }
        protected PlayerInputManager InputManager { get; private set; }
        protected CharacterPickupHandler PickupHandler { get; private set; }
        protected CharacterAnimatorController AnimatorController { get; private set; }
        public virtual void EnterState(PlayerStateManager stateManager)
        {
            StateManager = stateManager;
            if(StateManager != null)
            {
                MotionController = StateManager.MotionController;
                InputManager = StateManager.InputManager;
                PickupHandler = StateManager.PickupHandler;
                AnimatorController = StateManager.AnimatorController;
            }
        }

        public virtual void UpdateState(float deltaTime)
        {
            
        }
        public virtual void ExitState() { }

        protected void UpdateLook()
        {
            if (StateManager == null) return;
            if (StateManager.HasOpenedOptionsMenu) return;
            if (StateManager.InputManager == null || StateManager.FirstPersonCamera == null) return;

            float minPitchAngle = -89f;
            float maxPitchAngle = 89f;

            Transform camTransform = StateManager.FirstPersonCamera.transform;
            Vector2 lookDeltaInput = StateManager.InputManager.LookDeltaInput;

            // Get the current pitch
            float currentPitch = camTransform.localEulerAngles.x;
            if (currentPitch > 180f)
                currentPitch -= 360f;

            // Apply delta
            float pitch = Mathf.Clamp(currentPitch - lookDeltaInput.y, minPitchAngle, maxPitchAngle);

            // Reapply rotation
            camTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);

            // Rotate yaw
            StateManager.transform.Rotate(Vector3.up, lookDeltaInput.x, Space.World);

        }

        protected void UpdateInputMovement(float maxSpeed, float deltaTime)
        {
            if (StateManager == null) return;
            if (StateManager.HasOpenedOptionsMenu) return;
            if (StateManager.InputManager == null || StateManager.FirstPersonCamera == null) return;

            if(StateManager.MotionController == null) return;

            Vector3 velocity = StateManager.InputManager.GetWorldFlatMoveDirection(StateManager.FirstPersonCamera) * maxSpeed;
            StateManager.MotionController.SetInputLateralMotion(velocity * deltaTime);
        }
    }
}
