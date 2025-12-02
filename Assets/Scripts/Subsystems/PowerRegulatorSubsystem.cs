using NaughtyAttributes;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Subsystems
{
    [RequireComponent(typeof(NetworkObject))]
    public class PowerRegulatorSubsystem : SubsystemBase
    {
        private NetworkVariable<float> _targetFrequency = new
            (60f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        private NetworkVariable<float> _currentFrequency = new
            (60f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        public static float LowestFrequency = 60f, HighestFrequency = 240f;

        public event UnityAction<float> OnCurrentFrequencyUpdate;

        public float TargetFrequency => _targetFrequency.Value;
        public float CurrentFrequency => _currentFrequency.Value;
        public bool IsWithinTargetFrequency => Mathf.Abs(TargetFrequency - CurrentFrequency) <= 0.5f;

        [SerializeField] private DescriptionController _description;
        [SerializeField] private InteractableController _interactable;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _currentFrequency.OnValueChanged += CurrentFreqChange;
            
            if (IsServer)
            {
                _targetFrequency.Value = Random.Range(LowestFrequency, HighestFrequency);
                _currentFrequency.Value = Random.Range(LowestFrequency, HighestFrequency);

                IsLocallyActive.Value = IsWithinTargetFrequency;

                StartCoroutine(UpdateInteractable());
            }

            UpdateDescription();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
        }

        public void SetCurrentFrequency(float frequency)
        {
            if (!IsSpawned) return;

            SetCurrentFrequencyRpc(frequency);
        }

        [Rpc(SendTo.Server)]
        private void SetCurrentFrequencyRpc(float currentFreq)
        {
            Debug.Log($"[PowerRegulatorSubsystem] SetCurrentFrequencyRpc: {currentFreq}, _currentFrequency.Value {_currentFrequency.Value}");
            if (currentFreq == _currentFrequency.Value) return;
            _currentFrequency.Value = currentFreq;
        }

        private void CurrentFreqChange(float prev, float current)
        {
            if(IsLocallyActive.Value != IsWithinTargetFrequency && IsServer)
                IsLocallyActive.Value = IsWithinTargetFrequency;

            OnCurrentFrequencyUpdate?.Invoke(CurrentFrequency);
            UpdateDescription();
        }

        private void UpdateDescription()
        {
            if (_description == null) return;

            if(IsCurrentlyLocallyActive)
            {
                _description.Description = "Status: Active";
                return;
            }

            _description.Description = "Status: Inactive (Frequency out of sync)";
        }

        private IEnumerator UpdateInteractable()
        {
            if(_interactable == null || !IsServer) 
                yield break;

            while(!_interactable.IsSpawned)
                yield return null;

            _interactable.SetDescription("Adjust power synchronization");
        }
    }
}
