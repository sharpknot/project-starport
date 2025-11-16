using TMPro;
using UnityEngine;
using Starport.Characters;

namespace Starport
{
    public class UIPickableController : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText, _descriptionText;
        [SerializeField] private RectTransform _namePanel, _descriptionPanel;

        private PickupController _currentPickable;

        private void Awake()
        {
            CharacterPickupHandler.OnCurrentPickableUpdate += UpdateCurrentPickable;
        }

        private void Update()
        {
            UpdatePickableText();
        }

        private void OnDestroy()
        {
            CharacterPickupHandler.OnCurrentPickableUpdate -= UpdateCurrentPickable;
        }

        private void UpdateCurrentPickable(PickupController currentPickable)
        {
            _currentPickable = currentPickable;
        }

        private void UpdatePickableText()
        {
            if(_currentPickable == null)
            {
                UIUtility.ShowPanel(_namePanel, false);
                UIUtility.ShowPanel(_descriptionPanel, false);
                return;                    
            }

            UIUtility.ShowPanel(_namePanel, true);
            UIUtility.SetText(_nameText, _currentPickable.PickupName);
            UIUtility.ShowPanel(_descriptionPanel, true);
            UIUtility.SetText(_descriptionText, _currentPickable.PickupDescription);
                
        }
    }
}
