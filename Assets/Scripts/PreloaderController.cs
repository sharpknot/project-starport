using DG.Tweening;
using NaughtyAttributes;
using NUnit.Framework;
using Starport.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Starport
{
    public class PreloaderController : MonoBehaviour
    {
        [SerializeField, Scene] private string _defaultScene;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private UISpinner _spinner;
        [SerializeField] private float _blendInDuration = 0.5f, _blendOutDuration = 0.5f;

        private float _canvasAlpha = 1f;
        private Sequence _fadeSequence = null;
        private bool _isUnloading = false;
        private List<AsyncOperation> _loadOperations;
        private GameStateManager _stateManager;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            if (_spinner != null) _spinner.StartSpinning();

            _isUnloading = false;

            KillSequence();
            if (_blendInDuration <= 0f)
            {
                FinishBlendIn();
            }
            else
            {
                _fadeSequence = DOTween.Sequence()
                    .Append(DOTween.To(x => _canvasAlpha = x, 0, 1f, _blendInDuration)).
                    AppendCallback(FinishBlendIn);
            }
        }

        // Update is called once per frame
        void Update()
        {
            UpdateCanvasAlpha();
            UpdateUnloading();
        }

        private void OnDestroy()
        {
            if(_stateManager != null)
                _stateManager.OnSceneFinishLoaded -= StartBlendOut;
        }

        private void OnValidate()
        {
            _blendInDuration = Mathf.Max(0f, _blendInDuration);
            _blendOutDuration = Mathf.Max(0f, _blendOutDuration);
        }

        private void FinishBlendIn()
        {
            KillSequence();
            _canvasAlpha = 1f;
            UpdateCanvasAlpha();

            // Unload all existing scenes, except the preloader scene
            _loadOperations = new();
            for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (scene != gameObject.scene)
                {
                    _loadOperations.Add(SceneManager.UnloadSceneAsync(scene));
                }
            }

            _isUnloading = true;
            Debug.Log($"[PreloaderController] Unloading {_loadOperations.Count} scenes");
        }

        private void UnloadComplete()
        {
            Debug.Log($"[PreloaderController] Unloading complete!");
            _stateManager = GameStateManager.Instance;

            // No next scene, instantly load default scene
            if(!_stateManager.HasNextScene(out string nextScene))
            {
                Debug.LogError($"[PreloaderController] Unable to find the next scene from state manager, going to default scene");
                SceneManager.LoadSceneAsync(_defaultScene, LoadSceneMode.Single);
                return;
            }

            _stateManager.OnSceneFinishLoaded += StartBlendOut;

            Debug.Log($"[PreloaderController] Loading {nextScene}...");
            SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Additive);
        }

        private void StartBlendOut()
        {
            _stateManager.OnSceneFinishLoaded -= StartBlendOut;
            KillSequence();

            _canvasAlpha = 1f;
            UpdateCanvasAlpha();

            if (_blendOutDuration <= 0f)
            {
                FinishBlendOut();
            }
            else
            {
                _fadeSequence = DOTween.Sequence()
                    .Append(DOTween.To(x => _canvasAlpha = x, 1f, 0f, _blendOutDuration)).
                    AppendCallback(FinishBlendOut);
            }
        }

        private void FinishBlendOut()
        {
            KillSequence();
            _stateManager.ClearNextScene();
            _canvasAlpha = 0f;
            UpdateCanvasAlpha();

            // Unload this scene
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }

        private void UpdateUnloading()
        {
            if (!_isUnloading) return;

            _loadOperations ??= new();
            foreach (var operation in _loadOperations)
            {
                if (operation == null) continue;
                if (!operation.isDone)
                {
                    Debug.Log($"[PreloaderController] Unloading operation {operation} still not done!");
                    return;
                }
            }

            // All unload operations are done
            _isUnloading = false;
            UnloadComplete();
        }

        private void KillSequence()
        {
            if(_fadeSequence == null) return;
            _fadeSequence.Kill();
            _fadeSequence = null;
        }

        private void UpdateCanvasAlpha()
        {
            if (_canvasGroup == null) return;
            if (_canvasGroup.alpha == _canvasAlpha) return;

            _canvasGroup.alpha = _canvasAlpha;
        }
    }
}
