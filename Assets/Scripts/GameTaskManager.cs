using NaughtyAttributes;
using Starport.Characters;
using Starport.Systems;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    [RequireComponent (typeof (NetworkObject))]
    public class GameTaskManager : NaughtyNetworkBehaviour
    {
        private NetworkVariable<double> _taskStartTime = new(
            0, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        private NetworkVariable<bool> _hasTask = new(
            false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server
            );

        public bool HasPlayerInWorkingArea => _hasPlayerInWorkingArea.Value;
        private NetworkVariable<bool> _hasPlayerInWorkingArea = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );
        public static event UnityAction<bool> OnPlayerInWorkingAreaUpdate;

        public bool TaskReadyToComplete => _isTaskReadyToComplete.Value;
        private NetworkVariable<bool> _isTaskReadyToComplete = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
            );
        public static event UnityAction<bool> OnTaskReadyToCompleteUpdate;

        [SerializeField]
        private CharacterVolumeDetector[] _workAreas;
        [SerializeField, Required]
        private NetworkObject _spawnPoint;
        [SerializeField, Required]
        private NetworkObject _hidePosition;

        [SerializeField, BoxGroup("Systems")]
        private SystemBase[] _systems;
        [SerializeField, ReadOnly, BoxGroup("Systems")]
        private SystemBase _currentSystem;

        public static event UnityAction OnTaskStarted;
        public static event UnityAction<double> OnTaskCompleted;

        private bool _isSpawningTask = false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            OnPlayerInWorkingAreaUpdate?.Invoke(HasPlayerInWorkingArea);
            OnTaskReadyToCompleteUpdate?.Invoke(TaskReadyToComplete);

            _taskStartTime.OnValueChanged += TaskStartTimeUpdate;
            _hasTask.OnValueChanged += HasTaskUpdate;
            _hasPlayerInWorkingArea.OnValueChanged += PlayerInWorkingAreaUpdate;
            _isTaskReadyToComplete.OnValueChanged += TaskReadyToCompleteUpdate;

            if (!IsServer) return;

            InitializeWorkingArea();
            StartCoroutine(InitializeTaskObjects());
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();
            DeinitializeWorkingArea();

            _taskStartTime.OnValueChanged -= TaskStartTimeUpdate;
            _hasTask.OnValueChanged -= HasTaskUpdate;
            _hasPlayerInWorkingArea.OnValueChanged -= PlayerInWorkingAreaUpdate;
            _isTaskReadyToComplete.OnValueChanged -= TaskReadyToCompleteUpdate;

            if (IsServer)
            {
                if(_currentSystem != null)
                    _currentSystem.OnSystemActiveUpdated -= OnCurrentSystemActiveUpdate;
            }

            base.OnNetworkDespawn();
        }

        public bool HasCurrentTask(out double elapsedTime)
        {
            elapsedTime = 0;
            if (!_hasTask.Value)
                return false;

            elapsedTime = NetworkManager.ServerTime.Time - _taskStartTime.Value;
            return true;
        }

        [Button("Spawn Task", EButtonEnableMode.Playmode)]
        public bool StartTask()
        {
            if (!IsServer) return false;
            if(HasCurrentTask(out _)) return false;

            StartCoroutine(SpawnTask());
            return true;
        }

        [Button("Despawn Task", EButtonEnableMode.Playmode)]
        public bool CompleteTask()
        {
            if(!IsServer) return false;
            if(!CanCompleteTask()) return false;

            DespawnTask();
            _hasTask.Value = false;

            Debug.Log($"[GameTaskManager] CompleteTask completed!");
            return true;
        }

        public bool CanCompleteTask()
        {
            if (!IsServer)
            {
                Debug.LogError($"[GameTaskManager] CanCompleteTask failed: Not the server!");
                return false; 
            }

            if (!HasCurrentTask(out _))
            {
                Debug.LogError($"[GameTaskManager] CanCompleteTask failed: No current task!");
                return false;
            }

            if (!TaskReadyToComplete)
            {
                Debug.LogError($"[GameTaskManager] CanCompleteTask failed: TaskReadyToComplete false!");
                return false;
            }

            if (HasPlayerInWorkingArea)
            {
                Debug.LogError($"[GameTaskManager] CanCompleteTask failed: HasPlayerInWorkingArea true!");
                return false;
            }

            return true;
        }

        private void TaskStartTimeUpdate(double prev, double current)
        {
            Debug.Log($"[GameTaskManager] TaskStartTimeUpdate {_taskStartTime.Value}");
        }

        private void HasTaskUpdate(bool prev, bool current)
        {
            Debug.Log($"[GameTaskManager] HasTaskUpdate {_hasTask.Value}");
            if (_hasTask.Value)
            {
                Debug.Log($"[GameTaskManager] Task started!");
                OnTaskStarted?.Invoke();
            }
            else
            {
                double elapsedTime = NetworkManager.ServerTime.Time - _taskStartTime.Value;
                Debug.Log($"[GameTaskManager] Task completed after {elapsedTime} seconds!");
                OnTaskCompleted?.Invoke(elapsedTime);
            }
        }

        private void PlayerInWorkingAreaUpdate(bool prev, bool current)
        {
            Debug.Log($"[GameTaskManager] PlayerInWorkingAreaUpdate {HasPlayerInWorkingArea}");
            OnPlayerInWorkingAreaUpdate?.Invoke(HasPlayerInWorkingArea);
        }

        private void TaskReadyToCompleteUpdate(bool prev, bool current)
        {
            Debug.Log($"[GameTaskManager] TaskReadyToCompleteUpdate {TaskReadyToComplete}");
            OnTaskReadyToCompleteUpdate?.Invoke(TaskReadyToComplete);
        }

        private IEnumerator SpawnTask()
        {
            if(_isSpawningTask)
                yield break;

            _currentSystem = GetRandomSystem();
            if (_currentSystem == null || _spawnPoint == null)
                yield break;

            _isSpawningTask = true;

            while(!_currentSystem.NetworkObject.IsSpawned)
                yield return null;

            _currentSystem.NetworkObject.TrySetParent(_spawnPoint, false);
            _currentSystem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            _currentSystem.InitializeSystem(0.2f);

            _isTaskReadyToComplete.Value = _currentSystem.IsSystemActive;
            _currentSystem.OnSystemActiveUpdated += OnCurrentSystemActiveUpdate;

            UpdatePlayerInWorkingArea();

            _hasTask.Value = true;
            _taskStartTime.Value = NetworkManager.ServerTime.Time;

            _isSpawningTask = false;

            Debug.Log($"[GameTaskManager] Task spawned");
        }

        private void DespawnTask()
        {
            DespawnNonTaskItems();

            if (!IsServer) return;

            _isTaskReadyToComplete.Value = false;
            if (_currentSystem == null) return;

            _currentSystem.OnSystemActiveUpdated -= OnCurrentSystemActiveUpdate;
            _currentSystem.Deinitialize();

            HideTaskObjects(_currentSystem);
            _currentSystem = null;
        }

        private void HideTaskObjects(SystemBase system)
        {
            if (system == null || _hidePosition == null) return;

            system.NetworkObject.TrySetParent(_hidePosition,false);
            system.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private IEnumerator InitializeTaskObjects()
        {
            if (_systems == null) yield break;

            while(true)
            {
                bool done = true;
                foreach(var s in _systems)
                {
                    if (s == null) continue;
                    if(!s.NetworkObject.IsSpawned)
                    {
                        done = false;
                        break;
                    }
                }

                if (done) break;

                yield return null;
            }

            foreach(var s in _systems)
            {
                HideTaskObjects(s);
            }
        }

        private void DespawnNonTaskItems()
        {
            if (!IsServer) return;

            if (_workAreas == null) return;
            foreach(var workArea in _workAreas)
            {
                TriggerHelper helper = workArea.GetComponent<TriggerHelper>();
                if (helper == null) continue;

                GameObject[] objs = helper.CurrentObjects;
                if (objs == null) continue;

                foreach(var obj in objs)
                {
                    if (obj == null) continue;

                    NetworkObject netObj = obj.GetComponent<NetworkObject>();
                    if (netObj == null) continue;

                    CharacterNetworkManager cm = netObj.GetComponent<CharacterNetworkManager>();
                    if (cm != null) continue;

                    if(_currentSystem != null)
                    {
                        // If the object is parented to the current system
                        SystemBase curSystem = netObj.GetComponentInParent<SystemBase>();
                        if (curSystem == _currentSystem) continue;
                    }

                    Debug.Log($"[GameTaskManager] DespawnNonTaskItems: Despawning {obj.name}...");
                    netObj.Despawn(true);
                }
            }
        }

        private SystemBase GetRandomSystem()
        {
            if(_systems == null) return null;

            List<SystemBase> validSystems = new();
            foreach(var system in _systems)
            {
                if (system == null) continue;
                if (validSystems.Contains(system)) continue;
                validSystems.Add(system);
            }

            if (validSystems.Count <= 0) return null;
            return validSystems[Random.Range(0, validSystems.Count)];
        }

        private void InitializeWorkingArea()
        {
            if (!IsServer) return;
            UpdatePlayerInWorkingArea();

            if (_workAreas == null) return;
            foreach (var area in _workAreas)
            {
                if (area == null) continue;
                area.OnCharacterDetectionUpdate += CharacterDetectionUpdate;
            }
        }

        private void DeinitializeWorkingArea()
        {
            if (!IsServer) return;
            if (_workAreas == null) return;
            foreach (var area in _workAreas)
            {
                if (area == null) continue;
                area.OnCharacterDetectionUpdate -= CharacterDetectionUpdate;
            }
        }

        private void CharacterDetectionUpdate(bool detected) => UpdatePlayerInWorkingArea();

        private void UpdatePlayerInWorkingArea()
        {
            if (_workAreas == null)
            {
                _hasPlayerInWorkingArea.Value = false;
                return;
            }

            foreach(var area in _workAreas)
            {
                if(area == null) continue;
                if (!area.HasDetectedCharacter) continue;

                _hasPlayerInWorkingArea.Value = true;
                return;  
            }

            _hasPlayerInWorkingArea.Value = false;
        }

        private void OnCurrentSystemActiveUpdate(bool isActive) => _isTaskReadyToComplete.Value = isActive;
    }
}
