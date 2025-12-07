using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Systems
{
    [RequireComponent(typeof(NetworkObject))]
    public class SystemBase : NaughtyNetworkBehaviour
    {
        protected NetworkVariable<bool> IsCurrentlyActive = new(
            false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        public bool IsSystemActive => IsCurrentlyActive.Value;
        public event UnityAction<bool> OnSystemActiveUpdated;

        [SerializeField, Foldout("Activation Events")] private UnityEvent OnSystemActivated = new(), OnSystemDeactivated = new();

        [SerializeField, ReadOnly] private bool _debugIsSystemActive;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Force fire trigger
            IsCurrentlyActiveUpdated(false, false);
            IsCurrentlyActive.OnValueChanged += IsCurrentlyActiveUpdated;

        }

        protected virtual void Update()
        {
            _debugIsSystemActive = IsSystemActive;
        }

        public override void OnNetworkDespawn()
        {
            IsCurrentlyActive.OnValueChanged -= IsCurrentlyActiveUpdated;
            base.OnNetworkDespawn();
        }

        private void IsCurrentlyActiveUpdated(bool prev, bool current)
        {
            OnSystemActiveUpdated?.Invoke(IsSystemActive);
            if (IsSystemActive) OnSystemActivated?.Invoke();
            else OnSystemDeactivated?.Invoke();
        }
    }
}
