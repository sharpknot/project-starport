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

        protected float CurrentFixAmount
        {
            get
            {
                if (!NetworkObject.IsSpawned) return 0f;
                return _currentFixAmount.Value;
            }
        }

        private NetworkVariable<float> _currentFixAmount = new(
            0f, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        protected event UnityAction<float> OnCurrentFixAmountUpdate;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            InitializeFixValues();
            SubscribeServerEvents();

            _currentFixAmount.OnValueChanged += OnFixAmountUpdate;
        }

        public override void OnNetworkDespawn()
        {
            _currentFixAmount.OnValueChanged -= OnFixAmountUpdate;
            UnsubscribeServerEvents();
            base.OnNetworkDespawn();
        }

        private void InitializeFixValues()
        {
            if (!IsServer) return;
            if(_fixable == null) return;

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
            _currentFixAmount.Value = fixedVal;

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

            if (amount == _currentFixAmount.Value) return;
            _currentFixAmount.Value = amount;
        }

        private void OnFixAmountUpdate(float prev, float current)
        {
            OnCurrentFixAmountUpdate?.Invoke(CurrentFixAmount);
        }
    }
}
