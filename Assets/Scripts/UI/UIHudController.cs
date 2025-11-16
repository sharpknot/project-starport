using UnityEngine;

namespace Starport.UI
{
    public class UIHudController : MonoBehaviour
    {
        [SerializeField] private RectTransform _parentPanel;

        private void Awake()
        {
            UIEvents.ShowHUD += Show;
        }

        private void OnDestroy()
        {
            UIEvents.ShowHUD -= Show;
        }

        private void Show(bool show)
        {
            if (show) ShowHUD();
            else HideHUD();
        }

        private void ShowHUD()
        {
            Cursor.visible = false;
            UIUtility.ShowPanel(_parentPanel, true);
        }

        private void HideHUD()
        {
            UIUtility.ShowPanel(_parentPanel, false);
        }
    }
}
