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

            CharacterPickupHandler.OnCurrentPickupUpdate += CurrentPickupUpdate;
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);
            UpdateLook();
        }

        public override void ExitState()
        {
            UnsubscribeInputEvents();

            CharacterPickupHandler.OnCurrentPickupUpdate -= CurrentPickupUpdate;

            base.ExitState();
        }

        private void SubscribeInputEvents()
        {
            UnsubscribeInputEvents();

            if (InputManager == null) return;

            InputManager.OnOptionsMenuInput += OpenOptionsMenu;
            InputManager.OnPrimaryInput += PrimaryAction;
            InputManager.OnSecondaryInput += SecondaryAction;
            InputManager.OnInteractInput += InteractAction;
        }

        private void UnsubscribeInputEvents()
        {
            if (InputManager == null) return;
            InputManager.OnOptionsMenuInput -= OpenOptionsMenu;
            InputManager.OnPrimaryInput -= PrimaryAction;
            InputManager.OnSecondaryInput -= SecondaryAction;
            InputManager.OnInteractInput -= InteractAction;
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
            } 
        }

        private void SecondaryAction()
        {
            if(PickupHandler == null) return;

            if (PickupHandler.CurrentPickup == null)
            {
                PickupHandler.AttemptPickup();
            }  
            else
            {
                PickupHandler.DropCurrentPickup();
            }
                
        }

        private void InteractAction()
        {
            if (InteractableController == null)
                return;

            if (InteractableController.CurrentInteractable == null)
                return;

            InteractableController.AttemptInteract(CharacterNetworkManager);
        }

        private void CurrentPickupUpdate(PickupController currentPickup)
        {
            if (InteractableController != null)
            {
                InteractableController.SetAllowInteract(currentPickup == null);
            }
        }
    }
}
