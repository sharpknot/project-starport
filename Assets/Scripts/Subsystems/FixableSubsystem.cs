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
                if (_fixable == null) return 0f;
                return _fixable.FixedAmount;
            }
        }
        protected event UnityAction<float> OnCurrentFixAmountUpdate;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            InitializeFixValues();
            if (_fixable != null)
                _fixable.OnFixAmountUpdate += OnFixAmountUpdate;
        }

        public override void OnNetworkDespawn()
        {
            if (_fixable != null)
                _fixable.OnFixAmountUpdate -= OnFixAmountUpdate;
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

            if(fullyFixed)
            {
                _fixable.FixedAmount = 1f;
                return;
            }

            _fixable.FixedAmount = Random.Range(_brokenRange.x, _brokenRange.y);
        }

        private void OnFixAmountUpdate(float amount, bool isFixed) => OnCurrentFixAmountUpdate?.Invoke(amount);

    }
}
