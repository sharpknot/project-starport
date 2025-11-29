using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Starport
{
    [RequireComponent(typeof(FixableController))]
    public class RepairEffectsController : NaughtyNetworkBehaviour
    {
        [SerializeField] private VFXParticlesController _vfx;
        private List<VFXParticlesController> _vfxList;

        private FixableController _fixable = null;
        private static readonly float _closestVfxDistance = 0.02f;
        private float _closestDistSq = 0f;

        private void Awake()
        {
            _closestDistSq = _closestVfxDistance * _closestVfxDistance;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _fixable = GetComponent<FixableController>();
            if (_fixable != null)
                _fixable.OnFixPointUpdate += FixPointUpdate;
        }

        public override void OnNetworkDespawn()
        {
            if (_fixable != null)
                _fixable.OnFixPointUpdate -= FixPointUpdate;

            base.OnNetworkDespawn();
        }

        private void FixPointUpdate(Vector3 point, Quaternion rotationToCharacter)
        {
            if (_vfx == null) return;

            _vfxList ??= new();
            _vfxList.RemoveAll(v => v == null || v.IsBeingDestroyed);

            // Check if any existing vfx is already there
            foreach (var vfx in _vfxList)
            {
                Vector3 vfxPos = vfx.transform.position;
                Vector3 dir = vfxPos - point;

                // Dist close
                if (dir.sqrMagnitude <= _closestDistSq)
                    return;
            }

            GameObject g = Instantiate(_vfx.gameObject, point, rotationToCharacter);
            VFXParticlesController v = g.GetComponent<VFXParticlesController>();
            v.StartVFX();

            _vfxList.Add(v);
        }
    }
}
