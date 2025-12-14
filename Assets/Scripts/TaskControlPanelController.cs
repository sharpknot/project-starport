using DG.Tweening;
using NaughtyAttributes;
using Starport.Characters;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Starport
{
    public class TaskControlPanelController : NaughtyNetworkBehaviour
    {
        [SerializeField, Required] private GameTaskManager _taskManager;
        [SerializeField, Required] private InteractableController _button;
        [SerializeField, Required, BoxGroup("UI")] private RectTransform _readyToStartPanel, _readyToSubmitPanel, _clearAreaPanel, _taskIncompletePanel;
        [SerializeField, Required] private TMP_Text _readyToStartText;

        private static readonly float _cooldownDuration = 3f;
        private Sequence _cooldownSequence;
        private float _cooldown;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            StartCoroutine(Initialize());
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();

            TweenUtility.KillAndDestroySequence(ref _cooldownSequence);

            GameTaskManager.OnTaskStarted -= TaskStarted;
            GameTaskManager.OnTaskCompleted -= TaskCompleted;
            GameTaskManager.OnPlayerInWorkingAreaUpdate -= WorkingAreaUpdated;
            GameTaskManager.OnTaskReadyToCompleteUpdate -= TaskReadyToSubmit;

            if(_button != null)
                _button.OnInteractAttemptResultServer += InteractPressed;

            base.OnNetworkDespawn();
        }

        void Update()
        {
            UpdateStartTaskText();
        }

        private IEnumerator Initialize()
        {
            if(_taskManager==null || _button==null)
            {
                Debug.LogError($"[TaskControlPanelController] _taskManager Initialize failed");
                yield break;
            }
            
            while(!_taskManager.NetworkObject.IsSpawned)
                yield return null;
            while (!_button.NetworkObject.IsSpawned)
                yield return null;

            GameTaskManager.OnTaskStarted += TaskStarted;
            GameTaskManager.OnTaskCompleted += TaskCompleted;
            GameTaskManager.OnPlayerInWorkingAreaUpdate += WorkingAreaUpdated;
            GameTaskManager.OnTaskReadyToCompleteUpdate += TaskReadyToSubmit;

            UpdateButtonStatus();
            UpdateDisplay();

            if(IsServer)
            {
                _button.OnInteractAttemptResultServer += InteractPressed;
            }

            Debug.Log($"[TaskControlPanelController] Initialize completed!");
        }

        private void WorkingAreaUpdated(bool hasPlayer)
        {
            UpdateButtonStatus();
            UpdateDisplay();
        }

        private void TaskStarted()
        {
            UpdateButtonStatus();
            UpdateDisplay();
        }

        private void TaskReadyToSubmit(bool ready)
        {
            UpdateButtonStatus();
            UpdateDisplay();
        }

        private void TaskCompleted(double elapsedTime)
        {
            UpdateButtonStatus();
            UpdateDisplay();

            TweenUtility.KillAndDestroySequence(ref _cooldownSequence);
            _cooldownSequence = DOTween.Sequence().
                Append(DOTween.To(x => _cooldown = x, _cooldownDuration, 0f, _cooldownDuration)).
                AppendCallback(FinishCooldown);
        }

        private void FinishCooldown()
        {
            TweenUtility.KillAndDestroySequence(ref _cooldownSequence);
        }

        private void UpdateStartTaskText()
        {
            if (_readyToStartText == null) return;

            string str = "Ready for new task";
            if(_cooldownSequence != null)
            {
                TimeSpan ts = TimeSpan.FromSeconds(_cooldown);
                string formatted = $"{ts.Seconds:00};{ts.Milliseconds:000}";
                str = $"New Task Cooldown: {formatted}";
            }

            _readyToStartText.text = str;
        }

        private void UpdateButtonStatus()
        {
            if (!IsServer) return;

            if (_button == null || !_button.NetworkObject.IsSpawned)
                return;
            if (_taskManager == null || !_taskManager.NetworkObject.IsSpawned)
                return;

            if(!_taskManager.HasCurrentTask(out _))
            {
                SetButtonState(_taskManager.CanStartNewTask(), "Start new task");

                Debug.Log($"[TaskControlPanelController] Button status updated: No current task! CanStartNewTask {_taskManager.CanStartNewTask()}");
                return;
            }

            if(!_taskManager.CanCompleteTask())
            {
                SetButtonState(false);

                Debug.Log($"[TaskControlPanelController] Button status updated: Cannot complete task yet");
                return;
            }

            SetButtonState(true, "Submit task");
            Debug.Log($"[TaskControlPanelController] Button status updated: Can complete task!");
        }

        private void SetButtonState(bool show, string buttonText ="Interact")
        {
            if (_button == null || !_button.NetworkObject.IsSpawned)
                return;

            if(!show)
            {
                _button.SetInteractionAllowed(false);
                return;
            }

            _button.SetDescription(buttonText);
            _button.SetInteractionAllowed(true);
        }

        private void InteractPressed(bool success, CharacterNetworkManager interactor)
        {
            if(!IsServer) return;
            if (!success) return;
            if (_taskManager == null || !_taskManager.NetworkObject.IsSpawned) return;

            if(_taskManager.HasCurrentTask(out _))
            {
                bool successSubmit = _taskManager.CompleteTask();
                Debug.Log($"[TaskControlPanelController] InteractPressed submitting task {successSubmit}");
                return;
            }

            if (_cooldownSequence != null) return;
            bool successStart = _taskManager.StartTask();
            Debug.Log($"[TaskControlPanelController] InteractPressed starting task {successStart}");
        }

        private void UpdateDisplay()
        {
            HideDisplay();

            if (_taskManager == null || !_taskManager.NetworkObject.IsSpawned)
                return;

            if (_taskManager.HasPlayerInWorkingArea)
                UIUtility.ShowPanel(_clearAreaPanel, true);

            if (!_taskManager.HasCurrentTask(out _))
            {
                if (_taskManager.CanStartNewTask())
                    UIUtility.ShowPanel(_readyToStartPanel, true);
                return;
            }

            if (_taskManager.TaskReadyToComplete)
                UIUtility.ShowPanel(_readyToSubmitPanel, true);
            else
                UIUtility.ShowPanel(_taskIncompletePanel, true);
        }

        private void HideDisplay()
        {
            UIUtility.ShowPanel(_readyToStartPanel, false);
            UIUtility.ShowPanel(_readyToSubmitPanel, false);
            UIUtility.ShowPanel(_clearAreaPanel, false);
            UIUtility.ShowPanel(_taskIncompletePanel, false);
        }
    }
}
