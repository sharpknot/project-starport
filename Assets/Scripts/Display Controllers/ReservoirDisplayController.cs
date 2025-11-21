using NaughtyAttributes;
using Starport.Subsystems;
using TMPro;
using UnityEngine;

namespace Starport.Display
{
    public class ReservoirDisplayController : MonoBehaviour
    {
        [SerializeField, Required]
        private FluidReservoirSubsystem _reservoir;

        [SerializeField] private RectTransform _localActivePanel, _localInactivePanel;
        [SerializeField] private TMP_Text _fluidName, _currentCapacity, _minCapacity;

        private void Update()
        {
            if(_reservoir == null)
            {
                SetLocalActive(false);
                UIUtility.SetText(_fluidName, "MISSING RESERVOIR");
                UIUtility.SetText(_currentCapacity, "Unkown current capacity");
                UIUtility.SetText(_minCapacity, "Unkown minimum capacity");
                return;
            }

            SetLocalActive(_reservoir.IsCurrentlyLocallyActive);

            string fluidName = "Unknown Fluid";
            if (_reservoir.FluidType != null)
                fluidName = _reservoir.FluidType.FluidName;
            UIUtility.SetText(_fluidName, fluidName);

            string curCap = $"Current capacity: {string.Format("{0:0.0%}", _reservoir.CurrentCapacity)}";
            string minCap = $"Minimum capacity: {string.Format("{0:0.0%}", _reservoir.MinCapacity)}";

            UIUtility.SetText(_currentCapacity, curCap);
            UIUtility.SetText(_minCapacity, minCap);
        }

        private void SetLocalActive(bool active)
        {
            UIUtility.ShowPanel(_localInactivePanel, !active);
            UIUtility.ShowPanel(_localActivePanel, active);
        }
    }
}
