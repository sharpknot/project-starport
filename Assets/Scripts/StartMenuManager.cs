using NaughtyAttributes;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starport
{
    public class StartMenuManager : MonoBehaviour
    {
        [SerializeField, Scene] private string _gameScene;
        [SerializeField, Scene] private string _preloaderScene;

        [field: SerializeField, ReadOnly] public string JoinCode { get; set; }

        private GameStateManager _stateManager;

        void Start()
        {
            // Make this the active scene
            SceneManager.SetActiveScene(gameObject.scene);

            _stateManager = GameStateManager.Instance;
            _stateManager.StopHostAttempt();
            _stateManager.StopJoinHostAttempt();
            _stateManager.StopOfflineAttempt();

            Cursor.visible = true;
            _stateManager.OnSceneFinishLoaded?.Invoke();
        }

        public void StartHost()
        {
            _stateManager.StartHostAttempt();
            _stateManager.SetNextScene(_gameScene);
            SceneManager.LoadSceneAsync(_preloaderScene, LoadSceneMode.Additive);
        }

        public void StartOffline()
        {
            _stateManager.StartOfflineAttempt();
            _stateManager.SetNextScene(_gameScene);
            SceneManager.LoadSceneAsync(_preloaderScene, LoadSceneMode.Additive);
        }

        public void JoinHost()
        {
            _stateManager.StartJoinHostAttempt(JoinCode);
            _stateManager.SetNextScene(_gameScene);
            SceneManager.LoadSceneAsync(_preloaderScene, LoadSceneMode.Additive);
        }

        public void Quit()
        {
            Application.Quit();
        }

    }
}
