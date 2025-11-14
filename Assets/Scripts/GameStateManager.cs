using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameStateManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameStateManager");
                        _instance = go.AddComponent<GameStateManager>();
                    }
                }
                return _instance;
            }
        }
        private static GameStateManager _instance;

        public UnityAction OnSceneFinishLoaded;

        private void Awake()
        {
            // If an instance already exists and it's not this, destroy the duplicate
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Assign and make persistent
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private string _targetJoinCode = string.Empty;
        public bool IsAttemptingToJoinHost(out string targetJoinCode)
        {
            targetJoinCode = _targetJoinCode;
            return !string.IsNullOrEmpty(targetJoinCode);
        }
        public void StopJoinHostAttempt() => _targetJoinCode = string.Empty;
        public void StartJoinHostAttempt(string  targetJoinCode) => _targetJoinCode = targetJoinCode;

        private bool _isAttemptingToHost = false;
        public bool IsAttemptingToHost() => _isAttemptingToHost;
        public void StartHostAttempt() => _isAttemptingToHost = true;
        public void StopHostAttempt() => _isAttemptingToHost = false;

        private bool _isOffline;
        public bool IsAttemptingOffline() => _isOffline;
        public void StartOfflineAttempt() => _isOffline = true;
        public void StopOfflineAttempt() => _isOffline = false; 

        private string _nextScene = string.Empty;
        public bool HasNextScene(out string nextScene)
        {
            nextScene = _nextScene;
            return !string.IsNullOrEmpty(nextScene);
        }
        public void ClearNextScene() => _nextScene = string.Empty;
        public void SetNextScene(string nextScene) => _nextScene = nextScene;
    }
}
