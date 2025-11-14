using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starport.UI
{
    public class UIDisconnectionWindow : MonoBehaviour
    {
        [SerializeField, Scene] private string _startMenu;
        [SerializeField, Scene] private string _preloadScene;

        [SerializeField] private RectTransform _parentPanel;
        [SerializeField] private TMP_Text _text;
        
        private bool _initialized = false;
        private bool _isLoadingToMainMenu = false;

        private void Awake()
        {
            UIEvents.ShowDisconnectWindow += Show;
        }

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            UIEvents.ShowDisconnectWindow -= Show;
        }

        private void Initialize()
        {
            if (_initialized) return;
            UIUtility.ShowPanel(_parentPanel, false);
            _initialized = true;
        }

        private void Show(string message)
        {
            UIEvents.ShowDisconnectWindow -= Show;
            UIUtility.ShowPanel(_parentPanel, true);
            UIUtility.SetText(_text, message);
            _initialized = true;
        }

        public void GoToStartMenu()
        {
            if(_isLoadingToMainMenu ) return;

            _isLoadingToMainMenu = true;

            GameStateManager stateManager = GameStateManager.Instance;
            stateManager.SetNextScene(_startMenu);

            SceneManager.LoadSceneAsync(_preloadScene, LoadSceneMode.Additive);
        }
    }
}
