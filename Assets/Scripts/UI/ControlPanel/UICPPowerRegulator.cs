using NaughtyAttributes;
using Starport.Subsystems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Starport.UI.ControlPanel
{
    public class UICPPowerRegulator : MonoBehaviour
    {
        [SerializeField, Required] private PowerRegulatorSubsystem _powerRegulator;

        [SerializeField, Required] private Slider _slider;

        [SerializeField, Required, BoxGroup("Current Frequency")] 
        private TMP_Text _currentText;
        [SerializeField, BoxGroup("Current Frequency")]
        private Color _currentNormalColor, _activatedColor;

        [SerializeField, Required, BoxGroup("Slider Frequency")]
        private TMP_Text _sliderText;

        private void OnEnable()
        {
            _slider.minValue = PowerRegulatorSubsystem.LowestFrequency;
            _slider.maxValue = PowerRegulatorSubsystem.HighestFrequency;

            _slider.SetValueWithoutNotify(_powerRegulator.CurrentFrequency);
            _sliderText.text = $"Current Frequency: {UIUtility.GetDecimals(_powerRegulator.CurrentFrequency)}Hz";

            UpdateValues(_powerRegulator.CurrentFrequency);
            _powerRegulator.OnCurrentFrequencyUpdate += UpdateValues;
        }

        private void OnDisable()
        {
            _powerRegulator.OnCurrentFrequencyUpdate -= UpdateValues;
        }

        private void UpdateValues(float currentValue)
        {
            _currentText.text = $"System Frequency: {UIUtility.GetDecimals(currentValue)}Hz";

            Color c = _currentNormalColor;
            if (_powerRegulator.IsWithinTargetFrequency) c = _activatedColor;
            _currentText.color = c;
        }

        public void SetCurrentFrequency(float frequency)
        {
            _sliderText.text = $"Current Frequency: {UIUtility.GetDecimals(frequency)}Hz";
            _powerRegulator.SetCurrentFrequency(frequency);
        }
    }
}
