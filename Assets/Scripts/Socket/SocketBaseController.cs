using NaughtyAttributes;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using Starport.Pickups;

namespace Starport.Sockets
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public class SocketBaseController : NaughtyNetworkBehaviour
    {
        [SerializeField, Required] private TriggerHelper _socketArea;
        [field: SerializeField] public PickupController DefaultPickup { get; private set; }
        public event UnityAction<PickupController> OnSocketUpdate;

        [field: SerializeField, ReadOnly, BoxGroup("Current Socket Params")]
        public PickupController CurrentPickup { get; private set; } = null;
        [SerializeField, ReadOnly, BoxGroup("Current Socket Params")]
        private PickupController _previousPickup = null;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            InitializeSocket();
        }

        void Update()
        {
            UpdateCurrentPickup();
        }

        private void UpdateCurrentPickup()
        {
            if (!IsServer) return;

            if(CurrentPickup != null)
            {
                // Still locked in position
                if (!CurrentPickup.IsPickedUp(out ulong currentOwner)) return;

                // Remove current pickup
                CurrentPickup.NetworkObject.TryRemoveParent(true);
                _previousPickup = CurrentPickup;
                CurrentPickup = null;

                SocketEmptied();
                OnSocketUpdate?.Invoke(CurrentPickup);
            }

            if(_socketArea == null) return;

            // Check if the previous pickup object is still within the socket volume
            List<GameObject> pickupsInSocket = new(_socketArea.CurrentObjects);

            if (_previousPickup != null)
            {
                if (pickupsInSocket.Contains(_previousPickup.gameObject))
                {
                    // Previous pickup is still in the volume, remove it from consideration
                    pickupsInSocket.RemoveAll(g => g == _previousPickup.gameObject);
                }
                else
                {
                    // No longer in volume, can consider the previous pickup as history
                    _previousPickup = null;
                }
            }

            // Are there any pickups remaining to be considered?
            List<PickupController> potentialPickups = new();
            foreach (var obj in pickupsInSocket)
            {
                if(obj == null) continue;
                PickupController p = obj.GetComponent<PickupController>();
                if(p == null) continue;
                if (p.IsPickedUp(out _)) continue;
                if (potentialPickups.Contains(p)) continue;

                potentialPickups.Add(p);
            }

            if (potentialPickups.Count <= 0) return;

            PickupController validPickup = GetValidPickup(potentialPickups.ToArray());
            if (validPickup == null) return;

            // Valid pickup found! Socket it
            CurrentPickup = validPickup;
            CurrentPickup.Rigidbody.isKinematic = true;
            if (_socketArea != null)
            {
                CurrentPickup.transform.SetPositionAndRotation(_socketArea.transform.position, _socketArea.transform.rotation);
            }
            CurrentPickup.transform.SetParent(transform, true);

            SocketFilled();
            OnSocketUpdate?.Invoke(CurrentPickup);
        }

        private void InitializeSocket()
        {
            if (!IsServer) return;

            CurrentPickup = SpawnInitialPickup();

            if (CurrentPickup != null)
            {
                CurrentPickup.Rigidbody.isKinematic = true;
                if (_socketArea != null)
                {
                    CurrentPickup.transform.SetPositionAndRotation(_socketArea.transform.position, _socketArea.transform.rotation);
                }

                CurrentPickup.transform.SetParent(transform, true);
            }

            if (CurrentPickup != null) SocketFilled();
            else SocketEmptied();

            OnSocketUpdate?.Invoke(CurrentPickup);
        }

        protected virtual PickupController SpawnInitialPickup()
        {
            if(DefaultPickup == null) return null;

            GameObject g = Instantiate(DefaultPickup.gameObject, transform.position, Quaternion.identity);
            PickupController p = g.GetComponent<PickupController>();
            p.NetworkObject.Spawn();

            return p;
        }

        protected virtual PickupController GetValidPickup(PickupController[] potentialSocketables)
        {
            if (potentialSocketables == null) return null;
            foreach (var socketable in potentialSocketables)
            {
                if (socketable == null) continue;
                return socketable;
            }

            return null;
        }

        protected virtual void SocketEmptied() { }
        protected virtual void SocketFilled() { }
        
    }
}
