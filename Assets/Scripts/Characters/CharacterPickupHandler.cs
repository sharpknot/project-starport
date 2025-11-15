using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Characters
{
    public class CharacterPickupHandler : MonoBehaviour
    {
        [SerializeField] private float _pickupDistance = 3f, _throwForce = 50f;
        [SerializeField, Required] private Transform _pickupOriginReference, _pickupHoldReference;

        [SerializeField] private LayerMask _pickupLayer, _blockingLayer;

        [field: SerializeField, ReadOnly]
        public PickupController CurrentPickup { get; private set; } = null;

        [field: SerializeField, ReadOnly]
        public PickupController CurrentPickable { get; private set; } = null;
        private PickupController _previousPickable = null;

        public event UnityAction<PickupController> OnCurrentPickableUpdate, OnCurrentPickupUpdate;
        public event UnityAction<PickupController> OnPickupAttemptResult;

        private PickupController _pickupToAttempt = null;
        private bool _hasCurrentPickup = false;
        private Sequence _pickupAttempt = null;
        private static readonly float _pickupAttemptDuration = 2f;

        private void Update()
        {
            UpdateCurrentPickable();
        }

        private void FixedUpdate()
        {
            UpdateCurrentPickupPosition();
        }

        private void OnDestroy()
        {
            KillPickupAttemptSequence();
            if (_pickupToAttempt != null)
                _pickupToAttempt.OnPickupAttemptResult -= PickupAttemptResult;
        }

        private void OnValidate()
        {
            _pickupDistance = Mathf.Max(0f, _pickupDistance);
            _throwForce = Mathf.Max(0f, _throwForce);
        }

        public void AttemptPickup()
        {
            // Nothing to pickup or currently is trying to pickup something or already picked up something
            if(CurrentPickable == null || _pickupAttempt != null || CurrentPickup != null)
            {
                OnPickupAttemptResult?.Invoke(CurrentPickup);
                return;
            }

            _hasCurrentPickup = false;
            _pickupToAttempt = CurrentPickable;
            _pickupToAttempt.OnPickupAttemptResult += PickupAttemptResult;
            _pickupAttempt = DOTween.Sequence().AppendInterval(_pickupAttemptDuration).AppendCallback(PickupAttemptFailed);
            _pickupToAttempt.AttemptPickup();
        }

        public void ThrowCurrentPickup()
        {
            _hasCurrentPickup = false;
            // Is currently attempting pickup or the current pickup is null/empty
            if (_pickupAttempt != null || CurrentPickup == null)
            {
                return;
            }

            CurrentPickup.ThrowPickup(_pickupOriginReference.forward * _throwForce);
            Debug.Log($"[CharacterPickupHandler] Pickup {CurrentPickup.gameObject.name} thrown!");

            SetCurrentPickup(null);
        }

        public void DropCurrentPickup()
        {
            _hasCurrentPickup = false;
            // Is currently attempting pickup or the current pickup is null/empty
            if (_pickupAttempt != null || CurrentPickup == null)
            {
                return;
            }

            CurrentPickup.ReleasePickup();
            Debug.Log($"[CharacterPickupHandler] Pickup {CurrentPickup.gameObject.name} dropped!");
            SetCurrentPickup(null);            
        }

        private void UpdateCurrentPickable()
        {
            // Is currently picking up || unable to calculate without origin reference || distance too small
            if(CurrentPickup != null || _pickupOriginReference == null || _pickupDistance <= 0f)
            {
                SetCurrentPickable(null);
                return;
            }

            // If currently trying to pickup something
            if(_pickupAttempt != null)
            {
                SetCurrentPickable(null);
                return;
            }

            // Get closest pickable object
            RaycastHit[] hits = new RaycastHit[256];
            int hitcount = Physics.RaycastNonAlloc(_pickupOriginReference.position, _pickupOriginReference.forward, hits, _pickupDistance, _pickupLayer, QueryTriggerInteraction.UseGlobal);

            PickupController currentPickable = null;
            float closestDistance = _pickupDistance;

            for (int i = 0; i < hitcount; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.transform == null) continue;

                PickupController p = hit.transform.GetComponent<PickupController>();
                if(p == null) continue;

                if (p.IsPickedUp(out _)) continue;
                
                if(currentPickable == null || hit.distance < closestDistance)
                {
                    currentPickable = p;
                    closestDistance = hit.distance;
                }
            }

            // No pickable found
            if(currentPickable == null)
            {
                SetCurrentPickable(null);
                return;
            } 

            // Validate if the pickable is actually blocked from view
            hits = new RaycastHit[16];
            hitcount = Physics.RaycastNonAlloc(_pickupOriginReference.position, _pickupOriginReference.forward, hits, closestDistance, _blockingLayer, QueryTriggerInteraction.Ignore);

            // Something is blocking
            if(hitcount > 0)
            {
                SetCurrentPickable(null);
                return;
            }

            SetCurrentPickable(currentPickable);
        }

        private void SetCurrentPickable(PickupController currentPickable)
        {
            CurrentPickable = currentPickable;
            if (CurrentPickable == _previousPickable)
                return;

            _previousPickable = CurrentPickable;
            OnCurrentPickableUpdate?.Invoke(CurrentPickable);

            string pickableName = "Null pickable";
            if (CurrentPickable != null) pickableName = CurrentPickable.gameObject.name;
            Debug.Log($"[CharacterPickupHandler] Current pickable updated: {pickableName}");
        }

        private void SetCurrentPickup(PickupController currentPickup)
        {
            if (currentPickup == CurrentPickup)
                return;
            CurrentPickup = currentPickup;
            OnCurrentPickupUpdate?.Invoke(CurrentPickup);

            string pickupName = "Null pickup";
            if (CurrentPickup != null) pickupName = CurrentPickup.gameObject.name;
            Debug.Log($"[CharacterPickupHandler] Current pickup updated: {pickupName}");
        }

        private void PickupAttemptResult(bool success)
        {
            if (_pickupToAttempt != null)
                _pickupToAttempt.OnPickupAttemptResult -= PickupAttemptResult;

            if (success) PickupAttemptSuccess();
            else PickupAttemptFailed();

            _pickupToAttempt = null;
        }

        private void PickupAttemptFailed()
        {
            KillPickupAttemptSequence();
            SetCurrentPickup(null);
            _pickupToAttempt = null;
            _hasCurrentPickup = false; 
        }

        private void PickupAttemptSuccess()
        {
            KillPickupAttemptSequence();
            SetCurrentPickup(_pickupToAttempt);
            _hasCurrentPickup = (CurrentPickup != null);
        }

        private void KillPickupAttemptSequence()
        {
            if(_pickupAttempt == null) return;
            _pickupAttempt.Kill();
            _pickupAttempt = null;
        }

        private void UpdateCurrentPickupPosition()
        {
            if(CurrentPickup == null) return;

            Vector3 holdPosition = transform.position + transform.forward + transform.up;
            Quaternion holdRotation = transform.rotation;
            if(_pickupHoldReference != null)
            {
                _pickupHoldReference.GetPositionAndRotation(out holdPosition, out holdRotation);
            }

            CurrentPickup.Rigidbody.MoveRotation(holdRotation);
            CurrentPickup.Rigidbody.angularVelocity = Vector3.zero;

            Vector3 toDestination = holdPosition - CurrentPickup.Rigidbody.transform.position;
            Vector3 force = toDestination / Time.fixedDeltaTime;

            CurrentPickup.Rigidbody.linearVelocity = Vector3.zero;
            CurrentPickup.Rigidbody.AddForce(force, ForceMode.VelocityChange);
        }
    }
}
