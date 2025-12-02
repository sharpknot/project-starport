using Starport.PlayerState;
using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PSBaseControlPanel", menuName = "Player State/Base/Control Panel")]
    public class PSBaseControlPanel : PSBase
    {
        public override void EnterState(PlayerStateManager stateManager)
        {
            base.EnterState(stateManager);

            StateManager.InteractableController.SetAllowInteract(false);
        }
    }
}
