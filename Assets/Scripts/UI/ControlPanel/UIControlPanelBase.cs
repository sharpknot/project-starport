using UnityEngine;

namespace Starport.UI.ControlPanel
{
    [RequireComponent(typeof(CanvasGroup))]
    public class UIControlPanelBase : MonoBehaviour
    {
        public CanvasGroup CanvasGroup
        {
            get
            {
                if(_canvasGroup == null)
                    _canvasGroup = GetComponent<CanvasGroup>();
                return _canvasGroup;
            }
        }
        private CanvasGroup _canvasGroup;

        public virtual void EnableUI()
        {
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;

            Debug.Log($"[UIControlPanelBase {gameObject.name}] Enabled!");
        }

        public virtual void DisableUI()
        {
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;

            Debug.Log($"[UIControlPanelBase {gameObject.name}] Disabled!");
        }
    }
}
