using NaughtyAttributes;
using Starport.Pickups;
using Starport.Sockets;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Subsystems
{

    public class FluidReservoirSubsystem : SubsystemBase
    {
        [field: SerializeField] public Fluid FluidType { get; private set; }

        [SerializeField] private FluidSocketController[] _fluidSockets;
        [SerializeField, MinMaxSlider(0, 10)]
        private Vector2Int _minCapacityRange = new(0, 10);

        private NetworkVariable<float> _minCapacity = new(
            0f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        private NetworkVariable<float> _currentCapacity = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        public float MinCapacity => _minCapacity.Value;
        public float CurrentCapacity => _currentCapacity.Value;
        
        [SerializeField, ReadOnly]
        private float _debugShowMinCapacity = 0f;

        private FluidSocketController[] _validSockets;

        public event UnityAction<float, bool> OnCapacityUpdated;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer) return;

            _minCapacity.Value = GetMinCapacity();
            _debugShowMinCapacity = _minCapacity.Value;

            _validSockets = GetValidSockets();
            _currentCapacity.Value = GetCurrentCapacity();

            IsLocallyActive.Value = GetUpdatedLocallyActive(_currentCapacity.Value, _minCapacity.Value);
            OnCapacityUpdated?.Invoke(CurrentCapacity, IsCurrentlyLocallyActive);

            SubscribeEvents();
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeEvents();
            base.OnNetworkDespawn();
        }

        private float GetMinCapacity()
        {
            return (float)Random.Range(_minCapacityRange.x, _minCapacityRange.y + 1) / 10f;
        }

        private void SubscribeEvents()
        {
            UnsubscribeEvents();
            if (_validSockets == null) return;

            foreach (var socket in _validSockets)
            {
                if (socket == null) continue;
                socket.OnCanisterSocketUpdate += UpdateCanisterValues;
            }
        }

        private void UnsubscribeEvents()
        {
            if(_validSockets == null) return;

            foreach(var socket in _validSockets)
            {
                if(socket == null) continue;
                socket.OnCanisterSocketUpdate -= UpdateCanisterValues;
            }
        }

        private void UpdateCanisterValues(FluidCanister canister, float capacity)
        {
            float curCapacity = GetCurrentCapacity();
            bool localActive = GetUpdatedLocallyActive(curCapacity, MinCapacity);

            // No changes
            if (CurrentCapacity == curCapacity && IsCurrentlyLocallyActive == localActive)
                return;

            _currentCapacity.Value = curCapacity;
            IsLocallyActive.Value = localActive;
            OnCapacityUpdated?.Invoke(curCapacity, localActive);

            Debug.Log($"[FluidReservoirSubsystem] {gameObject.name} update: IsLocallyActive {IsCurrentlyLocallyActive}, Current cap {CurrentCapacity}, Min cap {MinCapacity}");
        }

        private FluidSocketController[] GetValidSockets()
        {
            List<FluidSocketController> result = new();
            if (_fluidSockets == null) return result.ToArray();
            if (FluidType == null) return result.ToArray();

            foreach(var fs in _fluidSockets)
            {
                if (fs == null) continue;
                if (fs.FluidType != FluidType) continue;
                if (result.Contains(fs)) continue;
                result.Add(fs);
            }

            return result.ToArray();
        }

        private float GetCurrentCapacity()
        {
            if(_validSockets == null || _validSockets.Length <= 0) 
                return 0f;

            float capacity = 0f;
            foreach(var fs in _validSockets)
            {
                if (fs == null) continue;
                if(!fs.HasCanister(out float curCap)) continue;

                capacity += curCap;
            }

            return capacity / (float) _validSockets.Length;
        }

        private bool GetUpdatedLocallyActive(float currentCapacity, float minCapacity)
        {
            if (_validSockets == null || _validSockets.Length <= 0)
                return false;

            return currentCapacity >= minCapacity;
        }
    }
}