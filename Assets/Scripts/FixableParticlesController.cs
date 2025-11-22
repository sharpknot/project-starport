using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;

namespace Starport
{
    public class FixableParticlesController : MonoBehaviour
    {
        [SerializeField] private FixableController _fixable;
        [SerializeField] private ParticleSystem[] _particles;
        [SerializeField, ReadOnly] private float _multiplier;
        private ParticleSystem[] _validParticles = null;
        private Dictionary<ParticleSystem, float> _originalValues;
        void Start()
        {
            if (_fixable == null) return;
            if (_particles == null) return;

            List<ParticleSystem> result = new();
            foreach (ParticleSystem particle in _particles)
            {
                if(particle == null) continue;
                if(result.Contains(particle)) continue;

                result.Add(particle);
            }

            _validParticles = result.ToArray();

            _originalValues = new();
            foreach (ParticleSystem particle in _validParticles)
            {
                ParticleSystem.EmissionModule em = particle.emission;
                float orig = em.rateOverTimeMultiplier;
                _originalValues.Add(particle, orig);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(_fixable == null) return;
            if( _validParticles == null) return;

            float mult = 1f - _fixable.FixedAmount;
            mult = Mathf.Clamp01(mult);
            _multiplier = mult;
            foreach (ParticleSystem particle in _validParticles)
            {
                if(particle == null) continue;
                ParticleSystem.EmissionModule em = particle.emission;
                em.rateOverTimeMultiplier = mult * _originalValues[particle];
            }
        }
    }
}
