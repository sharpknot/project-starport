using NaughtyAttributes;
using Starport.Pickups;
using Starport.Sockets;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Subsystems
{
    public class FluidPipeSubsystem : FixableSubsystem
    {
        [SerializeField, Required] private Fluid _fluid;
        [SerializeField, Required] private DescriptionController _description;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (_description == null)
                return;

            string fluidName = "Unknown fluid";
            if (_fluid != null)
                fluidName = _fluid.FluidName;

            _description.Title = $"{fluidName} Pipe";
            UpdateDescription(CurrentFixAmount);

            OnCurrentFixAmountUpdate += UpdateDescription;
        }

        public override void OnNetworkDespawn()
        {
            OnCurrentFixAmountUpdate -= UpdateDescription;
            base.OnNetworkDespawn();
        }

        private void UpdateDescription(float currentAmount)
        {
            if (_description == null) return;

            string fluidName = "Unknown fluid";
            if (_fluid != null)
                fluidName = _fluid.FluidName;

            string status = "Status: Fixed";
            if (currentAmount < 1f)
                status = $"Status: Broken ({UIUtility.GetPercentage(currentAmount)})";

            string result = $"Pipe carrying {fluidName}.\n{status}";
            _description.Description = result;
        }

    }
}
