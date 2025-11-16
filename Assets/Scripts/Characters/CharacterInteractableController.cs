using DG.Tweening;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Characters
{
    public class CharacterInteractableController : MonoBehaviour
    {
        [SerializeField] private float _interactDistance = 3f;
        [SerializeField, Required] private Transform _interactOriginReference;
        [SerializeField, Required] private NetworkObject _parentNetworkObject;

        [SerializeField] private LayerMask _interactLayer, _blockingLayer;

        [field: SerializeField, ReadOnly]
        public InteractableController CurrentInteractable { get; private set; } = null;
        private InteractableController _previousInteractable = null;

        public static event UnityAction<InteractableController> OnCurrentInteractableUpdate;
        public static event UnityAction<InteractableController> OnInteractAttemptResult;

        private bool _allowInteract = true;

        private Sequence _interactAttempt = null;
        private static readonly float _interactAttemptDuration = 2f;
        private InteractableController _interactableAttempted;

        public void SetAllowInteract(bool allow)
        {
            if(allow == _allowInteract) return;

            _allowInteract = allow;
            if (!_allowInteract)
                SetCurrentInteractable(null);
        }

        private void Update()
        {
            if (_parentNetworkObject != null && !_parentNetworkObject.IsOwner)
                return;

            UpdateCurrentInteractable();
        }

        private void OnDestroy()
        {
            if (_interactableAttempted != null)
                _interactableAttempted.OnInteractAttemptResultClient -= InteractAttemptResult;

            KillInteractAttemptSequence();
        }

        private void OnValidate()
        {
            _interactDistance = Mathf.Max(0f, _interactDistance);
        }

        public void AttemptInteract(CharacterNetworkManager characterNetworkManager)
        {
            if (_parentNetworkObject != null && !_parentNetworkObject.IsOwner)
                return;

            if (CurrentInteractable == null || _interactAttempt != null || characterNetworkManager == null)
            {
                OnInteractAttemptResult?.Invoke(null);
                return;
            }

            _interactableAttempted = CurrentInteractable;
            _interactAttempt = DOTween.Sequence().AppendInterval(_interactAttemptDuration).AppendCallback(InteractAttemptFailed);

            if (_interactableAttempted != null)
                _interactableAttempted.OnInteractAttemptResultClient += InteractAttemptResult;
            _interactableAttempted.AttemptInteract(characterNetworkManager);

        }

        private void InteractAttemptResult(bool success)
        {
            if (_interactableAttempted != null)
                _interactableAttempted.OnInteractAttemptResultClient -= InteractAttemptResult;

            if(success) InteractAttemptSuccess();
            else InteractAttemptFailed();
        }

        private void InteractAttemptFailed()
        {
            KillInteractAttemptSequence();
            _interactableAttempted = null;
            OnInteractAttemptResult?.Invoke(null);
        }


        private void InteractAttemptSuccess()
        {
            KillInteractAttemptSequence();
            OnInteractAttemptResult?.Invoke(_interactableAttempted);
            _interactableAttempted = null;
        }


        private void UpdateCurrentInteractable()
        {
            if(_interactDistance <= 0f || _interactOriginReference == null || !_allowInteract)
            {
                SetCurrentInteractable(null);
                return;
            }

            RaycastHit[] hits = new RaycastHit[128];
            int hitCount = Physics.RaycastNonAlloc(_interactOriginReference.position, _interactOriginReference.forward, hits, _interactDistance, _interactLayer, QueryTriggerInteraction.Collide);

            InteractableController closestInteractable = null;
            float closestDistance = _interactDistance;

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                Transform t = hit.transform;
                if (t == null) continue;

                InteractableController interactable = t.GetComponent<InteractableController>();
                if(interactable == null) continue;
                if (!interactable.IsInteractionAllowed()) continue;

                if (closestInteractable == null || closestDistance < hit.distance)
                {
                    closestDistance = hit.distance;
                    closestInteractable = interactable;
                }
            }

            if (closestInteractable == null)
            {
                SetCurrentInteractable(null);
                return;
            }

            // Check for blocking
            hits = new RaycastHit[8];
            hitCount = Physics.RaycastNonAlloc(_interactOriginReference.position, _interactOriginReference.forward, hits, closestDistance, _blockingLayer, QueryTriggerInteraction.Ignore);

            if(hitCount > 0)
            {
                SetCurrentInteractable(null);
                return;
            }

            SetCurrentInteractable(closestInteractable);
        }

        private void SetCurrentInteractable(InteractableController interactable)
        {
            if (interactable == _previousInteractable) return;

            CurrentInteractable = interactable;
            _previousInteractable = CurrentInteractable;
            OnCurrentInteractableUpdate?.Invoke(CurrentInteractable);

            string interactableDesc = "Null interactable";
            if(CurrentInteractable != null)
            {
                interactableDesc = $"Interactable with desc: {CurrentInteractable.GetDescription()}";
            }

            Debug.Log($"[CharacterInteractableController] Current interactable updated = {interactableDesc}");
        }

        private void KillInteractAttemptSequence()
        {
            if(_interactAttempt == null) return;
            _interactAttempt.Kill();
            _interactAttempt = null;
        }
    }
}
