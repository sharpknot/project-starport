using Starport.Characters;
using UnityEngine;

namespace Starport.Tools
{
    public class ToolBase : MonoBehaviour
    {
        [field: SerializeField] public string ToolName { get; private set; } = "Default Tool";
        protected virtual string GetDescription() => "Default tool description";

        protected PlayerStateManager StateManager { get; private set; } = null;
        protected CharacterToolsHandler ToolsHandler { get; private set; } = null;
        protected bool IsHolstered { get; private set; } = true;

        public virtual void Equip(PlayerStateManager stateManager)
        {
            StateManager = stateManager;
            if(StateManager != null)
            {
                ToolsHandler = StateManager.ToolsHandler;
            }

            AttachToParent();
        }

        public virtual void Unequip()
        {
            StateManager = null;
        }

        public virtual void Unholster() 
        {
            IsHolstered = false;
        }


        public virtual void Holster() 
        {
            IsHolstered = true;
        }

        public virtual void PrimaryAction() { }
        public virtual void SecondaryAction() { }
        public virtual void PrimaryPressedAction(float deltaTime) { }
        public virtual void SecondaryPressedAction(float deltaTime) { }

        private void AttachToParent()
        {
            Transform parent = null;
            if (StateManager != null) parent = StateManager.transform;
            if(ToolsHandler != null && ToolsHandler.ToolHoldPosition != null)
                parent = ToolsHandler.ToolHoldPosition;

            transform.SetParent(parent, true);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        protected void ShowObject(GameObject gameObject, bool show)
        {
            if(gameObject == null) return;
            if(gameObject.activeSelf == show) return;

            gameObject.SetActive(show);
        }
    }
}
