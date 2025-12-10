using NaughtyAttributes;
using Starport.Characters;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    [RequireComponent (typeof (TriggerHelper))]
    public class CharacterVolumeDetector : MonoBehaviour
    {
        protected TriggerHelper Trigger
        {
            get
            {
                if (_trigger == null)
                    _trigger = GetComponent<TriggerHelper>();
                return _trigger;
            }
        }
        private TriggerHelper _trigger;

        [SerializeField] private UnityEvent CharacterDetected = new(), CharacterNotDetected = new();
        public UnityAction<bool> OnCharacterDetectionUpdate;

        [field: SerializeField, ReadOnly]
        public bool HasDetectedCharacter { get; private set; } = false;

        private void Awake()
        {
            Trigger.OnTriggerEnterEvent += OnEnter;
            Trigger.OnTriggerExitEvent += OnExit;
        }

        void Start()
        {
            UpdateDetection();

            OnCharacterDetectionUpdate?.Invoke(HasDetectedCharacter);
            if (HasDetectedCharacter) CharacterDetected.Invoke();
            else CharacterNotDetected.Invoke();
        }

        private void OnDestroy()
        {
            Trigger.OnTriggerEnterEvent -= OnEnter;
            Trigger.OnTriggerExitEvent -= OnExit;
        }

        private void OnEnter(GameObject g) => UpdateDetection();

        private void OnExit(GameObject g) => UpdateDetection();

        private void UpdateDetection()
        {
            GameObject[] objs = Trigger.CurrentObjects;
            if(objs == null || objs.Length == 0)
            {
                UpdateDetectionValue(false);
                return;
            }

            foreach(GameObject obj in objs)
            {
                if(obj == null) continue;
                
                CharacterNetworkManager cm = obj.GetComponent<CharacterNetworkManager>();
                if(cm == null) continue;

                UpdateDetectionValue(true);
                return;
            }

            UpdateDetectionValue(false);
        }

        private void UpdateDetectionValue(bool detected)
        {
            if (detected == HasDetectedCharacter)
                return;
            
            HasDetectedCharacter = detected;
            OnCharacterDetectionUpdate?.Invoke(HasDetectedCharacter);
            if (HasDetectedCharacter) CharacterDetected.Invoke();
            else CharacterNotDetected.Invoke();

            Debug.Log($"[CharacterVolumeDetector: {gameObject.name}] UpdateDetectionValue {HasDetectedCharacter}");
        }
    }
}
