using TMPro;
using UnityEngine;

namespace Starport
{
    public class UIDescriptionController : MonoBehaviour
    {
        [SerializeField] private RectTransform _indicatorLinePanel;
        [SerializeField] private RectTransform _titlePanel, _descriptionPanel;
        [SerializeField] private TMP_Text _titleText, _descriptionText;

        [SerializeField] private RectTransform _centerDot, _line;

        private DescriptionController _currentDescController = null;
        private static readonly float _verticalMargin = 10f;

        private Camera _camera;

        private void Awake()
        {
            CharacterDescriptionDetectorController.OnCurrentDescriptionControllerUpdate += UpdateCurrentDescription;
        }

        private void Update()
        {
            UpdateDescription();
        }

        private void OnDestroy()
        {
            CharacterDescriptionDetectorController.OnCurrentDescriptionControllerUpdate -= UpdateCurrentDescription;
        }

        private void UpdateCurrentDescription(DescriptionController descriptionController) => _currentDescController = descriptionController;
        private void UpdateDescription()
        {
            if(_currentDescController == null)
            {
                UIUtility.ShowPanel(_indicatorLinePanel, false);
                UIUtility.ShowPanel(_titlePanel, false);
                UIUtility.ShowPanel(_descriptionPanel, false);
                return;
            }

            UIUtility.ShowPanel(_indicatorLinePanel, true);
            UIUtility.ShowPanel(_titlePanel, true);
            UIUtility.ShowPanel(_descriptionPanel, true);

            UIUtility.SetText(_titleText, _currentDescController.Title);
            UIUtility.SetText(_descriptionText, _currentDescController.Description);

            SetPanelHeight(_titleText, _titlePanel, _verticalMargin);
            SetPanelHeight(_descriptionText, _descriptionPanel, _verticalMargin);

            UpdateCenterDotPosition();
            UpdateLine();
        }

        private void UpdateCenterDotPosition()
        {
            if (_centerDot == null) return;
            if (_camera == null) _camera = Camera.main;

            if(_currentDescController == null || _camera == null)
            {
                _centerDot.anchoredPosition = Vector2.zero;
                return;
            }

            Vector3 tgtPos = _currentDescController.GetCenterPos();
            Vector3 screenPos = _camera.WorldToScreenPoint(tgtPos);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _centerDot.parent as RectTransform,   // parent canvas space
                screenPos,
                null,                                // Overlay canvas uses NULL camera
                out Vector2 localPoint
            );

            _centerDot.anchoredPosition = localPoint;
        }

        private void UpdateLine()
        {
            if(_line == null || _centerDot == null) return;

            Vector2 from = _line.anchoredPosition;
            Vector2 to = _centerDot.anchoredPosition;

            Vector2 direction = to - from;

            // Rotation angle (UI uses Z rotation)
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Apply rotation
            _line.rotation = Quaternion.Euler(0, 0, angle);

            Vector2 curSize = _line.sizeDelta;
            _line.sizeDelta = new(direction.magnitude, curSize.y);
        }

        private void SetPanelHeight(TMP_Text text, RectTransform panel, float verticalMargin)
        {
            if (text == null || panel == null) return;

            float netHeight = Mathf.Max(0f, verticalMargin) + text.renderedHeight;
            Vector2 originalSize = panel.sizeDelta;
            panel.sizeDelta = new Vector2(originalSize.x, netHeight);
        }
    }
}
