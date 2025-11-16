using TMPro;
using UnityEngine;

namespace Starport
{
    public static class UIUtility
    {
        public static void ShowPanel(RectTransform panel, bool show)
        {
            if (panel == null) return;
            if(panel.gameObject.activeSelf != show)
                panel.gameObject.SetActive(show);
        }

        public static void SetText(TMP_Text textBox, string text)
        {
            if(textBox == null) return;
            textBox.text = text;
        }
    }
}
