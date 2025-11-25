using DG.Tweening;
using NaughtyAttributes;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport.Characters
{
    public class CharacterFixableController : MonoBehaviour
    {
        [SerializeField] private float _fixDistance = 3f;
        [SerializeField, Required] private Transform _originReference;
        [SerializeField, Required] private NetworkObject _parentNetworkObject;

        [SerializeField] private LayerMask _fixableLayer, _blockingLayer;

        [SerializeField] private Transform[] _nonDetectables;
        protected List<Transform> NonDetectables
        {
            get
            {
                if (_validNonDetectables == null)
                {
                    List<Transform> result = new();
                    if (_nonDetectables != null)
                    {
                        foreach (Transform t in _nonDetectables)
                        {
                            if (t == null) continue;
                            if (result.Contains(t)) continue;
                            result.Add(t);
                        }
                    }

                    _validNonDetectables = result;
                }

                return new(_validNonDetectables);
            }
        }
        private List<Transform> _validNonDetectables;

        [field: SerializeField, ReadOnly]
        public FixableController CurrentFixable { get; private set; } = null;
        public static event UnityAction<FixableController> OnCurrentFixableUpdate;

        private bool _allowFixing = false;

        public void SetAllowFixing(bool allow)
        {
            if(allow == _allowFixing) return;

            _allowFixing = allow;
            if (!_allowFixing)
                SetCurrentFixable(null);
        }

        public bool AttemptFix(float fixAmount)
        {
            if(CurrentFixable == null)
            {
                Debug.Log($"[CharacterFixableController] Fix attempt failed, no current fixable");
                return false; 
            }

            if(CurrentFixable.IsFixed)
            {
                Debug.Log($"[CharacterFixableController] {CurrentFixable.gameObject.name} is fully fixed!");
                return false;
            }

            Debug.Log($"[CharacterFixableController] Attempting to fix {CurrentFixable.gameObject.name} for {UIUtility.GetPercentage(fixAmount)}");
            CurrentFixable.AttemptFix(fixAmount);

            return true;
        }

        private void Update()
        {
            if (_originReference == null || _parentNetworkObject == null)
                return;
            if (!_parentNetworkObject.IsOwner) return;

            UpdateCurrentFixable();
        }

        private void SetCurrentFixable(FixableController fixable)
        {
            if (fixable == CurrentFixable) return;
            CurrentFixable = fixable;
            OnCurrentFixableUpdate?.Invoke(fixable);

            string desc = "Null current fixable";
            if (CurrentFixable != null)
                desc = $"{CurrentFixable.gameObject.name} ({UIUtility.GetPercentage(CurrentFixable.FixedAmount)})";

            Debug.Log($"[CharacterFixableController] Current fixable updated = {desc}");
        }

        private void UpdateCurrentFixable()
        {
            if (!_allowFixing || _fixDistance <= 0f)
            {
                SetCurrentFixable(null);
                return;
            }

            RaycastHit[] hits = new RaycastHit[128];
            int hitCount = Physics.RaycastNonAlloc(_originReference.position, _originReference.forward, hits, _fixDistance, _fixableLayer, QueryTriggerInteraction.Collide);

            FixableController closest = null;
            float closestDistance = _fixDistance;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                Transform t = hit.transform;
                if (t == null) continue;

                if (NonDetectables.Contains(t)) continue;
                FixableController d = t.GetComponent<FixableController>();
                if (d == null) continue;
                if (!d.IsFixable) continue;

                if (closest == null || closestDistance < hit.distance)
                {
                    closestDistance = hit.distance;
                    closest = d;
                }
            }

            if (closest == null)
            {
                SetCurrentFixable(null);
                return;
            }

            // Check for blocking
            hits = new RaycastHit[128];
            hitCount = Physics.RaycastNonAlloc(_originReference.position, _originReference.forward, hits, closestDistance, _blockingLayer, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                Transform t = hit.transform;
                if (t == null) continue;
                if (closest.transform == t) continue;
                if (NonDetectables.Contains(t)) continue;

                SetCurrentFixable(null);
                return;
            }

            SetCurrentFixable(closest);
        }
    }
}
