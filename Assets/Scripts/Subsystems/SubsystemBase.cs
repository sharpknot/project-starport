using NaughtyAttributes;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Subsystems
{
    [RequireComponent (typeof (NetworkObject))]
    public class SubsystemBase : NaughtyNetworkBehaviour
    {
        [field: SerializeField]
        public string SubsystemName { get; private set; }

        protected NetworkVariable<bool> IsLocallyActive = new(
            false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        protected NetworkVariable<float> Percent = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        public float CurrentPercent => Percent.Value;

        [SerializeField] private SubsystemBase[] _requiredSubsystems;

        private NetworkVariable<bool> _areAllSubsystemsActive = new(
            false, 
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        private NetworkVariable<bool> _isCurrentlyActive = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        [SerializeField, ReadOnly]
        private bool _debugIsCurrentlyActive, _debugIsLocallyActive;
        public bool IsCurrentlyActive => _isCurrentlyActive.Value;
        public bool IsCurrentlyLocallyActive => IsLocallyActive.Value;
        public event UnityAction<bool> OnCurrentlyActiveUpdate, OnLocallyActiveUpdate;
        [SerializeField, Foldout("Activation Events")]
        private UnityEvent OnActivated = new(), OnDeactivated = new();
        [SerializeField, Foldout("Local Activation Events")]
        private UnityEvent OnLocallyActivated = new(), OnLocallyDeactivated = new();

        public event UnityAction<float> OnPercentageUpdate;
        [SerializeField, Foldout("Percentage Events")]
        private UnityEvent<float> OnPercentageUpdateEvent;

        List<SubsystemBase> _validSubsystems;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Force Intial firing of events
            UpdateLocalActive(false, false);
            UpdateOverallActive(false, false);

            IsLocallyActive.OnValueChanged += UpdateLocalActive;
            _isCurrentlyActive.OnValueChanged += UpdateOverallActive;
            Percent.OnValueChanged += UpdateCurrentPercentage;

            StartCoroutine(InitializeAsServer());

        }

        protected virtual void Update()
        {
            _debugIsCurrentlyActive = IsCurrentlyActive;
            _debugIsLocallyActive = IsCurrentlyLocallyActive;
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();

            IsLocallyActive.OnValueChanged -= UpdateLocalActive;
            _isCurrentlyActive.OnValueChanged -= UpdateOverallActive;
            Percent.OnValueChanged -= UpdateCurrentPercentage;

            if (_validSubsystems != null)
            {
                foreach(var  subsystem in _validSubsystems)
                {
                    if(subsystem == null) continue;
                    subsystem.OnCurrentlyActiveUpdate -= SubsystemActivationUpdate;
                }
            }

            base.OnNetworkDespawn();
        }

        private void UpdateLocalActive(bool prev, bool current)
        {
            OnLocallyActiveUpdate?.Invoke(IsCurrentlyLocallyActive);
            if (IsCurrentlyLocallyActive) OnLocallyActivated?.Invoke();
            else OnLocallyDeactivated?.Invoke();

            if(IsServer)
                _isCurrentlyActive.Value = IsCurrentlyLocallyActive && _areAllSubsystemsActive.Value;
        }

        private void UpdateOverallActive(bool prev, bool current)
        {
            OnCurrentlyActiveUpdate?.Invoke(IsCurrentlyActive);
            if(IsCurrentlyActive) OnActivated?.Invoke();
            else OnDeactivated?.Invoke();
        }

        private void UpdateCurrentPercentage(float prev,  float current)
        {
            OnPercentageUpdate?.Invoke(CurrentPercent);
            OnPercentageUpdateEvent?.Invoke(CurrentPercent);
        }

        private IEnumerator InitializeAsServer()
        {
            if(!IsServer) yield break;

            _areAllSubsystemsActive.OnValueChanged += UpdateAllSubsystemActive;

            if (_requiredSubsystems == null)
            {
                _areAllSubsystemsActive.Value = true;
                yield break;
            }

            while (true)
            {
                bool allSpawned = true;
                foreach (var subsystem in _requiredSubsystems)
                {
                    if(subsystem == null) continue;
                    if (subsystem.NetworkObject.IsSpawned) continue;

                    allSpawned = false;
                    break;
                }

                if (allSpawned)
                    break;

                yield return null;
            };

            _validSubsystems = new();
            foreach(var subsystem in _requiredSubsystems)
            {
                if(subsystem == null) continue;
                if (_validSubsystems.Contains(subsystem))
                    continue;

                subsystem.OnCurrentlyActiveUpdate += SubsystemActivationUpdate;
                _validSubsystems.Add(subsystem);
            }

            _areAllSubsystemsActive.Value = AllSubsystemsActive();
        }

        private void SubsystemActivationUpdate(bool active)
        {
            _areAllSubsystemsActive.Value = AllSubsystemsActive();
        }

        private bool AllSubsystemsActive()
        {
            if (_validSubsystems == null) return true;
            foreach(var subsystem in _validSubsystems)
            {
                if(subsystem == null) continue;
                if(!subsystem.IsCurrentlyActive)
                    return false;
            }

            return true;
        }

        private void UpdateAllSubsystemActive(bool prev, bool current)
        {
            if (!IsServer) return;
            _isCurrentlyActive.Value = IsCurrentlyLocallyActive && _areAllSubsystemsActive.Value;
        }
    }
}
