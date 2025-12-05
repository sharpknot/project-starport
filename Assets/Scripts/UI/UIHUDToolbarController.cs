using Starport.Characters;
using Starport.Tools;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using DG.Tweening;
using TMPro;
using NaughtyAttributes;

namespace Starport.UI
{
    public class UIHUDToolbarController : MonoBehaviour
    {
        [SerializeField] private UIHUDToolbarIconController _icon;
        [SerializeField] private RectTransform _content;

        [SerializeField, BoxGroup("Tool Name")] private RectTransform _toolNameContent;
        [SerializeField, BoxGroup("Tool Name")] private CanvasGroup _toolNameCanvasGroup;
        [SerializeField, BoxGroup("Tool Name")] private TMP_Text _toolNameText;
        [SerializeField, BoxGroup("Tool Name")] private float _toolNameShowDuration = 3f, _toolNameHideDuration = 2f;

        private List<UIHUDToolbarIconController> _currentIcons;

        private Sequence _toolNameSequence = null;
        private float _toolNameAlpha;

        private void Awake()
        {
            CharacterToolsHandler.OnToolsUpdate += RedrawToolbar;
        }

        private void Update()
        {
            UpdateToolNameAlpha();
        }

        private void OnDestroy()
        {
            CharacterToolsHandler.OnToolsUpdate -= RedrawToolbar;
            TweenUtility.KillAndDestroySequence(ref _toolNameSequence);
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

            if (currentToolIndex < 0 || currentToolIndex >= tools.Length)
                return;

            if (tools[currentToolIndex] == null) return;

            StartToolNameDisplay(tools[currentToolIndex].ToolName);
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

            FinishToolNameDisplay();
        }

        private void UpdateToolNameAlpha()
        {
            if (_toolNameCanvasGroup == null) return;
            if (_toolNameContent == null) return;
            if (!_toolNameContent.gameObject.activeInHierarchy) return;

            _toolNameCanvasGroup.alpha = Mathf.Clamp01(_toolNameAlpha);
        }

        private void StartToolNameDisplay(string toolName)
        {
            FinishToolNameDisplay();

            float textWidth = 0f;
            if(_toolNameText != null)
            {
                _toolNameText.text = toolName;
                textWidth = _toolNameText.preferredWidth + 50f;
            }

            if(_toolNameContent != null)
            {
                _toolNameContent.gameObject.SetActive(true);

                Vector2 curSize = _toolNameContent.sizeDelta;
                _toolNameContent.sizeDelta = new(textWidth, curSize.y);
            }

            _toolNameAlpha = 1f;
            if(_toolNameCanvasGroup != null)
            {
                _toolNameCanvasGroup.alpha = _toolNameAlpha;
            }

            _toolNameSequence = DOTween.Sequence().AppendInterval(_toolNameShowDuration).
                Append(DOTween.To(x => _toolNameAlpha = x, 1f, 0f, _toolNameHideDuration)).
                AppendCallback(FinishToolNameDisplay);
        }

        private void FinishToolNameDisplay()
        {
            TweenUtility.KillAndDestroySequence(ref _toolNameSequence);

            _toolNameAlpha = 0f;
            if (_toolNameCanvasGroup != null)
                _toolNameCanvasGroup.alpha = 0f;

            if (_toolNameText != null)
                _toolNameText.text = "";

            if (_toolNameContent != null ) 
                _toolNameContent.gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            _toolNameShowDuration = Mathf.Max(0.1f, _toolNameShowDuration);
            _toolNameHideDuration = Mathf.Max(0.1f, _toolNameHideDuration);
        }
    }
}
