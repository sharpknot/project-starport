using Starport.Characters;
using UnityEngine;

namespace Starport.Tools
{
    public class RepairToolController : ToolBase
    {
        [SerializeField] private float _repairSpeed = 0.2f;
        [SerializeField] private GameObject _parentMesh;
        private CharacterFixableController _fixableController;
        private CharacterInteractableController _interactController;

        public override void Equip(PlayerStateManager stateManager)
        {
            base.Equip(stateManager);

            if(StateManager != null)
            {
                _fixableController = StateManager.FixableController;
                _interactController = StateManager.InteractableController;
            }
        }

        public override void Unholster()
        {
            base.Unholster();
            ShowObject(_parentMesh, true);

            if (_fixableController != null)
                _fixableController.SetAllowFixing(true);
        }

        public override void Holster()
        {
            base.Holster();
            ShowObject(_parentMesh, false);

            if (_fixableController != null)
                _fixableController.SetAllowFixing(false);

        }

        public override void PrimaryPressedAction(float deltaTime)
        {
            base.PrimaryPressedAction(deltaTime);

            if (_fixableController == null) return;
            if (_fixableController.CurrentFixable == null) return;

            float toFixAmount  = _repairSpeed * deltaTime;
            _fixableController.AttemptFix(toFixAmount);
        }

        private void OnValidate()
        {
            _repairSpeed = Mathf.Max(0.001f, _repairSpeed);
        }
    }
}
