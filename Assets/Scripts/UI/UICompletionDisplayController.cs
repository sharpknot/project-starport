using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starport
{
    [RequireComponent(typeof(Animator))]
    public class UICompletionDisplayController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField, AnimatorParam("_animator", AnimatorControllerParameterType.Trigger)]
        private string _completeParam, _startedParam;
        [SerializeField] private Image _completeBackground;
        [SerializeField] private TMP_Text _completeText, _completeTimeText;

        [SerializeField] private Color _gradeAColor, _gradeBColor, _gradeCColor;

        private readonly double _aMax = 120, _bMax = 240;
        private bool _started = false;
        private void Awake()
        {
            GameTaskManager.OnTaskStarted += TaskStart;
            GameTaskManager.OnTaskCompleted += TaskComplete;
        }

        private void OnDestroy()
        {
            GameTaskManager.OnTaskStarted -= TaskStart;
            GameTaskManager.OnTaskCompleted -= TaskComplete;
        }

        private void TaskComplete(double duration)
        {
            if (!_started) return;

            TimeSpan ts = TimeSpan.FromSeconds(duration);
            string durationText = $"{ts.Minutes:00}:{ts.Seconds:00};{ts.Milliseconds:000}";
            _completeTimeText.text = $"Completion Time: {durationText}";

            string grade = GetGrade(duration, out Color gradeColor);
            _completeBackground.color = gradeColor;
            _completeText.text = $"Task Completed! Grade:{grade}";

            _animator.SetTrigger(_completeParam);

            _started = false;
        }

        private void TaskStart()
        {
            _animator.SetTrigger(_startedParam);
            _started = true;
        }

        private string GetGrade(double duration, out Color color)
        {
            color = Color.black;

            if(duration <= _aMax)
            {
                color = _gradeAColor;
                return "A";
            }

            if (duration <= _bMax)
            {
                color = _gradeBColor;
                return "B";
            }

            color = _gradeCColor;
            return "C";
        }
    }
}
