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
            UpdateDescription();

            OnCurrentFixAmountUpdate += UpdateFixAmount;
        }

        public override void OnNetworkDespawn()
        {
            OnCurrentFixAmountUpdate -= UpdateFixAmount;
            base.OnNetworkDespawn();
        }

        private void UpdateFixAmount(float currentAmount)
        {
            UpdateDescription();
        }

        private void UpdateDescription()
        {
            if (_description == null) return;

            string fluidName = "Unknown fluid";
            if (_fluid != null)
                fluidName = _fluid.FluidName;

            string status = "Status: Fixed";
            if (CurrentFixAmount < 1f)
                status = $"Status: Broken ({UIUtility.GetPercentage(CurrentFixAmount)})";

            string result = $"Pipe carrying {fluidName}.\n{status}";
            _description.Description = result;
            _description.Title = SubsystemName;
        }

    }
}
