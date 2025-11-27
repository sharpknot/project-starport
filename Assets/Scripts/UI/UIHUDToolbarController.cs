using Starport.Characters;
using Starport.Tools;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Starport.UI
{
    public class UIHUDToolbarController : MonoBehaviour
    {
        [SerializeField] private UIHUDToolbarIconController _icon;
        [SerializeField] private RectTransform _content;

        private List<UIHUDToolbarIconController> _currentIcons;

        private void Awake()
        {
            CharacterToolsHandler.OnToolsUpdate += RedrawToolbar;
        }

        private void OnDestroy()
        {
            CharacterToolsHandler.OnToolsUpdate -= RedrawToolbar;
        }

        private void RedrawToolbar(int currentToolIndex, ToolBase[] tools)
        {
            ClearCurrentToolbar();

            _currentIcons ??= new();
            if (tools == null || tools.Length <= 0 || _icon == null || _content == null)
                return;

            for(int i = 0; i < tools.Length; i++)
            {
                GameObject g = Instantiate(_icon.gameObject, _content);
                var curIcon = g.GetComponent<UIHUDToolbarIconController>();

                Sprite logo = null;
                if (tools[i] != null)
                    logo = tools[i].ToolIcon;

                curIcon.SetIcon(logo, (currentToolIndex == i));
                _currentIcons.Add(curIcon);
            }
        }

        private void ClearCurrentToolbar()
        {
            _currentIcons ??= new();
            foreach (var icon in _currentIcons)
            {
                if (icon == null) continue;
                Destroy(icon.gameObject);
            }

            _currentIcons.Clear();
        }
    }
}
