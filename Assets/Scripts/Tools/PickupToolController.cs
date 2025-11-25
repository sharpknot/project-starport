using Starport.Characters;
using UnityEngine;

namespace Starport.Tools
{
    public class PickupToolController : ToolBase
    {
        [SerializeField] private GameObject _parentMesh;
        private CharacterPickupHandler _pickupHandler;
        private CharacterInteractableController _interactController;

        public override void Equip(PlayerStateManager stateManager)
        {
            base.Equip(stateManager);
            if (StateManager != null)
            {
                _pickupHandler = StateManager.PickupHandler;
                _interactController = StateManager.InteractableController;
            }
        }

        public override void Unholster()
        {
            base.Unholster();

            ShowObject(_parentMesh, true);
            if (_pickupHandler != null)
                _pickupHandler.SetAllowPickup(true);
        }

        public override void Holster()
        {
            base.Holster();

            ShowObject(_parentMesh, false);

            if (_pickupHandler != null)
                _pickupHandler.SetAllowPickup(false);
            
            if(_interactController != null)
                _interactController.SetAllowInteract(true);
        }

        public override void PrimaryAction()
        {
            base.PrimaryAction();
            if (_pickupHandler == null) return;
            if (_pickupHandler.CurrentPickup == null) return;

            _pickupHandler.ThrowCurrentPickup();
        }

        public override void SecondaryAction()
        {
            base.SecondaryAction();

            if (_pickupHandler == null) return;

            if (_pickupHandler.CurrentPickup != null)
            {
                _pickupHandler.DropCurrentPickup();
                return;
            }

            _pickupHandler.AttemptPickup();
        }

        private void Update()
        {
            if (IsHolstered) return;
            if(_pickupHandler != null && _interactController != null)
            {
                _interactController.SetAllowInteract(_pickupHandler.CurrentPickup == null);
            }
        }
    }
}
