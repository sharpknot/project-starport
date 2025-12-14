using NaughtyAttributes;
using Unity.Cinemachine;
using UnityEngine;

namespace Starport
{
    public class CameraManager : MonoBehaviour
    {
        public static CameraManager Instance { get; private set; }

        [field: SerializeField]
        public Camera MainCamera { get; private set; }
        [field: SerializeField]
        public CinemachineCamera InitialCamera { get; private set; }

        [field: SerializeField, ReadOnly]
        public CinemachineCamera CurrentCamera { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"Multiple CameraManagers found in scene. Destroying duplicate on {gameObject.name}");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PrioritizeCamera(InitialCamera);
        }

        protected virtual void OnDestroy()
        {
            // Clear the instance only if THIS is the active one
            if (Instance == this)
                Instance = null;
        }

        public void PrioritizeCamera(CinemachineCamera camera)
        {
            if (camera == null) return;   

            if(CurrentCamera != null)
            {
                CurrentCamera.Priority = 0;
            }

            camera.Priority = 10;
            CurrentCamera = camera;
        }
    }
}
