using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starport.UI.Systems
{
    [RequireComponent(typeof(RectTransform))]
    public class UISubsystemDisplay : MonoBehaviour
    {
        [SerializeField, Required] private TMP_Text _nameLabel;
        [SerializeField, Required] private RectTransform _progressBar;
        [SerializeField, Required] private Image _progressImage;
        [SerializeField] private Color _activeColor = Color.green, _inactiveColor = Color.red;

        public void SetSubsystemDisplay(string subsystemName, bool isActive, float progress = 1f)
        {
            if(_nameLabel != null)
            {
                if(_nameLabel.text != subsystemName)
                    _nameLabel.text = subsystemName;
            }
                

            if(_progressImage != null)
            {
                Color finalColor = _inactiveColor;
                if(isActive) finalColor = _activeColor;
                _progressImage.color = finalColor;
            }

            if (_progressBar != null)
            {
                float curProgress = Mathf.Clamp01(progress);
                Vector3 curScale = _progressBar.localScale;
                _progressBar.localScale = new(curProgress, curScale.y, curScale.z);
            }
        }
        
    }
}
