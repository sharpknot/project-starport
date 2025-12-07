using UnityEngine;

namespace Starport.UI.Systems
{
    [RequireComponent(typeof(Canvas))]
    public class UISystemDisplay : MonoBehaviour
    {
        protected UISubsystemDisplay AddDisplay(UISubsystemDisplay displayUI, RectTransform parent, string name, bool isActive, float progress = 1f)
        {
            if (parent == null || displayUI == null) return null;

            GameObject g = Instantiate(displayUI.gameObject, parent);
            UISubsystemDisplay d = g.GetComponent<UISubsystemDisplay>();
            d.SetSubsystemDisplay(name, isActive, progress);

            return d;
        }
    }
}
