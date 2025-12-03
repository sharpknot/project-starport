using NaughtyAttributes;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.Port;

namespace Starport.Pickups
{
    public class ReactorCore : PickupController
    {
        private NetworkVariable<float> _energy = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
            );

        [SerializeField] private Transform _coreTransform;
        [SerializeField] private float _coreRotationSpeed = 720f;
        [SerializeField] private Light _coreLight;
        [SerializeField] private ParticleSystem[] _particles;

        private Dictionary<ParticleSystem, float> _initialSpawnRate;
        public float GetCurrentEnergy() => _energy.Value;
        public void SetCurrentEnergy(float currentEnergy)
        {
            if (!IsOwner) return;

            float cap = Mathf.Clamp01(currentEnergy);
            _energy.Value = cap;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _energy.OnValueChanged += OnEnergyUpdate;

            _initialSpawnRate = GenerateInitialParticleValues();

            UpdateDescription();
            UpdateCoreLight();
            UpdateParticleEmissionSpeed();
        }

        public override void OnNetworkDespawn()
        {
            _energy.OnValueChanged -= OnEnergyUpdate;
            base.OnNetworkDespawn();
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            UpdateCoreRotation(deltaTime);
        }

        private void OnEnergyUpdate(float prev, float current)
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

            float curRot = _coreRotationSpeed * deltaTime * GetCurrentEnergy();
            Vector3 euler = new(0f, curRot, 0f);

            _coreTransform.Rotate(euler, Space.Self);
        }

        private void UpdateCoreLight()
        {
            if (!IsSpawned) return;
            if(_coreLight == null) return;

            _coreLight.intensity = GetCurrentEnergy();
        }

        private void UpdateParticleEmissionSpeed()
        {
            if (!IsSpawned) return;
            if (_initialSpawnRate == null) return;

            float curMult = GetCurrentEnergy();
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
            Description.Description = $"Reactor core for power generation.\nRemaining energy {string.Format("{0:0.0%}", GetCurrentEnergy())}";
        }
    }
}
