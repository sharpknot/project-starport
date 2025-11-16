using Starport.PlayerState;
using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    public class PSBase : PlayerStateBase
    {
        public override void EnterState(PlayerStateManager stateManager)
        {
            base.EnterState(stateManager);

            if(AnimatorController != null)
            {
                AnimatorController.SetBaseResetTrigger();
            }
        }
    }
}
