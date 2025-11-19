using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    public class CharacterDescriptionDetectorController : MonoBehaviour
    {
        [SerializeField] private float _detectionDistance = 3f;
        [SerializeField, Required] private Transform _originReference;
        [SerializeField, Required] private NetworkObject _parentNetworkObject;

        [SerializeField] private LayerMask _detectionLayer, _blockingLayer;

        [SerializeField] private Transform[] _nonDetectables;
        protected List<Transform> NonDetectables
        {
            get
            {
                if(_validNonDetectables == null)
                {
                    List<Transform> result = new();
                    if(_nonDetectables != null)
                    {
                        foreach(Transform t in _nonDetectables)
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
        public DescriptionController CurrentDescriptionController { get; private set; } = null;
        private DescriptionController _previousDescriptionController = null;

        public static event UnityAction<DescriptionController> OnCurrentDescriptionControllerUpdate;
        
        private bool _allowDetection  = true;

        public void SetAllowDetection(bool allowDetection)
        {
            if(allowDetection == _allowDetection) return;

            _allowDetection = allowDetection;
            if (!_allowDetection)
                SetCurrentDescribable(null);
        }

        void Update()
        {
            if (_originReference == null || _parentNetworkObject == null)
                return;
            if (!_parentNetworkObject.IsOwner) return;

            UpdateCurrentDescriptionController();
        }

        private void SetCurrentDescribable(DescriptionController describable)
        {
            if (CurrentDescriptionController == describable) return;

            CurrentDescriptionController = describable;
            _previousDescriptionController = CurrentDescriptionController;
            OnCurrentDescriptionControllerUpdate?.Invoke(CurrentDescriptionController);

            string desc = "Null current description controller";
            if (CurrentDescriptionController != null)
                desc = CurrentDescriptionController.ToString();

            Debug.Log($"[CharacterDescriptionDetectorController] Current description controller updated = {desc}");
        }

        private void UpdateCurrentDescriptionController()
        {
            if(!_allowDetection || _detectionDistance <= 0f)
            {
                SetCurrentDescribable(null);
                return;
            }

            RaycastHit[] hits = new RaycastHit[128];
            int hitCount = Physics.RaycastNonAlloc(_originReference.position, _originReference.forward, hits, _detectionDistance, _detectionLayer, QueryTriggerInteraction.Collide);

            DescriptionController closest = null;
            float closestDistance = _detectionDistance;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = hits[i];
                Transform t = hit.transform;
                if (t == null) continue;

                if(NonDetectables.Contains(t)) continue;
                DescriptionController d = t.GetComponent<DescriptionController>();
                if(d == null) continue;
                if (!d.ShowDescription) continue;

                if(closest == null || closestDistance < hit.distance)
                {
                    closestDistance = hit.distance;
                    closest = d;
                }
            }

            if(closest == null)
            {
                SetCurrentDescribable(null);
                return;
            }

            // Check for blocking
            hits = new RaycastHit[128];
            hitCount = Physics.RaycastNonAlloc(_originReference.position, _originReference.forward, hits, closestDistance, _blockingLayer, QueryTriggerInteraction.Ignore);

            for(int i = 0;i < hitCount;i++)
            {
                RaycastHit hit = hits[i];
                Transform t = hit.transform;
                if (t == null) continue;
                if (closest.transform == t) continue;
                if (NonDetectables.Contains(t)) continue;

                SetCurrentDescribable(null);
                return;
            }

            SetCurrentDescribable(closest);
        }
    }
}
