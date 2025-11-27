using UnityEngine;
using UnityEngine.UI;

namespace Starport.UI
{
    public class UIHUDToolbarIconController : MonoBehaviour
    {
        [SerializeField] private Image _iconImage, _selectedImage;

        public void SetIcon(Sprite icon, bool isSelected)
        {
            if(_iconImage != null)
            {
                if(icon != null)
                {
                    _iconImage.enabled = true;
                    _iconImage.sprite = icon;
                }
                else
                {
                    _iconImage.enabled = false;
                }
            }

            if(_selectedImage != null)
            {
                Color curColor = _selectedImage.color;
                curColor.a = 1f;
                if (!isSelected)
                    curColor.a = 0.3f;

                _selectedImage.color = curColor;                
            }
            
        }
    }
}
