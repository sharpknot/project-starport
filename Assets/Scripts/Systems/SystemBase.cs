using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

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

        protected NetworkVariable<bool> ShowInitializableObjects = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        [SerializeField, Foldout("Activation Events")] private UnityEvent OnSystemActivated = new(), OnSystemDeactivated = new();
        [SerializeField] private List<GameObject> _hideableInitializedObjects;
        [SerializeField, ReadOnly] private bool _debugIsSystemActive;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Force fire trigger
            IsCurrentlyActiveUpdated(false, false);
            IsCurrentlyActive.OnValueChanged += IsCurrentlyActiveUpdated;

            ShowInitializableObjectsToggle(ShowInitializableObjects.Value);
            ShowInitializableObjects.OnValueChanged += ShowInitializableObjectsUpdate;

        }

        protected virtual void Update()
        {
            _debugIsSystemActive = IsSystemActive;
        }

        public override void OnNetworkDespawn()
        {
            ShowInitializableObjects.OnValueChanged -= ShowInitializableObjectsUpdate;
            IsCurrentlyActive.OnValueChanged -= IsCurrentlyActiveUpdated;
            base.OnNetworkDespawn();
        }

        public virtual void InitializeSystem(float completionAmount = 1f)
        {
            if(IsServer)
                ShowInitializableObjects.Value = true;
        }

        public virtual void Deinitialize()
        {
            if (IsServer)
                ShowInitializableObjects.Value = false;
        }

        protected virtual void OnValidate()
        {
            
            if (_hideableInitializedObjects != null)
            {
                for (int i = 0; i < _hideableInitializedObjects.Count; i++)
                {
                    GameObject obj = _hideableInitializedObjects[i];
                    if (obj == null) continue;
                    NetworkObject[] netObjs = obj.GetComponentsInChildren<NetworkObject>(true);
                    if (netObjs == null || netObjs.Length == 0) continue;

                    _hideableInitializedObjects[i] = null;
                }
            }
            
            
        }

        private void IsCurrentlyActiveUpdated(bool prev, bool current)
        {
            OnSystemActiveUpdated?.Invoke(IsSystemActive);
            if (IsSystemActive) OnSystemActivated?.Invoke();
            else OnSystemDeactivated?.Invoke();
        }

        protected virtual void ShowInitializableObjectsUpdate(bool prev, bool current)
        {
            ShowInitializableObjectsToggle(ShowInitializableObjects.Value);
        }

        private void ShowInitializableObjectsToggle(bool show)
        {
            if (_hideableInitializedObjects == null) return;
            foreach (var obj in _hideableInitializedObjects)
            {
                if(obj == null) continue;
                obj.SetActive(show);
            }
        }
    }
}
