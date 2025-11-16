using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starport.UI
{
    public class UISessionStartCover : MonoBehaviour
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private RectTransform _parentPanel;
        [SerializeField] private UISpinner _spinner;

        private void Awake()
        {
            UIEvents.ShowSessionStartCover += Show;
            UIEvents.HideSessionStartCover += Hide;
        }

        private void OnDestroy()
        {
            UIEvents.ShowSessionStartCover -= Show;
            UIEvents.HideSessionStartCover -= Hide;
        }

        private void Show(string message)
        {
            SetParentPanelActive(true);
            SetStatusText(message);
            ActivateSpinner(true);
        }

        private void Hide()
        {
            SetStatusText("");
            ActivateSpinner(false);
            SetParentPanelActive(false);
        }

        private void ActivateSpinner(bool active)
        {
            if (_spinner == null) return;

            if (active) _spinner.StartSpinning();
            else _spinner.StopSpinning();
        }

        private void SetStatusText(string text)
        {
            if (_statusText == null) return;
            _statusText.text = text;
        }

        private void SetParentPanelActive(bool active)
        {
            if (_parentPanel == null) return;
            _parentPanel.gameObject.SetActive(active);
        }

    }
}
