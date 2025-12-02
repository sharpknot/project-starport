using Starport.PlayerState;
using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PSLocoControlPanelSit", menuName = "Player State/Locomotion/Control Panel Sit")]
    public class PSLocoControlPanelSit : PSLoco
    {
        [SerializeField] private int _layer;

        public override void EnterState(PlayerStateManager stateManager)
        {
            base.EnterState(stateManager);

            if (AnimatorController != null)
            {
                AnimatorController.SetLayerWeight(_layer, 1f, 0.1f);
            }

            if (MotionController != null)
            {
                MotionController.GravityMultiplier = 0f;

                if (StateManager.transform.parent != null)
                {
                    MotionController.TeleportInstant(StateManager.transform.parent.position);
                    StateManager.transform.localRotation = Quaternion.identity;
                }
            }

            if(NetworkTransform != null)
                NetworkTransform.Interpolate = false;
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);

            if (MotionController != null && StateManager.transform.parent != null)
            {
                MotionController.TeleportInstant(StateManager.transform.parent.position);
                StateManager.transform.localRotation = Quaternion.identity;
            }
        }

        public override void ExitState()
        {
            if (AnimatorController != null)
            {
                AnimatorController.SetLayerWeight(_layer, 0f, 0.1f);
            }

            if (NetworkTransform != null)
                NetworkTransform.Interpolate = true;

            base.ExitState();
        }
    }

}