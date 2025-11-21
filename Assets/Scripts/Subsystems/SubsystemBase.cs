using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Subsystems
{
    [RequireComponent (typeof (NetworkObject))]
    public class SubsystemBase : NaughtyNetworkBehaviour
    {
        protected NetworkVariable<bool> IsLocallyActive = new(
            false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Owner
            );

        [SerializeField] private SubsystemBase[] _requiredSubsystems;

        [field: SerializeField, ReadOnly]
        public bool IsCurrentlyActive { get; private set;  } = false;
        public bool IsCurrentlyLocallyActive => IsLocallyActive.Value;
        public event UnityAction<bool> OnCurrentlyActiveUpdate;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer) return;
            
            if(_requiredSubsystems != null)
            {
                foreach(var subsystem in _requiredSubsystems)
                {
                    if(subsystem == null) continue;
                    subsystem.OnCurrentlyActiveUpdate += RequiredSubsystemActivationUpdate;
                }
            }

            IsLocallyActive.OnValueChanged += OnIsLocallyActiveValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            if (_requiredSubsystems != null)
            {
                foreach (var subsystem in _requiredSubsystems)
                {
                    if (subsystem == null) continue;
                    subsystem.OnCurrentlyActiveUpdate -= RequiredSubsystemActivationUpdate;
                }
            }

            IsLocallyActive.OnValueChanged -= OnIsLocallyActiveValueChanged;
            base.OnNetworkDespawn();
        }

        private void OnIsLocallyActiveValueChanged(bool prev, bool current) => UpdateCurrentlyActiveFlag();

        private void RequiredSubsystemActivationUpdate(bool subsystemActive) => UpdateCurrentlyActiveFlag();

        private void UpdateCurrentlyActiveFlag()
        {
            if(_requiredSubsystems != null)
            {
                foreach(var subsystem in _requiredSubsystems)
                {
                    if(subsystem == null) continue;
                    if (subsystem.IsCurrentlyActive) continue;

                    // Inactive subsystem, fail the current
                    SetCurrentlyActive(false);
                    return;
                }
            }

            // Set currently active based on local active
            SetCurrentlyActive(IsLocallyActive.Value);
        }

        private void SetCurrentlyActive(bool currentlyActive)
        {
            if (IsCurrentlyActive == currentlyActive) return;

            IsCurrentlyActive = currentlyActive;
            OnCurrentlyActiveUpdate?.Invoke(IsCurrentlyActive);
        }
    }
}
