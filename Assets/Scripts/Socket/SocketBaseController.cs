using NaughtyAttributes;
using Starport.Pickups;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Sockets
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public class SocketBaseController : NaughtyNetworkBehaviour
    {
        [SerializeField, Required] private NetworkTriggerHelper _socketArea;
        [field: SerializeField] protected PickupController DefaultPickup { get; private set; }
        public event UnityAction<PickupController> OnSocketUpdate;

        [field: SerializeField, ReadOnly, BoxGroup("Current Socket Params")]
        public PickupController CurrentPickup { get; private set; } = null;
        [SerializeField, ReadOnly, BoxGroup("Current Socket Params")]
        private PickupController _previousPickup = null;

        private NetworkVariable<NetworkObjectReference> _currentPickupObject = new(
            default, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _currentPickupObject.OnValueChanged += PickupObjectUpdated;

            if (!_currentPickupObject.Value.Equals(default))
            {
                PickupObjectUpdated(default, _currentPickupObject.Value);
            }
        }

        void Update()
        {
            UpdateCurrentPickup();
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();
            _currentPickupObject.OnValueChanged -= PickupObjectUpdated;
            
            if (CurrentPickup != null)
                CurrentPickup.StateUpdate -= PickupStateUpdate;

            base.OnNetworkDespawn();
        }

        private void UpdateCurrentPickup()
        {
            if (!IsServer) return;

            if(CurrentPickup != null)
            {
                // Still locked in position
                if (!CurrentPickup.IsPickedUp(out ulong currentOwner)) return;

                // Remove current pickup
                _currentPickupObject.Value = default;
            }

            if(_socketArea == null)
            {
                _currentPickupObject.Value = default;
                return;
            }

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

            if (potentialPickups.Count <= 0)
            {
                _currentPickupObject.Value = default;
                return; 
            }

            PickupController validPickup = GetValidPickup(potentialPickups.ToArray());
            if (validPickup == null || !validPickup.NetworkObject.IsSpawned)
            {
                _currentPickupObject.Value = default;
                return;
            }

            // Valid pickup found! Socket it
            _currentPickupObject.Value = new(validPickup.NetworkObject);
        }

        private void PickupObjectUpdated(NetworkObjectReference prev, NetworkObjectReference current)
        {
            StopPickupUpdateResolver();

            if (_currentPickupObject.Value.Equals(default))
            {
                RemoveCurrentPickup();
                OnSocketUpdate?.Invoke(CurrentPickup);
                return;
            }

            _pickupObjectResolver = ResolvePickupObjectUpdate(_currentPickupObject.Value);
            StartCoroutine(_pickupObjectResolver);
        }

        private IEnumerator _pickupObjectResolver = null;
        private bool _isResolvingPickupUpdate;

        private void StopPickupUpdateResolver()
        {
            if (!_isResolvingPickupUpdate) return;
            if (_pickupObjectResolver == null) return;

            StopCoroutine(_pickupObjectResolver);
            _pickupObjectResolver = null;
            _isResolvingPickupUpdate = false;
        }

        private IEnumerator ResolvePickupObjectUpdate(NetworkObjectReference reference)
        {
            _isResolvingPickupUpdate = true;
            float timeout = 5f;

            while (timeout > 0f)
            {
                if (reference.TryGet(out NetworkObject netObj))
                {
                    PickupController pickup = netObj.GetComponent<PickupController>();

                    if (pickup != null)
                    {
                        SetCurrentPickup(pickup);
                        OnSocketUpdate?.Invoke(CurrentPickup);
                        _isResolvingPickupUpdate = false;
                        yield break;
                    }

                    // Object exists but wrong type abort
                    break;
                }

                timeout -= Time.deltaTime;
                yield return null;
            }

            // Resolution failed
            if (IsServer)
                _currentPickupObject.Value = default;

            RemoveCurrentPickup();
            OnSocketUpdate?.Invoke(CurrentPickup);
            _isResolvingPickupUpdate = false;
        }

        private void RemoveCurrentPickup()
        {
            if(CurrentPickup ==null) return;
            CurrentPickup.StateUpdate -= PickupStateUpdate;

            _previousPickup = CurrentPickup;
            if (IsServer)
            {
                CurrentPickup.NetworkObject.TryRemoveParent(true);
                CurrentPickup.Rigidbody.isKinematic = true;
            }

            CurrentPickup = null;
        }

        private void SetCurrentPickup(PickupController pickup)
        {
            if (pickup == null) return;
            CurrentPickup = pickup;
            CurrentPickup.Rigidbody.isKinematic = true;
            CurrentPickup.StateUpdate += PickupStateUpdate;
            
            if (_socketArea == null || !IsServer)
                return;

            CurrentPickup.NetworkObject.TrySetParent(_socketArea.transform, false);
            CurrentPickup.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
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

        private bool _isSpawningPickup = false;
        public void SpawnPickupInSocket(PickupStateValues initialState)
        {
            if (!IsServer) return;

            StartCoroutine(SpawnPickupInSocketProcess(initialState));
        }

        private IEnumerator SpawnPickupInSocketProcess(PickupStateValues initialState)
        {
            if (_isSpawningPickup)
                yield break;

            RemoveCurrentPickup();

            if (_previousPickup != null)
            {
                _previousPickup.NetworkObject.Despawn(true);
                _previousPickup = null;
            }

            PickupController validPickup = GetValidDefaultPickable();
            if (validPickup == null || _socketArea == null || !_socketArea.NetworkObject.IsSpawned)
            {
                _currentPickupObject.Value = default;
                yield break;
            }

            _isSpawningPickup = true;

            GameObject g = Instantiate(validPickup.gameObject, _socketArea.transform.position, _socketArea.transform.rotation);
            PickupController p = g.GetComponent<PickupController>();
            p.Rigidbody.isKinematic = true;

            // Spawn network object
            p.NetworkObject.Spawn(true);

            while (!p.NetworkObject.IsSpawned)
                yield return null;

            p.NetworkObject.TrySetParent(_socketArea.NetworkObject);

            // Set initial pickup state
            p.SetState(initialState);
            
            _isSpawningPickup = false;

            // Update network variable
            _currentPickupObject.Value = new(p.NetworkObject);
        }

        public void ClearSocket()
        {
            if (!IsServer) return;

            RemoveCurrentPickup();
            if (_previousPickup != null)
            {
                _previousPickup.NetworkObject.Despawn(true);
                _previousPickup = null;
            }

            _currentPickupObject.Value = default;
        }

        private void PickupStateUpdate(PickupStateValues currentStateValues) => OnSocketUpdate?.Invoke(CurrentPickup);

        protected virtual PickupController GetValidDefaultPickable() => DefaultPickup;
        
    }
}
