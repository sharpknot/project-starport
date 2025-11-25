using UnityEngine;
using Starport.Characters;
using Starport.Pickups;

namespace Starport.PlayerState
{
    [CreateAssetMenu(fileName = "PSBaseLook", menuName = "Player State/Base/Look")]
    public class PSBaseLook : PSBase
    {
        public override void EnterState(PlayerStateManager stateManager)
        {
            base.EnterState(stateManager);
            SubscribeInputEvents();

            StateManager.EnableAndUseCamera();
        }

        public override void UpdateState(float deltaTime)
        {
            base.UpdateState(deltaTime);
            UpdateLook();
            UpdatePrimaryPressed(deltaTime);
            UpdateSecondaryPressed(deltaTime);
        }

        public override void ExitState()
        {
            UnsubscribeInputEvents();

            base.ExitState();
        }

        private void UpdatePrimaryPressed(float deltaTime)
        {
            if(InputManager == null) return;
            if (!InputManager.IsPrimaryPressed) return;
            if(ToolsHandler == null) return;

            ToolsHandler.PrimaryPressedAction(deltaTime);
        }

        private void UpdateSecondaryPressed(float deltaTime)
        {
            if (InputManager == null) return;
            if (!InputManager.IsPrimaryPressed) return;
            if (ToolsHandler == null) return;

            ToolsHandler.SecondaryPressedAction(deltaTime);
        }


        private void SubscribeInputEvents()
        {
            UnsubscribeInputEvents();

            if (InputManager == null) return;

            InputManager.OnOptionsMenuInput += OpenOptionsMenu;
            InputManager.OnPrimaryInput += PrimaryAction;
            InputManager.OnSecondaryInput += SecondaryAction;
            InputManager.OnInteractInput += InteractAction;
            InputManager.OnEquipToolInput += EquipToolAction;

            InputManager.OnNextToolInput += EquipNextTool;
            InputManager.OnPreviousToolInput += EquipPreviousTool;
        }

        private void UnsubscribeInputEvents()
        {
            if (InputManager == null) return;
            InputManager.OnOptionsMenuInput -= OpenOptionsMenu;
            InputManager.OnPrimaryInput -= PrimaryAction;
            InputManager.OnSecondaryInput -= SecondaryAction;
            InputManager.OnInteractInput -= InteractAction;
            InputManager.OnEquipToolInput -= EquipToolAction;

            InputManager.OnNextToolInput -= EquipNextTool;
            InputManager.OnPreviousToolInput -= EquipPreviousTool;
        }

        private void OpenOptionsMenu()
        {
            if (StateManager == null) return;

            StateManager.OpenOptionsMenu();
        }

        private void PrimaryAction()
        {
            if (ToolsHandler != null)
                ToolsHandler.PrimaryAction();
        }

        private void SecondaryAction()
        {
            if (ToolsHandler != null)
                ToolsHandler.SecondaryAction();
        }

        private void EquipToolAction(int toolIndex)
        {
            if(ToolsHandler == null) return;
            ToolsHandler.SetCurrentTool(toolIndex);
        }

        private void EquipNextTool() => CycleTool(true);
        private void EquipPreviousTool() => CycleTool(false);
        private void CycleTool(bool nextTool)
        {
            if (ToolsHandler == null) return;

            int nextIndex = 0;
            if(nextTool)
            {
                nextIndex = ToolsHandler.CurrentToolIndex + 1;
                if (nextIndex >= ToolsHandler.Tools.Length)
                    nextIndex = 0;
            }
            else
            {
                nextIndex = ToolsHandler.CurrentToolIndex - 1;
                if (nextIndex < 0) 
                    nextIndex = ToolsHandler.Tools.Length - 1;
            }

            ToolsHandler.SetCurrentTool(nextIndex);
        }

        private void InteractAction()
        {
            if (InteractableController == null)
                return;

            if (InteractableController.CurrentInteractable == null)
                return;

            InteractableController.AttemptInteract(CharacterNetworkManager);
        }
    }
}
