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

        public override void InitializeSubSystem(float completionAmount = 1)
        {
            base.InitializeSubSystem(completionAmount);
            if (!IsServer) return;

            _fixable.FixedAmount = Mathf.Clamp01(completionAmount);
        }

        public override void Deinitialize()
        {
            if (IsServer)
            {
                _fixable.FixedAmount = 1f;
            }
            base.Deinitialize();
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

        private void InitializeFixValues()
        {
            if (!IsServer) return;
            if (_fixable == null)
            {
                IsLocallyActive.Value = true;
                return;
            }

            _fixable.IsFixable = true;
            _fixable.FixedAmount = 1f;
            Percent.Value = 1f;

            IsLocallyActive.Value = CurrentFixAmount >= 1f;

        }
    }
}
