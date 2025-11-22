using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    [RequireComponent (typeof (NetworkObject))]
    public class FixableController : NaughtyNetworkBehaviour
    {
        private NetworkVariable<bool> _isFixable = new(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );
        public bool IsFixable
        {
            get {  return _isFixable.Value; }
            set
            {
                if (!IsServer) return;
                if (_isFixable.Value == value) return;
                _isFixable.Value = value;
            }
        }

        private NetworkVariable<float> _fixedAmount = new(
            1f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );
        public float FixedAmount
        {
            get 
            { 
                if(!NetworkObject.IsSpawned) return 0f;
                return _fixedAmount.Value;
            }
            set
            {
                if (!IsServer) return;

                float amt = Mathf.Clamp01(value);
                if (_fixedAmount.Value == amt) return;
                _fixedAmount.Value = amt;
            }
        }
        public bool IsFixed => FixedAmount >= 1f;

        public event UnityAction<float, bool> OnFixAmountUpdate;
        public event UnityAction<bool> OnFixableUpdate;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _isFixable.OnValueChanged += FixableUpdated;
            _fixedAmount.OnValueChanged += FixAmountUpdated;
        }

        public override void OnNetworkDespawn()
        {
            _isFixable.OnValueChanged -= FixableUpdated;
            _fixedAmount.OnValueChanged -= FixAmountUpdated;

            base.OnNetworkDespawn();
        }

        public void AttemptFix(float amountToFix) => SetAmountToFixServerRpc(amountToFix);

        private void FixAmountUpdated(float prev, float current)
        {
            OnFixAmountUpdate?.Invoke(_fixedAmount.Value, _fixedAmount.Value >= 1f);
        }

        private void FixableUpdated(bool prev, bool  current) 
        {
            OnFixableUpdate?.Invoke(_isFixable.Value);
        }

        [Rpc(SendTo.Server)]
        private void SetAmountToFixServerRpc(float amountToFix)
        {
            float finalFixedAmount = Mathf.Clamp01(amountToFix +  _fixedAmount.Value);
            if (finalFixedAmount == _fixedAmount.Value) return;

            _fixedAmount.Value = finalFixedAmount;
        }
    }
}
