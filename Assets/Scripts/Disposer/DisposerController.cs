using DG.Tweening;
using NaughtyAttributes;
using Starport.Characters;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections;

namespace Starport.Disposer
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public class DisposerController : NetworkBehaviour
    {
        [SerializeField, Required] private InteractableController _interactable;
        [SerializeField, Required] private TriggerHelper _disposalVolume;

        public event UnityAction OnDisposingFailedCurrentlyDisposing;
        public event UnityAction OnStartDisposing;
        public event UnityAction<int> OnCompleteDisposing;

        [SerializeField] private float _disposalTime = 3f;
        private Sequence _disposalSequence = null;

        [SerializeField] private string _interactDescription = "Dispose items";

        private List<PickupController> _toDispose;

        private void OnValidate()
        {
            _disposalTime = Mathf.Max(0.1f, _disposalTime);
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && _interactable != null)
            {
                _interactable.SetDescription(_interactDescription);
                _interactable.OnInteractAttemptResultServer += Dispose;
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (_interactable != null)
                _interactable.OnInteractAttemptResultServer -= Dispose;

            KillDisposalSequence();

            base.OnNetworkDespawn();
        }

        private void Dispose(bool success, CharacterNetworkManager interactingCharacter)
        {
            Debug.Log($"[DisposerController] {gameObject.name} starting to dispose!");
            if (!success) return;
            if(_disposalSequence != null )
            {
                FailedDisposeCurrentlyDisposingClientRpc();
                return;
            }

            _toDispose = new(GetToDisposePickup());

            // Disable interactable
            if (_interactable != null)
                _interactable.SetInteractionAllowed(false);

            _disposalSequence = DOTween.Sequence().AppendInterval(_disposalTime).AppendCallback(CompleteDispose);
            StartDisposeClientRpc();
        }

        private void CompleteDispose()
        {
            KillDisposalSequence();

            int disposedCount = 0;
            if(_toDispose != null)
            {
                while (_toDispose.Count > 0)
                {
                    PickupController p = _toDispose[0];
                    _toDispose.RemoveAt(0);
                    if (!IsServer) continue;
                    if (p == null) continue;

                    p.NetworkObject.Despawn(true);

                    disposedCount++;
                }
            }

            // Reenable interactable
            if (_interactable != null)
                _interactable.SetInteractionAllowed(true);

            Debug.Log($"[DisposerController] {gameObject.name} server completed disposal of {disposedCount} items!");
            CompleteDisposeClientRpc(disposedCount);
        }

        private PickupController[] GetToDisposePickup()
        {
            List<PickupController> result = new();
            if (_disposalVolume == null)
            {
                Debug.LogError($"[DisposerController] {gameObject.name} missing disposal volume!");
                return result.ToArray();
            }

            if(_disposalVolume.CurrentObjects == null)
            {
                Debug.LogError($"[DisposerController] {gameObject.name} null current objects!");
                return result.ToArray();
            }

            foreach (GameObject g in _disposalVolume.CurrentObjects)
            {
                if(g == null) continue;
                PickupController p = g.GetComponent<PickupController>();    
                if(p == null) continue;

                // Someone is picking it up already
                if (p.IsPickedUp(out _)) continue;

                // Disable pickable
                p.SetAllowPickup(false);

                if(p.Rigidbody != null)
                    p.Rigidbody.isKinematic = true; // Disable dynamic motion/forces

                result.Add(p);
            }

            return result.ToArray();
        }


        [ClientRpc]
        private void FailedDisposeCurrentlyDisposingClientRpc()
        {
            Debug.LogError($"[DisposerController] {gameObject.name} is currently disposing!");
            OnDisposingFailedCurrentlyDisposing?.Invoke();
        }

        [ClientRpc]
        private void StartDisposeClientRpc()
        {
            Debug.Log($"[DisposerController] {gameObject.name} started disposal process!");
            OnStartDisposing?.Invoke();
        }

        [ClientRpc]
        private void CompleteDisposeClientRpc(int disposedCount)
        {
            Debug.Log($"[DisposerController] {gameObject.name} completed disposal ({disposedCount} items)!");
            OnCompleteDisposing?.Invoke(disposedCount);
        }

        private void KillDisposalSequence()
        {
            if (_disposalSequence == null) return;

            _disposalSequence.Kill();
            _disposalSequence = null;
        }
    }
}
