using NaughtyAttributes;
using System;
using TMPro;
using UnityEngine;

namespace Starport
{
    public class UITaskTimerController : MonoBehaviour
    {
        [SerializeField, Required] 
        private RectTransform _parent;
        [SerializeField, Required]
        private TMP_Text _timerText;
        [SerializeField, Required]
        private GameTaskManager _gameTaskManager;

        // Update is called once per frame
        void Update()
        {
            if(_gameTaskManager ==null || !_gameTaskManager.NetworkObject.IsSpawned || _timerText == null)
            {
                UIUtility.ShowPanel(_parent, false);
                return;
            }

            if(!_gameTaskManager.HasCurrentTask(out double elapsedTime))
            {
                UIUtility.ShowPanel(_parent, false);
                return;
            }

            UIUtility.ShowPanel(_parent, true);

            TimeSpan ts = TimeSpan.FromSeconds(elapsedTime);
            string formatted = $"{ts.Minutes:00}:{ts.Seconds:00};{ts.Milliseconds:000}";
            _timerText.text = formatted;
        }



    }
}
