using NaughtyAttributes;
using UnityEngine;

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

    }
}
