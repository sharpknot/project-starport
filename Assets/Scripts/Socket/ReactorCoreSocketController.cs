using NaughtyAttributes;
using Starport.Pickups;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static UnityEditor.Experimental.GraphView.Port;
using static UnityEngine.Rendering.DebugUI;

namespace Starport.Sockets
{
    public class ReactorCoreSocketController : SocketBaseController
    {
        private NetworkVariable<float> _energy = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );

        public event UnityAction<float> OnCurrentEnergyUpdate;

        public float GetCurrentEnergy() => _energy.Value;
        
        private void SetCurrentEnergy(float value)
        {
            float curEnergy = Mathf.Clamp01(value);
            if (curEnergy == _energy.Value) return;

            _energy.Value = curEnergy;
            OnCurrentEnergyUpdate?.Invoke(_energy.Value);
        }

        protected override PickupController GetValidPickup(PickupController[] potentialSocketables)
        {
            if (potentialSocketables == null) return null;

            foreach(var socketable in potentialSocketables)
            {
                ReactorCore core = socketable as ReactorCore;
                if(core == null) return null;

                return core;
            }

            return null;
        }

        protected override void SocketFilled()
        {
            base.SocketFilled();

            if (!IsServer) return;

            if(CurrentPickup == null)
            {
                SocketEmptied();
                return;
            }

            ReactorCore p = CurrentPickup as ReactorCore;
            if(p == null)
            {
                SocketEmptied();
                return;
            }

            SetCurrentEnergy(p.GetCurrentEnergy());
        }

        protected override void SocketEmptied()
        {
            base.SocketEmptied();

            if (!IsServer) return;
            SetCurrentEnergy(0f);
        }

        public override void SpawnPickupInSocket(float percent)
        {
            base.SpawnPickupInSocket(percent);

            if (!IsServer) return;
            if (CurrentPickup != null)
                return;

            ReactorCore pc = SpawnReactorCore();
            if (pc == null) return;

            pc.SetCurrentEnergy(Mathf.Clamp01(percent));
            SetCurrentEnergy(percent);
        }

        public override void ClearSocket()
        {
            base.ClearSocket();

            if (!IsServer) return;
            _energy.Value = 0f;

        }

        private void Awake()
        {
            OnCurrentEnergyUpdate += UpdateEnergyValue;
        }

        public override void OnDestroy()
        {
            OnCurrentEnergyUpdate -= UpdateEnergyValue;
        }

        private void UpdateEnergyValue(float value)
        {
            Debug.Log($"[ReactorCoreSocketController] current energy value {value}");
        }

        private ReactorCore SpawnReactorCore()
        {
            ReactorCore canister = GetValidReactorCore(DefaultPickup);
            if (canister == null) return null;

            GameObject g = Instantiate(canister.gameObject, transform.position, Quaternion.identity);
            ReactorCore p = g.GetComponent<ReactorCore>();
            p.NetworkObject.Spawn();

            return p;
        }

        private ReactorCore GetValidReactorCore(PickupController potentialPickup)
        {
            if (potentialPickup == null) return null;

            ReactorCore c = potentialPickup as ReactorCore;
            if (c == null) return null;
            return c;
        }
    }
}
