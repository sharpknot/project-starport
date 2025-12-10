using NaughtyAttributes;
using System.Collections.Generic;
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
        public static float AcceptableRangeFrequency = 5f;

        public event UnityAction<float> OnCurrentFrequencyUpdate;

        public float TargetFrequency => _targetFrequency.Value;
        public float CurrentFrequency => _currentFrequency.Value;
        public bool IsWithinTargetFrequency => Mathf.Abs(TargetFrequency - CurrentFrequency) <= AcceptableRangeFrequency;

        [SerializeField] private DescriptionController _description;
        [SerializeField] private InteractableController _interactable;

        public RenderTexture FrequencyRender;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _currentFrequency.OnValueChanged += CurrentFreqChange;
            
            if (IsServer)
            {
                
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

        public override void InitializeSubSystem(float completionAmount = 1)
        {
            base.InitializeSubSystem(completionAmount);
            if (!IsServer) return;

            _targetFrequency.Value = Random.Range(LowestFrequency, HighestFrequency);
            if(completionAmount >= 1f)
            {
                float final = Random.Range(TargetFrequency - AcceptableRangeFrequency, TargetFrequency + AcceptableRangeFrequency);
                final = Mathf.Clamp(final, LowestFrequency, HighestFrequency);
                _currentFrequency.Value = final;
                return;
            }

            List<float> potentials = new()
            {
                Random.Range(LowestFrequency, TargetFrequency - AcceptableRangeFrequency),
                Random.Range(TargetFrequency + AcceptableRangeFrequency, HighestFrequency)
            };

            potentials.RemoveAll(p => Mathf.Abs(p - TargetFrequency) <= AcceptableRangeFrequency);
            if(potentials.Count<= 0)
            {
                _currentFrequency.Value = Random.Range(LowestFrequency, HighestFrequency);
                return;
            }

            _currentFrequency.Value = potentials[Random.Range(0, potentials.Count)];
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
