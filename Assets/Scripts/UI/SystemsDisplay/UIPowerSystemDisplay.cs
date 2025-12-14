using NaughtyAttributes;
using Starport.Subsystems;
using Starport.Systems;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starport.UI.Systems
{
    [RequireComponent(typeof(Canvas))]
    public class UIPowerSystemDisplay : UISystemDisplay
    {
        [SerializeField, Required] private PowerGenerationSystem _powerSystem;
        [SerializeField, Required] private UISubsystemDisplay _subsystemDisplay;

        [SerializeField] private RectTransform _subsystemPanel, _pipesPanel;

        [SerializeField, BoxGroup("Activation States")]
        private RectTransform _activeDisplay, _inactiveDisplay;

        private void Start()
        {
            _powerSystem.OnSystemActiveUpdated += UpdateActivationState;

            _powerSystem.OnPipeSubsystemUpdated += OnPipesUpdated;
            RedrawPipeDisplays();

            InitializeSubsystems(_powerSystem.ReservoirSubsystems, _subsystemPanel);
            InitializeSubsystems(_powerSystem.ReactorSubsystems, _subsystemPanel);
            InitializeSubsystems(_powerSystem.GeneratorSubsystems, _subsystemPanel);
            InitializeSubsystems(_powerSystem.RegulatorSubsystems, _subsystemPanel);

            _powerSystem.OnReservoirSubsystemsUpdated += SubsystemUpdated;
            _powerSystem.OnReactorSubsystemsUpdated += SubsystemUpdated;
            _powerSystem.OnGeneratorSubsystemsUpdated += SubsystemUpdated;
            _powerSystem.OnRegulatorSubsystemsUpdated += SubsystemUpdated;
        }

        private void Update()
        {
            UpdateActivationDisplayState();
        }

        private void OnDestroy()
        {
            _powerSystem.OnSystemActiveUpdated -= UpdateActivationState;
            _powerSystem.OnPipeSubsystemUpdated -= OnPipesUpdated;

            _powerSystem.OnReservoirSubsystemsUpdated -= SubsystemUpdated;
            _powerSystem.OnReactorSubsystemsUpdated -= SubsystemUpdated;
            _powerSystem.OnGeneratorSubsystemsUpdated -= SubsystemUpdated;
            _powerSystem.OnRegulatorSubsystemsUpdated -= SubsystemUpdated;
        }

        private void UpdateActivationDisplayState()
        {
            if(_powerSystem ==null || !_powerSystem.IsSpawned)
            {
                UpdateActivationState(false);
                return;
            }

            UpdateActivationState(_powerSystem.IsSystemActive);
        }

        private void UpdateActivationState(bool active)
        {
            UIUtility.ShowPanel(_activeDisplay, active);
            UIUtility.ShowPanel(_inactiveDisplay, !active);
        }

        private Dictionary<FluidPipeSubsystem, UISubsystemDisplay> _pipeDisplay;
        private void OnPipesUpdated(FluidPipeSubsystem[] pipes) => RedrawPipeDisplays();
        private void RedrawPipeDisplays()
        {
            _pipeDisplay ??= new();

            if (_powerSystem == null) return;
            if(_powerSystem.PipeSubsystems == null) return;

            foreach(var pipe in _powerSystem.PipeSubsystems)
            {
                if(pipe == null) continue;

                if (_pipeDisplay.ContainsKey(pipe))
                {
                    if (_pipeDisplay[pipe] == null)
                    {
                        _pipeDisplay[pipe] = AddDisplay(_subsystemDisplay, _pipesPanel, pipe.SubsystemName, pipe.CurrentFixAmount >= 1f, pipe.CurrentFixAmount);
                    }
                    else
                    {
                        _pipeDisplay[pipe].SetSubsystemDisplay(pipe.SubsystemName, pipe.CurrentFixAmount >= 1f, pipe.CurrentFixAmount);
                    }

                    continue;
                }

                _pipeDisplay.Add(pipe, AddDisplay(_subsystemDisplay, _pipesPanel, pipe.SubsystemName, pipe.CurrentFixAmount >= 1f, pipe.CurrentFixAmount));
            }
        }

        private Dictionary<SubsystemBase, UISubsystemDisplay> _subsystemDisplays;
        private void InitializeSubsystems(SubsystemBase[] subsystems, RectTransform panel)
        {
            _subsystemDisplays ??= new();
            if (subsystems == null || panel == null) return;
            if (_subsystemDisplay == null) return;

            foreach (var subsystem in subsystems)
            {
                if(subsystem == null) continue;
                if (_subsystemDisplays.ContainsKey(subsystem)) continue;

                _subsystemDisplays.Add(subsystem, AddDisplay(_subsystemDisplay, panel, subsystem.SubsystemName, subsystem.IsCurrentlyLocallyActive, subsystem.CurrentPercent));
            }
        }
        private void SubsystemUpdated(SubsystemBase[] subsystems)
        {
            _subsystemDisplays ??= new();
            foreach (var subsystem in _subsystemDisplays.Keys)
            {
                if (subsystem == null) continue;
                if (_subsystemDisplays[subsystem] == null) continue;

                _subsystemDisplays[subsystem].SetSubsystemDisplay(subsystem.SubsystemName, subsystem.IsCurrentlyLocallyActive, subsystem.CurrentPercent);
            }
        }

        
    }
}
