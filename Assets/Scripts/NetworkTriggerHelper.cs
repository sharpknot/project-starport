using Unity.Netcode;
using UnityEngine;

namespace Starport
{
    [RequireComponent (typeof (NetworkObject))]
    public class NetworkTriggerHelper : TriggerHelper
    {
        public NetworkObject NetworkObject
        { 
            get
            {
                if (_netObj == null)
                    _netObj = GetComponent<NetworkObject>();
                return _netObj;
            }
        }
        private NetworkObject _netObj;
    }
}
