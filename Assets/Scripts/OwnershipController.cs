using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

namespace Starport
{
    public class OwnershipController : NetworkBehaviour
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

        private void Awake()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }

        public override void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
            base.OnDestroy();
        }

        private void HandleDisconnect(ulong disconnectedClientId)
        {
            if (!IsServer) return;
            if (!HasOwner(out ulong currentOwner)) return;
            if (currentOwner != disconnectedClientId) return;

            ResetOwnershipInternal();
        }

        [Rpc(SendTo.Server)]
        private void RequestOwnershipServerRpc(ulong requestingClientId)
        {
            // Already has an owner
            if (HasOwner(out ulong currentOwner) && currentOwner != requestingClientId)
            {
                OwnershipRequestResultClientRpc(requestingClientId, false);
                return;
            }

            _hasOwner.Value = true;
            _currentOwner.Value = requestingClientId;
            NetworkObject.ChangeOwnership(requestingClientId);

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
            NetworkObject.RemoveOwnership();

            Debug.Log($"[OwnershipController] {gameObject.name} ownership returned to {NetworkObject.OwnerClientId}");

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
            _currentOwner.Value = NetworkManager.ServerClientId;
            NetworkObject.RemoveOwnership();
            OwnershipResetClientRpc();
        }
    }
}
