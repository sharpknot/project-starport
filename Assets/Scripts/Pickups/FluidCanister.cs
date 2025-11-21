using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace Starport.Pickups
{
    public class FluidCanister : PickupController
    {
        [field: SerializeField] public Fluid FluidType { get; private set; }

        private NetworkVariable<float> _capacity = new(
            1f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Owner
            );

        public float GetCurrentCapacity() => _capacity.Value;
        public void SetCurrentCapacity(float currentCapacity)
        {
            if (!IsOwner) return;

            float cap = Mathf.Clamp01(currentCapacity);
            _capacity.Value = cap;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _capacity.OnValueChanged += OnCapacityUpdate;
            UpdateDescription();
        }

        public override void OnNetworkDespawn()
        {
            _capacity.OnValueChanged -= OnCapacityUpdate;
            base.OnNetworkDespawn();
        }

        private void OnCapacityUpdate(float prev, float current)
        {
            UpdateDescription();
        }

        private void UpdateDescription()
        {
            string fluidName = "NULL FLUID";
            if (FluidType != null) fluidName = FluidType.FluidName;
            Description.Title = $"{fluidName} Canister";
            Description.Description = $"Canister containing {fluidName}.\nCapacity {string.Format("{0:0.0%}", GetCurrentCapacity())}";
        }
    }
}
