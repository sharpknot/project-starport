using UnityEngine;
using Starport.Characters;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PSBaseLook", menuName = "Player State/Base/Look")]
    public class PSBaseLook : PSBase
    {
        public override void EnterState(PlayerStateManager stateManager)
        {
            base.EnterState(stateManager);
            SubscribeInputEvents();

            if(PickupHandler  != null)
                PickupHandler.SetAllowPickup(true);
            if (InteractableController != null)
                InteractableController.SetAllowInteract(true);
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);
            UpdateLook();
        }

        public override void ExitState()
        {
            UnsubscribeInputEvents();
            base.ExitState();
        }

        private void SubscribeInputEvents()
        {
            UnsubscribeInputEvents();

            if (InputManager == null) return;

            InputManager.OnOptionsMenuInput += OpenOptionsMenu;
            InputManager.OnPrimaryInput += PrimaryAction;
            InputManager.OnSecondaryInput += SecondaryAction;
        }

        private void UnsubscribeInputEvents()
        {
            if (InputManager == null) return;
            InputManager.OnOptionsMenuInput -= OpenOptionsMenu;
            InputManager.OnPrimaryInput -= PrimaryAction;
            InputManager.OnSecondaryInput -= SecondaryAction;
        }

        private void OpenOptionsMenu()
        {
            if (StateManager == null) return;

            StateManager.OpenOptionsMenu();
        }

        private void PrimaryAction()
        {
            if(PickupHandler == null) return;
            if(PickupHandler.CurrentPickup != null)
            {
                PickupHandler.ThrowCurrentPickup();
                if (InteractableController != null)
                    InteractableController.SetAllowInteract(true);
            } 
        }

        private void SecondaryAction()
        {
            if(PickupHandler == null) return;

            if (PickupHandler.CurrentPickup == null)
            {
                PickupHandler.AttemptPickup();
                if (InteractableController != null)
                    InteractableController.SetAllowInteract(false);
            }  
            else
            {
                PickupHandler.DropCurrentPickup();
                if (InteractableController != null)
                    InteractableController.SetAllowInteract(true);
            }
                
        }
    }
}
