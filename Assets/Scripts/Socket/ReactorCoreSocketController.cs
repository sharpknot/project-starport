using NaughtyAttributes;
using Starport.Pickups;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Sockets
{
    public class ReactorCoreSocketController : SocketBaseController
    {
        protected override PickupController GetValidPickup(PickupController[] potentialSocketables)
        {
            if (potentialSocketables == null) return null;

            foreach(var socketable in potentialSocketables)
            {
                ReactorCore core = socketable as ReactorCore;
                if(core == null) return null;

                return core;
            }

            return null;
        }

    }
}
