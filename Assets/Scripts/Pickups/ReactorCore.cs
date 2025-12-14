using NaughtyAttributes;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Starport.Pickups
{
    public class ReactorCore : PickupController
    {
        [SerializeField] private Transform _coreTransform;
        [SerializeField] private float _coreRotationSpeed = 720f;
        [SerializeField] private Light _coreLight;
        [SerializeField] private ParticleSystem[] _particles;

        private Dictionary<ParticleSystem, float> _initialSpawnRate;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _initialSpawnRate = GenerateInitialParticleValues();

            EnergyUpdated(CurrentState);
            StateUpdate += EnergyUpdated;
        }

        public override void OnNetworkDespawn()
        {
            StateUpdate -= EnergyUpdated;
            base.OnNetworkDespawn();
        }

        protected override void Update()
        {
            base.Update();
            float deltaTime = Time.deltaTime;
            UpdateCoreRotation(deltaTime);
        }

        private void EnergyUpdated(PickupStateValues currentState)
        {
            UpdateDescription();
            UpdateCoreLight();
            UpdateParticleEmissionSpeed();
        }

        private void UpdateCoreRotation(float deltaTime)
        {
            if (!IsSpawned) return;
            if (_coreTransform == null) return;
            if (_coreRotationSpeed == 0f) return;

            float curRot = _coreRotationSpeed * deltaTime * Mathf.Clamp01(CurrentState.CapacityPercent);
            Vector3 euler = new(0f, curRot, 0f);

            _coreTransform.Rotate(euler, Space.Self);
        }

        private void UpdateCoreLight()
        {
            if (!IsSpawned) return;
            if(_coreLight == null) return;

            _coreLight.intensity = Mathf.Clamp01(CurrentState.CapacityPercent);
        }

        private void UpdateParticleEmissionSpeed()
        {
            if (!IsSpawned) return;
            if (_initialSpawnRate == null) return;

            float curMult = Mathf.Clamp01(CurrentState.CapacityPercent);
            foreach(var particle in _initialSpawnRate.Keys)
            {
                if(particle == null) continue;
                var em = particle.emission;

                em.rateOverTimeMultiplier = curMult * _initialSpawnRate[particle];
            }
        }

        private void OnValidate()
        {
            _coreRotationSpeed = Mathf.Max(0f, _coreRotationSpeed);
        }

        private Dictionary<ParticleSystem, float> GenerateInitialParticleValues()
        {
            Dictionary<ParticleSystem, float> result = new();
            if (_particles == null) return result;

            foreach (ParticleSystem particle in _particles)
            {
                if(particle ==null) continue;
                if (result.ContainsKey(particle)) continue;

                var em = particle.emission;
                result.Add(particle, em.rateOverTimeMultiplier);
            }

            return result;
        }

        private void UpdateDescription()
        {
            Description.Title = $"Reactor Core";
            Description.Description = $"Reactor core for power generation.\nRemaining energy {string.Format("{0:0.0%}", Mathf.Clamp01(CurrentState.CapacityPercent))}";
        }
    }
}
