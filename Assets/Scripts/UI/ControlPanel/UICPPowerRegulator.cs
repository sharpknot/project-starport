using NaughtyAttributes;
using Starport.Subsystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starport.UI.ControlPanel
{
    public class UICPPowerRegulator : UIControlPanelBase
    {
        [SerializeField, Required] private PowerRegulatorSubsystem _powerRegulator;

        [SerializeField, Required] private Slider _slider;
        [SerializeField, Required] private RawImage _frequencyRawImage;

        [SerializeField, Required, BoxGroup("Current Frequency")] 
        private TMP_Text _currentText;
        [SerializeField, BoxGroup("Current Frequency")]
        private Color _currentNormalColor, _activatedColor;

        [SerializeField, Required, BoxGroup("Slider Frequency")]
        private TMP_Text _sliderText;

        private void Awake()
        {
            _powerRegulator.OnCurrentFrequencyUpdate += UpdateValues;

            _slider.minValue = PowerRegulatorSubsystem.LowestFrequency;
            _slider.maxValue = PowerRegulatorSubsystem.HighestFrequency;            
        }

        private void OnDestroy()
        {
            if (_powerRegulator != null)
                _powerRegulator.OnCurrentFrequencyUpdate -= UpdateValues;
        }

        public override void EnableUI()
        {
            base.EnableUI();

            _slider.SetValueWithoutNotify(_powerRegulator.CurrentFrequency);
            _sliderText.text = $"Current Frequency: {UIUtility.GetDecimals(_powerRegulator.CurrentFrequency)}Hz";

            UpdateValues(_powerRegulator.CurrentFrequency);
            UpdateTexture();
        }

        public override void DisableUI()
        {
            //_powerRegulator.OnCurrentFrequencyUpdate -= UpdateValues;
            base.DisableUI();
        }

        private void UpdateValues(float currentValue)
        {
            _currentText.text = $"System Frequency: {UIUtility.GetDecimals(currentValue)}Hz";

            Color c = _currentNormalColor;
            if (_powerRegulator.IsWithinTargetFrequency) c = _activatedColor;
            _currentText.color = c;
        }

        private void UpdateTexture()
        {
            if (_powerRegulator == null) return;
            if (_frequencyRawImage == null) return;

            if(_frequencyRawImage.texture != _powerRegulator.FrequencyRender)
                _frequencyRawImage.texture = _powerRegulator.FrequencyRender;
        }

        public void SetCurrentFrequency(float frequency)
        {
            _sliderText.text = $"Current Frequency: {UIUtility.GetDecimals(frequency)}Hz";
            _powerRegulator.SetCurrentFrequency(frequency);
        }
    }
}
