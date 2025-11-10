using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PlayerStateBase", menuName = "Scriptable Objects/PlayerStateBase")]
    public class PlayerStateBase : ScriptableObject
    {
        public PlayerStateManager StateManager { get; private set; }

        public virtual void EnterState(PlayerStateManager stateManager)
        {
            StateManager = stateManager;

        }

        public virtual void UpdateState(float deltaTime)
        {
            UpdateLook();
        }
        public virtual void ExitState() { }

        protected void UpdateLook()
        {
            if (StateManager == null) return;
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
    }
}
