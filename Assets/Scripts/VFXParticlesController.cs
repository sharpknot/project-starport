using DG.Tweening;
using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace Starport
{
    public class VFXParticlesController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem[] _particles;
        [SerializeField] private bool _isLoopingFX = false, _startOnAwake = true;
        [SerializeField, HideIf("_isLoopingFX")] private float _playDuration = 1f;
        [SerializeField] private float _destroyDuration = 0.5f;

        [SerializeField, ReadOnly] private float _particleMultiplier = 1f;
        [field: SerializeField, ReadOnly] public bool HasStarted { get; private set; } = false;
        public bool IsBeingDestroyed => _destroySequence != null;

        private Sequence _playSequence = null, _destroySequence = null;
        private Dictionary<ParticleSystem, float> _validParticles;

        void Start()
        {
            if (_startOnAwake)
                StartVFX();
        }

        void Update()
        {
            UpdateMultipliers();
        }

        private void OnDestroy()
        {
            KillAllSequences();
        }

        private void OnValidate()
        {
            _destroyDuration = Mathf.Max(0f, _destroyDuration);
            _playDuration = Mathf.Max(0f, _playDuration);
        }

        public void StartVFX()
        {
            if (HasStarted) return;

            HasStarted = true;
            _validParticles ??= GenerateValidParticles();

            KillAllSequences();

            _particleMultiplier = 1f;
            if (_isLoopingFX) StartLoopVFX();
            else StartNonLoopVFX();
        }

        public void StopVFX()
        {
            if (IsBeingDestroyed) return;

            KillAllSequences();

            if (_destroyDuration <= 0f)
            {
                CompleteStop();
                return;
            }

            _destroySequence = DOTween.Sequence().
                Append(DOTween.To(x => _particleMultiplier = x, 1f, 0f, _destroyDuration).SetEase(Ease.Linear)).
                AppendCallback(CompleteStop);
        }

        private void CompleteStop()
        {
            KillAllSequences();
            Destroy(gameObject);
        }

        private void StartLoopVFX()
        {
            
        }

        private void StartNonLoopVFX()
        {
            if(_playDuration <= 0f)
            {
                StopVFX();
                return;
            }

            _playSequence = DOTween.Sequence().AppendInterval(_playDuration).AppendCallback(StopVFX);
        }

        private void UpdateMultipliers()
        {
            _validParticles ??= GenerateValidParticles();

            foreach(var p in _validParticles.Keys)
            {
                if (p == null) continue;

                var curEm = p.emission;
                curEm.rateOverTime = _validParticles[p] * _particleMultiplier;
            }
        }

        private void KillSequence(ref Sequence sequence)
        {
            if (sequence == null) return;
            sequence.Kill();
            sequence = null;
        }

        private void KillAllSequences()
        {
            KillSequence(ref _playSequence);
            KillSequence(ref _destroySequence);
        }

        private Dictionary<ParticleSystem, float> GenerateValidParticles()
        {
            Dictionary<ParticleSystem, float> result = new();

            if (_particles == null) return result;

            foreach(var p in _particles)
            {
                if (p == null) continue;
                if (result.ContainsKey(p)) continue;

                var em = p.emission;
                result.Add(p, em.rateOverTimeMultiplier);
            }

            return result;
        }
    }
}
