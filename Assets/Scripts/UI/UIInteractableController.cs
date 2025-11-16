using Starport.Characters;
using TMPro;
using UnityEngine;

namespace Starport
{
    public class UIInteractableController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _interactableText;
        [SerializeField] private RectTransform _parentPanel;

        private InteractableController _currentInteractable;
        private static readonly string _prefix = "[E] ";


        private void Awake()
        {
            CharacterInteractableController.OnCurrentInteractableUpdate += UpdateCurrentInteractable;
        }

        void Update()
        {
            UpdateInteractableText();
        }

        private void OnDestroy()
        {
            CharacterInteractableController.OnCurrentInteractableUpdate -= UpdateCurrentInteractable;
        }

        private void UpdateCurrentInteractable(InteractableController currentInteractable) => _currentInteractable = currentInteractable;

        private void UpdateInteractableText()
        {
            if(_currentInteractable == null)
            {
                UIUtility.ShowPanel(_parentPanel, false);
                return;
            }

            UIUtility.ShowPanel(_parentPanel, true);
            UIUtility.SetText(_interactableText, _prefix + _currentInteractable.GetDescription());
        }

    }
}
