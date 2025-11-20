using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;

namespace Starport.Pickups
{
    public class ReactorCoolantCanister : PickupController
    {
        [SerializeField] private string _canisterName = "Reactor Coolant";
        
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
            Description.Title = _canisterName;
            Description.Description = $"Tank containing coolant fluid for reactor.\nCapacity {string.Format("{0:0.0%}", GetCurrentCapacity())}";
        }
    }
}
