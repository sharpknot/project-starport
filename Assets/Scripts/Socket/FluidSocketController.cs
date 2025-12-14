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

        protected override PickupController GetValidDefaultPickable()
        {
            if(DefaultPickup == null) return null;

            if (FluidType == null) return null;

            FluidCanister c = DefaultPickup as FluidCanister;
            if (c == null) return null;
            if (c.FluidType != FluidType) return null;

            return c;
        }
    }
}
