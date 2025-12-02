using NaughtyAttributes;
using Starport.Characters;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    public class OwnershipController : NaughtyNetworkBehaviour
    {
        private NetworkVariable<bool> _hasOwner = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Tracks the owner client ID
        private NetworkVariable<ulong> _currentOwner = new NetworkVariable<ulong>(
            NetworkManager.ServerClientId,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public event UnityAction OnOwnershipRequestSuccess;
        public UnityEvent OnOwnershipRequestSuccessEvent = new();

        public event UnityAction OnOwnershipRequestFailure;
        public UnityEvent OnOwnershipRequestFailureEvent = new();

        public event UnityAction OnOwnershipReset;
        public UnityEvent OnOwnershipResetEvent = new();

        public event UnityAction OnServerOwnershipReset, OnServerOwnershipRequestSuccess, OnServerOwnershipRequestFailure;
        public UnityEvent OnServerOwnershipResetEvent = new();
        public UnityEvent OnServerOwnershipRequestSuccessEvent = new();
        public UnityEvent OnServerOwnershipRequestFailureEvent = new();

        [SerializeField, ReadOnly]
        private bool _currentHasOwner = false;
        [SerializeField, ReadOnly]
        private ulong _currentOwnerId = NetworkManager.ServerClientId;

        public bool HasOwner(out ulong currentOwner)
        {
            currentOwner = _currentOwner.Value;
            return _hasOwner.Value;
        }

        public void RequestOwnership()
        {
            RequestOwnershipServerRpc(NetworkManager.LocalClientId);
        }

        public void ResetOwnership()
        {
            ResetOwnershipServerRpc(NetworkManager.LocalClientId);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }

        private void Update()
        {
            _currentHasOwner = _hasOwner.Value;
            _currentOwnerId = _currentOwner.Value;
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
        }

        private void HandleDisconnect(ulong disconnectedClientId)
        {
            if (!IsServer) return;

            ulong ownerId = _currentOwner.Value;

            if (!_hasOwner.Value) return;
            if (ownerId != disconnectedClientId) return;

            ResetOwnershipInternal();
        }

        [Rpc(SendTo.Server)]
        private void RequestOwnershipServerRpc(ulong requestingClientId)
        {
            // Already has an owner
            if (HasOwner(out ulong currentOwner) && currentOwner != requestingClientId)
            {
                OnServerOwnershipRequestFailure?.Invoke();
                OnServerOwnershipRequestFailureEvent?.Invoke();

                OwnershipRequestResultClientRpc(requestingClientId, false);
                return;
            }

            _hasOwner.Value = true;
            _currentOwner.Value = requestingClientId;
            SetOwnershipRecursive(NetworkObject, requestingClientId);

            OnServerOwnershipRequestSuccess?.Invoke();
            OnServerOwnershipRequestSuccessEvent?.Invoke();

            OwnershipRequestResultClientRpc(requestingClientId, true);
        }

        [Rpc(SendTo.Server)]
        private void ResetOwnershipServerRpc(ulong requestingClientId)
        {
            if (!HasOwner(out ulong currentOwner))
                return;

            if (currentOwner != requestingClientId)
                return;

            _hasOwner.Value = false;
            _currentOwner.Value = NetworkManager.ServerClientId;
            SetOwnershipRecursive(NetworkObject, NetworkManager.ServerClientId);

            Debug.Log($"[OwnershipController] {gameObject.name} ownership returned to {NetworkObject.OwnerClientId}");
            OnServerOwnershipReset?.Invoke();
            OnServerOwnershipResetEvent?.Invoke();
            OwnershipResetClientRpc();
        }

        [ClientRpc]
        private void OwnershipRequestResultClientRpc(ulong requesterId, bool success)
        {
            if (NetworkManager.LocalClientId != requesterId)
                return;

            Debug.Log($"[OwnershipController] {gameObject.name} ownership set to {NetworkObject.OwnerClientId}");

            if (success)
            {
                OnOwnershipRequestSuccess?.Invoke();
                OnOwnershipRequestSuccessEvent?.Invoke();
            }
            else
            {
                OnOwnershipRequestFailure?.Invoke();
                OnOwnershipRequestFailureEvent?.Invoke();
            }
        }

        [ClientRpc]
        private void OwnershipResetClientRpc()
        {
            OnOwnershipReset?.Invoke();
            OnOwnershipResetEvent?.Invoke();
        }

        private void ResetOwnershipInternal()
        {
            _hasOwner.Value = false;
            ulong originalOwner = _currentOwner.Value;
            _currentOwner.Value = NetworkManager.ServerClientId;
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);

            Debug.Log($"[OwnershipController] Original owner ({originalOwner}) disconnected! {gameObject.name} ownership returned to {NetworkObject.OwnerClientId}");

            OwnershipResetClientRpc();
        }

        private void SetOwnershipRecursive(NetworkObject root, ulong newOwner)
        {
            if (root == null) return;

            // skip characters (or any object that shouldn't be reset)
            if (root.TryGetComponent<CharacterNetworkManager>(out _)) return;

            root.ChangeOwnership(newOwner);

            foreach (Transform child in root.transform)
            {
                var childNetworkObject = child.GetComponent<NetworkObject>();
                if (childNetworkObject != null)
                {
                    SetOwnershipRecursive(childNetworkObject, newOwner);
                }
            }
        }
    }
}
