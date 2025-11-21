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
        [SerializeField] bool _randomizeInitialCapacity = true;
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

        protected override PickupController SpawnInitialPickup()
        {
            FluidCanister canister = GetValidCanister(DefaultPickup);
            if(canister == null) return null;

            GameObject g = Instantiate(canister.gameObject, transform.position, Quaternion.identity);
            PickupController p = g.GetComponent<PickupController>();
            p.NetworkObject.Spawn();

            if(_randomizeInitialCapacity)
            {
                FluidCanister c = p as FluidCanister;
                if (c != null)
                    c.SetCurrentCapacity(Random.Range(0f, 1f));
            }

            return p;
        }

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
    }
}
