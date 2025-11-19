using UnityEngine;

namespace Starport
{
    public class DescriptionController : MonoBehaviour
    {
        [field: SerializeField] public string Title { get; set; } = "Default Title";
        [field: SerializeField] public string Description { get; set; } = "Default Description";
        [field: SerializeField] public bool ShowDescription { get; set;  } = true;

        [SerializeField] private Transform _descriptionCenter;

        public override string ToString()
        {
            return $"Description controller ({gameObject.name}): Title-{Title}, Desc-{Description}, Show-{ShowDescription}";
        }

        public Vector3 GetCenterPos()
        {
            if (_descriptionCenter == null) return transform.position;
            return _descriptionCenter.position;
        }
    }
}
