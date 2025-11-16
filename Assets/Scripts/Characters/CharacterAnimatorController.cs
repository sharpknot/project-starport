using NaughtyAttributes;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Starport.Characters
{
    public class CharacterAnimatorController : NetworkBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _characterController;
        [SerializeField] private NetworkAnimator _networkAnimator;

        [SerializeField, AnimatorParam("_animator", AnimatorControllerParameterType.Float)]
        private string _forwardVelocity, _rightVelocity;
        [SerializeField, AnimatorParam("_animator", AnimatorControllerParameterType.Trigger)]
        private string _baseResetTriggerParam, _locomotionResetTriggerParam;

        private readonly NetworkVariable<float> _currentForwardVelocity =
        new NetworkVariable<float>(0f, writePerm: NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<float> _currentRightVelocity =
            new NetworkVariable<float>(0f, writePerm: NetworkVariableWritePermission.Owner);

        private NetworkList<float> _layerWeights = new NetworkList<float>(writePerm: NetworkVariableWritePermission.Owner);
        private List<IEnumerator> _layerWeightChangeProcesses;

        #region Network Lifecycle
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                _layerWeights.Clear();
                for (int i = 0; i < _animator.layerCount; i++)
                    _layerWeights.Add(i == 0 ? 1f : 0f);
            }

            // Subscribe to changes (all clients)
            _layerWeights.OnListChanged += (_) => UpdateLayerWeights();

            // Initialize the coroutine list for blends
            _layerWeightChangeProcesses = InitializeLayerProcesses();
        }


        public override void OnNetworkDespawn()
        {
            _layerWeights?.Dispose();
            base.OnNetworkDespawn();
        }
        #endregion

        #region Unity Update
        private void Update()
        {
            UpdateForwardRightVelocities();
        }
        #endregion

        #region Velocity Sync
        private void UpdateForwardRightVelocities()
        {
            if (_animator == null) return;

            if (IsOwner)
            {
                if (_characterController != null)
                {
                    _currentForwardVelocity.Value = Vector3.Dot(transform.forward, _characterController.velocity);
                    _currentRightVelocity.Value = Vector3.Dot(transform.right, _characterController.velocity);
                }
                else
                {
                    _currentForwardVelocity.Value = 0f;
                    _currentRightVelocity.Value = 0f;
                }
            }

            // Apply to Animator
            _animator.SetFloat(_forwardVelocity, _currentForwardVelocity.Value);
            _animator.SetFloat(_rightVelocity, _currentRightVelocity.Value);
        }
        #endregion

        #region Booleans

        private void SetBoolean(string paramName, bool value)
        {
            if (_animator == null) return;
            _animator.SetBool(paramName, value);
        }
        #endregion

        #region Layer Weight Management
        public void SetLayerWeight(int layerIndex, float weight, float blendDuration = 0f)
        {
            if (!IsOwner) return;
            if (_layerWeights == null || _layerWeightChangeProcesses == null) return;
            if (!_layerWeights.CanClientRead(NetworkManager.LocalClientId)) return;
            if (layerIndex < 0 || layerIndex >= _layerWeights.Count)
            {
                Debug.LogError($"[AnimatorController] Invalid layerIndex ({layerIndex})");
                return;
            }

            // Stop any existing blend coroutine
            if (_layerWeightChangeProcesses[layerIndex] != null)
                StopCoroutine(_layerWeightChangeProcesses[layerIndex]);

            // Start new blend
            if (!gameObject.activeSelf) return;
            _layerWeightChangeProcesses[layerIndex] = LayerWeightChangeProcess(layerIndex, weight, blendDuration);
            StartCoroutine(_layerWeightChangeProcesses[layerIndex]);
        }

        private void UpdateLayerWeights()
        {
            if (_animator == null || _layerWeights == null) return;

            for (int i = 0; i < _layerWeights.Count; i++)
                _animator.SetLayerWeight(i, Mathf.Clamp01(_layerWeights[i]));
        }

        private IEnumerator LayerWeightChangeProcess(int layerIndex, float targetWeight, float blendDuration)
        {
            if (!IsOwner) yield break;
            if (_layerWeights == null) yield break;
            if (layerIndex < 0 || layerIndex >= _layerWeights.Count) yield break;

            float startWeight = _layerWeights[layerIndex];
            float finalWeight = Mathf.Clamp01(targetWeight);
            if (Mathf.Approximately(startWeight, finalWeight)) yield break;

            float duration = Mathf.Max(blendDuration, 0f);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                float pct = elapsed / duration;
                _layerWeights[layerIndex] = Mathf.Lerp(startWeight, finalWeight, pct);
            }

            _layerWeights[layerIndex] = finalWeight;
        }

        private List<IEnumerator> InitializeLayerProcesses()
        {
            var result = new List<IEnumerator>();
            if (_animator == null) return result;

            for (int i = 0; i < _animator.layerCount; i++)
                result.Add(null);

            return result;
        }

        #endregion

        #region Triggers

        public void SetBaseResetTrigger() => FireTrigger(_baseResetTriggerParam);
        public void SetLocomotionResetTrigger() => FireTrigger(_locomotionResetTriggerParam);

        private void FireTrigger(string triggerParamName)
        {
            if (string.IsNullOrEmpty(triggerParamName))
                return;

            // Only the owner should fire the ClientRpc
            if (!IsOwner) return;

            // Only if this NetworkObject is spawned
            if (!IsSpawned) return;

            FireTriggerClientRpc(triggerParamName);
        }

        [ClientRpc]
        private void FireTriggerClientRpc(string triggerParamName)
        {
            if(_animator == null) return;
            _animator.SetTrigger(triggerParamName);
        }

        #endregion
    }
}
