using NaughtyAttributes;
using Starport.Pickups;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using static UnityEngine.Rendering.DebugUI;

namespace Starport.Sockets
{
    public class ReactorCoreSocketController : SocketBaseController
    {
        [SerializeField] private bool _randomizeInitialEnergy = true;

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

        protected override PickupController SpawnInitialPickup()
        {
            if(!IsServer) return null;
            if(DefaultPickup == null) return null;

            ReactorCore core = DefaultPickup as ReactorCore;
            if(core == null) return null;

            GameObject g = Instantiate(DefaultPickup.gameObject, transform.position, Quaternion.identity);
            ReactorCore p = g.GetComponent<ReactorCore>();
            p.NetworkObject.Spawn();

            float capacity = 1f;
            if (_randomizeInitialEnergy)
                capacity = Random.Range(0f, 1f);

            p.SetCurrentEnergy(capacity);           

            return p;
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
    }
}
