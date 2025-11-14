using UnityEngine;
using UnityEngine.UI;

namespace Starport.UI
{
    [RequireComponent(typeof(Image))]
    public class UISpinner : MonoBehaviour
    {
        [SerializeField] private float _spinnerDuration = 1f;

        protected Image SpinnerImage
        {
            get
            {
                if(_spinnerImage == null)
                {
                    _spinnerImage = GetComponent<Image>();
                    _spinnerClockwise = _spinnerImage.fillClockwise;
                }
                    
                return _spinnerImage;
            }
        }

        private bool _spinnerClockwise;
        private Image _spinnerImage;
        private bool _isFilling = true;
        private float _percent = 0f;
        private bool _isPlaying = false;
        
        private bool _initialized = false;

        private void Awake()
        {
            if(!_initialized)
                StopSpinning();
        }

        void Update()
        {
            if (!_isPlaying) return;

            float spd = 1f / _spinnerDuration;
            float delta = spd * Time.deltaTime;

            if (_isFilling)
            {
                _percent += delta;

                SpinnerImage.fillClockwise = _spinnerClockwise;
                SpinnerImage.fillAmount = Mathf.Clamp01(_percent);

                if(_percent >= 1f)
                {
                    _percent = 1f;
                    SpinnerImage.fillClockwise = !_spinnerClockwise;
                    _isFilling = !_isFilling;
                }

                return;
            }

            _percent -= delta;

            SpinnerImage.fillClockwise = !_spinnerClockwise;
            SpinnerImage.fillAmount = Mathf.Clamp01(_percent);
            if (_percent <= 0f)
            {
                _percent = 0f;
                SpinnerImage.fillClockwise = !_spinnerClockwise;
                _isFilling = !_isFilling;
            }
        }

        private void OnValidate()
        {
            _spinnerDuration = Mathf.Max(0.1f, _spinnerDuration);
        }

        public void StartSpinning()
        {
            if(_isPlaying) return;

            SpinnerImage.fillAmount = 0f;
            SpinnerImage.fillClockwise = _spinnerClockwise;
            _percent = 0f;
            _isPlaying = true;
            _isFilling = true;

            _initialized = true;
        }

        public void StopSpinning()
        {
            SpinnerImage.fillAmount = 0f;
            SpinnerImage.fillClockwise = _spinnerClockwise;
            _percent = 0f;
            _isPlaying = false;

            _initialized = true;
        }
    }
}
