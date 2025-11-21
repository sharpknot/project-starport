using NaughtyAttributes;
using Starport.Pickups;
using Starport.Sockets;
using TMPro;
using UnityEngine;

namespace Starport.Display
{
    public class FluidSocketDisplayController : MonoBehaviour
    {
        [SerializeField, Required] private FluidSocketController _socket;
        [SerializeField, Required] private RectTransform _missingCanisterPanel, _hasCanisterPanel;
        [SerializeField, Required] private TMP_Text _fluidName, _fluidCapacity, _missingFluid;

        private void Update()
        {
            if (_socket == null)
            {
                UpdateNoCanister();
                return;
            }

            if (_socket.HasCanister(out float capacity))
            {
                UpdateHasCanister(capacity);
                return;
            }

            UpdateNoCanister();
        }

        private void UpdateHasCanister(float currentCapacity)
        {
            UIUtility.ShowPanel(_missingCanisterPanel, false);
            UIUtility.ShowPanel(_hasCanisterPanel, true);

            string fluidName = "Unknown fluid";
            if (_socket.FluidType != null)
                fluidName = _socket.FluidType.FluidName;

            UIUtility.SetText(_fluidName, fluidName);
            UIUtility.SetText(_fluidCapacity, string.Format("{0:0.0%}", currentCapacity));
        }

        private void UpdateNoCanister()
        {
            UIUtility.ShowPanel(_missingCanisterPanel, true);
            UIUtility.ShowPanel(_hasCanisterPanel, false);

            string fluidName = "unknown fluid";
            if (_socket != null && _socket.FluidType != null)
                fluidName = _socket.FluidType.FluidName;

            string warningText = $"Missing {fluidName} canister!";
            UIUtility.SetText(_missingFluid, warningText);
        }

    }
}
