using NaughtyAttributes;
using Starport.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace Starport.UI
{
    public class UIOptionsMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform _parentPanel, _confirmPanelMenu;
        [SerializeField] private TMP_InputField _joinCodeField;
        [ReadOnly] public bool OpenedSubMenu = false;

        [SerializeField, BoxGroup("Quit"), Scene]
        private string _startMenuScene, _preloaderScene;

        private bool _initialized = false;
        private InputSystemUIInputModule _uiModule;

        private bool _isGoingToStartMenu = false;

        private void Awake()
        {
            UIEvents.ShowOptionsMenu += Show;    
        }

        private void Start()
        {
            if(!_initialized)
                HideWithoutNotification();
        }

        private void OnDestroy()
        {
            UIEvents.ShowOptionsMenu -= Show;
            UnsubscribeInputEvents();
        }

        private void Show()
        {
            if (_isGoingToStartMenu) return;

            UIEvents.ShowOptionsMenu -= Show;

            SubscribeInputEvents();
            ShowPanel(_confirmPanelMenu, false);
            ShowPanel(_parentPanel, true);
            OpenedSubMenu = false;
            _initialized = true;

            Cursor.visible = true;

            UpdateJoinCode();
        }

        public void Hide()
        {
            if (_isGoingToStartMenu) return;
            if (OpenedSubMenu)
                return;

            HideWithoutNotification();
            UIEvents.HiddenOptionsMenu?.Invoke();
        }

        public void ShowQuitConfirmationMenu(bool show) => ShowPanel(_confirmPanelMenu, show);

        public void QuitToStartMenu()
        {
            if (_isGoingToStartMenu) return;
            _isGoingToStartMenu = true;

            GameStateManager.Instance.SetNextScene(_startMenuScene);
            SceneManager.LoadSceneAsync(_preloaderScene, LoadSceneMode.Additive);
        }

        private void HideWithoutNotification()
        {
            UnsubscribeInputEvents();

            ShowPanel(_parentPanel, false);
            ShowPanel(_confirmPanelMenu, false);
            UIEvents.ShowOptionsMenu += Show;

            OpenedSubMenu = false;
            _initialized = true;
        }
        
        private void SubscribeInputEvents()
        {
            UnsubscribeInputEvents();

            if (EventSystem.current == null)
                return;

            _uiModule = EventSystem.current.GetComponent<InputSystemUIInputModule>();
            if (_uiModule == null)
                return;

            _uiModule.cancel.action.performed += CloseOptionsMenu;
        }

        private void UnsubscribeInputEvents()
        {
            if (_uiModule == null)
                return;

            _uiModule.cancel.action.performed -= CloseOptionsMenu;


            _uiModule = null;
        }

        private void CloseOptionsMenu(InputAction.CallbackContext ctx)
        {
            Debug.Log("CloseOptionsMenu");
            Hide();
        }

        private static void ShowPanel(RectTransform panel, bool show)
        {
            if (panel == null) return;
            panel.gameObject.SetActive(show);
            Debug.Log($"[UIOptionsMenu] ShowPanel {panel.gameObject.name} {show}");
        }

        private void UpdateJoinCode()
        {
            if (_joinCodeField == null) return;

            string text = "Offline";

            if (RelayManager.Instance.IsHosting(out string hostCode)) text = hostCode;
            if (RelayManager.Instance.IsClient(out string clientCode)) text = clientCode;

            _joinCodeField.text = text;
        }
    }
}
