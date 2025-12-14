using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace Starport.Pickups
{
    public class FluidCanister : PickupController
    {
        [field: SerializeField] public Fluid FluidType { get; private set; }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            UpdateDescription();
            StateUpdate += FluidCapacityUpdated;
        }

        public override void OnNetworkDespawn()
        {
            StateUpdate -= FluidCapacityUpdated;
            base.OnNetworkDespawn();
        }

        private void FluidCapacityUpdated(PickupStateValues currentState)
        {
            UpdateDescription();
        }

        private void UpdateDescription()
        {
            string fluidName = "NULL FLUID";
            if (FluidType != null) fluidName = FluidType.FluidName;
            Description.Title = $"{fluidName} Canister";
            Description.Description = $"Canister containing {fluidName}.\nCapacity {string.Format("{0:0.0%}", Mathf.Clamp01(CurrentState.CapacityPercent))}";
        }
    }
}
