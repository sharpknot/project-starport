using NaughtyAttributes;
using Starport.Pickups;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Sockets
{
    public class FluidSocketController : SocketBaseController
    {
        [field: SerializeField, Required] 
        public Fluid FluidType { get; private set; }

        public event UnityAction<FluidCanister, float> OnCanisterSocketUpdate;

        public bool HasCanister(out float capacity)
        {
            capacity = 0f;
            if (!_hasCanister.Value) return false;

            capacity = _capacity.Value;
            return true;
        }

        private NetworkVariable<bool> _hasCanister = new(
            false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        private NetworkVariable<float> _capacity = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        protected override PickupController GetValidPickup(PickupController[] potentialSocketables)
        {
            if (potentialSocketables == null) return null;
            foreach (var socketable in potentialSocketables)
            {
                FluidCanister canister = GetValidCanister(socketable);
                if (canister == null) continue;

                return socketable;
            }

            return null;
        }

        private FluidCanister GetValidCanister(PickupController potentialPickup)
        { 
            if(potentialPickup == null) return null;
            if(FluidType == null) return null;

            FluidCanister c = potentialPickup as FluidCanister;
            if(c == null) return null;
            if(c.FluidType != FluidType) return null;

            return c;
        }

        protected override void SocketEmptied()
        {
            base.SocketEmptied();
            
            if (!IsServer) return;

            _hasCanister.Value = false;
            _capacity.Value = 0f;

            OnCanisterSocketUpdate?.Invoke(null, 0f);
        }

        protected override void SocketFilled()
        {
            base.SocketFilled();

            if (!IsServer) return;
            FluidCanister c = GetValidCanister(CurrentPickup);
            if(c == null)
            {
                SocketEmptied();
                return;
            }

            float currentCapacity = c.GetCurrentCapacity();
            
            _hasCanister.Value = true;
            _capacity.Value = currentCapacity;

            OnCanisterSocketUpdate?.Invoke(c, currentCapacity);
        }

        public override void SpawnPickupInSocket(float percent)
        {
            base.SpawnPickupInSocket(percent);

            if (!IsServer) return;
            if (CurrentPickup != null)
                return;

            FluidCanister pc = SpawnFluidCanister();
            if(pc == null) return;

            pc.SetCurrentCapacity(Mathf.Clamp01(percent));
        }

        public override void ClearSocket()
        {
            base.ClearSocket();

            if (!IsServer) return;
            _hasCanister.Value = false;
            _capacity.Value = 0f;
            
        }

        private FluidCanister SpawnFluidCanister()
        {
            FluidCanister canister = GetValidCanister(DefaultPickup);
            if (canister == null) return null;

            GameObject g = Instantiate(canister.gameObject, transform.position, Quaternion.identity);
            FluidCanister p = g.GetComponent<FluidCanister>();
            p.NetworkObject.Spawn();

            return p;
        }
    }
}
