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


        public float MinCapacity => _minCapacity.Value;
        public float CurrentCapacity => CurrentPercent;
        
        [SerializeField, ReadOnly]
        private float _debugShowMinCapacity = 0f;

        private FluidSocketController[] _validSockets;

        public event UnityAction<float, bool> OnCapacityUpdated;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer) return;

            _validSockets = GetValidSockets();
            Percent.Value = GetCurrentCapacity();

            IsLocallyActive.Value = GetUpdatedLocallyActive(CurrentCapacity, _minCapacity.Value);
            OnCapacityUpdated?.Invoke(CurrentCapacity, IsCurrentlyLocallyActive);

            SubscribeEvents();
        }

        public override void OnNetworkDespawn()
        {
            UnsubscribeEvents();
            base.OnNetworkDespawn();
        }

        public override void InitializeSubSystem(float completionAmount = 1)
        {
            base.InitializeSubSystem(completionAmount);

            List<FluidSocketController> sockets = new(GetValidSockets());
            sockets.RemoveAll(s => s == null || !s.NetworkObject.IsSpawned);

            foreach (var socket in sockets)
            {
                if (!socket.NetworkObject.IsSpawned)
                    continue;
                socket.ClearSocket();
            }

            Percent.Value = 0f;
            _minCapacity.Value = GetMinCapacity();
            _debugShowMinCapacity = _minCapacity.Value;

            float targetNetAmount = Mathf.Clamp01(Random.Range(_minCapacity.Value, 1f)) * sockets.Count;
            if(completionAmount < 1f)
            {
                targetNetAmount = Mathf.Clamp01(Random.Range(0f, _minCapacity.Value * completionAmount)) * sockets.Count;
            }

            while (sockets.Count>0)
            {
                int index = Random.Range(0, sockets.Count);
                var socket = sockets[index];
                sockets.RemoveAt(index);

                if(targetNetAmount <= 0f)
                {
                    bool spawn = Random.Range(0f, 1f) > 0.5f;
                    if (spawn)
                        socket.SpawnPickupInSocket(0f);
                    continue;
                }

                float cap = Random.Range(0f, 1f);
                targetNetAmount -= cap;
                socket.SpawnPickupInSocket(cap);
            }
        }

        public override void Deinitialize()
        {
            base.Deinitialize();
            if (!IsServer) return;

            foreach(var socket in GetValidSockets())
            {
                if (!socket.NetworkObject.IsSpawned)
                    continue;
                socket.ClearSocket();
            }
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

            Percent.Value = curCapacity;
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