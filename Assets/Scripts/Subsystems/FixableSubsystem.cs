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
    public class FixableSubsystem : SubsystemBase
    {
        [SerializeField, BoxGroup("Fixable Params"), Required] 
        private FixableController _fixable;

        [SerializeField, BoxGroup("Fixable Params"), Range(0f, 1f)] 
        private float _fullyFixedChance = 0.5f;

        [SerializeField, BoxGroup("Fixable Params"), MinMaxSlider(0f, 0.99f)]
        private Vector2 _brokenRange = new(0f, 0.5f);

        [SerializeField, ReadOnly] private float _debugCurrentFixAmount;
        public float CurrentFixAmount => Percent.Value;


        public event UnityAction<float> OnCurrentFixAmountUpdate;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            StartCoroutine(ServerInitialization());

           OnPercentageUpdate += OnFixAmountUpdate;
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();

            OnPercentageUpdate -= OnFixAmountUpdate;
            UnsubscribeServerEvents();


            base.OnNetworkDespawn();
        }

        protected override void Update()
        {
            base.Update();
            _debugCurrentFixAmount = CurrentFixAmount;
        }

        private void InitializeFixValues()
        {
            if (!IsServer) return;
            if(_fixable == null)
            {
                IsLocallyActive.Value = true;
                return;
            }

            _fixable.IsFixable = true;

            bool fullyFixed = false;
            if(_fullyFixedChance >= 1f)
            {
                fullyFixed = true;
            }
            else if(_fullyFixedChance <= 0f)
            {
                fullyFixed = false;
            }
            else
            {
                fullyFixed = Random.Range(0f, 1f) <= _fullyFixedChance;
            }

            float fixedVal = 1f;

            if(!fullyFixed)
            {
                fixedVal = Random.Range(_brokenRange.x, _brokenRange.y);
            }

            _fixable.FixedAmount = fixedVal;
            Percent.Value = fixedVal;

            IsLocallyActive.Value = CurrentFixAmount >= 1f;

        }

        private void SubscribeServerEvents()
        {
            UnsubscribeServerEvents();
            if (!IsServer) return;
            if(_fixable ==null) return;
            _fixable.OnFixAmountUpdate += OnFixableCurrentFixAmountUpdate;
        }

        private void UnsubscribeServerEvents()
        {
            if (!IsServer) return;
            if (_fixable == null) return;
            _fixable.OnFixAmountUpdate -= OnFixableCurrentFixAmountUpdate;
        }

        private void OnFixableCurrentFixAmountUpdate(float amount, bool isFixed)
        {
            if (!IsServer) return;

            if (amount == Percent.Value) return;
            Percent.Value = amount;
        }

        private void OnFixAmountUpdate(float current)
        {
            OnCurrentFixAmountUpdate?.Invoke(CurrentFixAmount);

            if (IsServer)
                IsLocallyActive.Value = CurrentFixAmount >= 1f;
        }

        private IEnumerator ServerInitialization()
        {
            if(_fixable == null)
                yield break;

            while(!_fixable.NetworkObject.IsSpawned)
                yield return null;

            InitializeFixValues();
            SubscribeServerEvents();
        }
    }
}
