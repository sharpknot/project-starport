using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    [RequireComponent(typeof(NetworkRigidbody), typeof(OwnershipController))]
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

        [field: SerializeField]
        public string PickupName { get; set; }
        [field: SerializeField]
        public string PickupDescription { get; set; }

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
            OwnershipController.ResetOwnership();
        }

        public void ThrowPickup(Vector3 force)
        {
            OwnershipController.ResetOwnership();
            ThrowServerRpc(force);
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
            OnPickupAttemptResult?.Invoke(true);
        }

        private void PickupFailed()
        {
            _isAttemptingPickup = false;
            UnsubscribeOwnershipEvents();
            OnPickupAttemptResult?.Invoke(false);
        }

        [Rpc(SendTo.Server)]
        private void ThrowServerRpc(Vector3 force)
        {
            Rigidbody.AddForce(force, ForceMode.Impulse);
        }
    }
}
