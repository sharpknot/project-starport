using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Pickups
{
    [RequireComponent (typeof(NetworkRigidbody), typeof(OwnershipController), typeof(DescriptionController))]
    [RequireComponent (typeof(NetworkObject))]
    public class PickupController : NaughtyNetworkBehaviour
    {
        protected OwnershipController OwnershipController
        {
            get
            {
                if(_ownershipController == null)
                    _ownershipController = GetComponent<OwnershipController>();
                return _ownershipController;
            }
        }
        private OwnershipController _ownershipController;

        protected DescriptionController Description
        {
            get
            {
                if(_description == null)
                    _description = GetComponent<DescriptionController>();
                return _description;
            }
        }
        private DescriptionController _description;

        private bool _isAttemptingPickup = false;

        public event UnityAction<bool> PickupAttemptResult;
        public PickupStateValues CurrentState => _state.Value;
        private NetworkVariable<PickupStateValues> _state = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
            );
        public event UnityAction<PickupStateValues> StateUpdate;

        public Rigidbody Rigidbody
        {
            get
            {
                if(_rigidBody == null)
                    _rigidBody = GetComponent<Rigidbody>();
                return _rigidBody;
            }
        }
        private Rigidbody _rigidBody;

        private NetworkVariable<bool> _canPickup = new(
            true, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _state.OnValueChanged += OnStateUpdated;
        }

        public override void OnNetworkDespawn()
        {
            _state.OnValueChanged -= OnStateUpdated;
            base.OnNetworkDespawn();
        }

        public bool IsPickedUp(out ulong pickerClientId) => OwnershipController.HasOwner(out pickerClientId);

        public void SetAllowPickup(bool allow)
        {
            if (!IsServer) return;
            if (_canPickup.Value == allow) return;
            _canPickup.Value = allow;
        }

        public bool PickupAllowed() => _canPickup.Value;

        public void AttemptPickup()
        {
            if (_isAttemptingPickup || IsPickedUp(out _) || !PickupAllowed())
            {
                PickupAttemptResult?.Invoke(false);
                return;
            }

            SubscribeOwnershipEvents();
            OwnershipController.RequestOwnership();
        }

        public void ReleasePickup()
        {
            Description.ShowDescription = true;
            OwnershipController.ResetOwnership();
        }

        public void ThrowPickup(Vector3 force)
        {
            Description.ShowDescription = true;
            OwnershipController.ResetOwnership();
            ThrowServerRpc(force);
        }

        public void SetState(PickupStateValues state)
        {
            if (!IsOwner) return;
            _state.Value = state;
        }

        private void OnStateUpdated(PickupStateValues prev, PickupStateValues current)
        {
            StateUpdate?.Invoke(CurrentState);
        }

        public override void OnDestroy()
        {
            UnsubscribeOwnershipEvents();
            base.OnDestroy();
        }

        protected virtual void Update()
        {

        }

        private void SubscribeOwnershipEvents()
        {
            UnsubscribeOwnershipEvents();

            if (OwnershipController == null)
                return;

            OwnershipController.OnOwnershipRequestSuccess += PickupSuccess;
            OwnershipController.OnOwnershipRequestFailure += PickupFailed;
        }

        private void UnsubscribeOwnershipEvents()
        {
            if (OwnershipController == null)
                return;

            OwnershipController.OnOwnershipRequestSuccess -= PickupSuccess;
            OwnershipController.OnOwnershipRequestFailure -= PickupFailed;
        }

        private void PickupSuccess()
        {
            _isAttemptingPickup = false;
            UnsubscribeOwnershipEvents();

            Description.ShowDescription = false;

            PickupAttemptResult?.Invoke(true);
        }

        private void PickupFailed()
        {
            _isAttemptingPickup = false;
            UnsubscribeOwnershipEvents();

            Description.ShowDescription = true;

            PickupAttemptResult?.Invoke(false);
        }

        [Rpc(SendTo.Server)]
        private void ThrowServerRpc(Vector3 force)
        {
            Rigidbody.AddForce(force, ForceMode.Impulse);
        }
    }

    public struct PickupStateValues : INetworkSerializable
    {
        public float CapacityPercent;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref CapacityPercent);
        }
    }
}
