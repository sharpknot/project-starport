using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    [RequireComponent(typeof(NetworkRigidbody), typeof(OwnershipController), typeof(DescriptionController))]
    public class PickupController : NetworkBehaviour
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

        public event UnityAction<bool> OnPickupAttemptResult;

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
                OnPickupAttemptResult?.Invoke(false);
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

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            UnsubscribeOwnershipEvents();
            base.OnDestroy();
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

            OnPickupAttemptResult?.Invoke(true);
        }

        private void PickupFailed()
        {
            _isAttemptingPickup = false;
            UnsubscribeOwnershipEvents();

            Description.ShowDescription = true;

            OnPickupAttemptResult?.Invoke(false);
        }

        [Rpc(SendTo.Server)]
        private void ThrowServerRpc(Vector3 force)
        {
            Rigidbody.AddForce(force, ForceMode.Impulse);
        }
    }
}
