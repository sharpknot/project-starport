using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

namespace Starport.Subsystems
{
    [RequireComponent(typeof(NetworkObject), typeof(Animator))]
    public class GeneratorSubsystem : SubsystemBase
    {
        [SerializeField, BoxGroup("Animation")] private Animator _animator;
        [SerializeField, BoxGroup("Animation"), AnimatorParam("_animator", AnimatorControllerParameterType.Bool)]
        private string _isActiveParam;

        [SerializeField] private Part _turbine, _shaft, _generator;
        [SerializeField] private GameObject _lightsParent;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            StartCoroutine(InitializeServer());
            StartCoroutine(InitializeClient());
            
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();

            if (_turbine.Fixable != null)
                _turbine.Fixable.OnFixAmountUpdate += FixAmountUpdate;
            if (_shaft.Fixable != null)
                _shaft.Fixable.OnFixAmountUpdate += FixAmountUpdate;
            if (_generator.Fixable != null)
                _generator.Fixable.OnFixAmountUpdate += FixAmountUpdate;

            base.OnNetworkDespawn();
        }

        private void SetInitialRepairState(FixableController fixable)
        {
            if (fixable == null) return;
            if(Random.Range(0, 1f) > 0.5f)
            {
                fixable.FixedAmount = 1f;
                return;
            }

            fixable.FixedAmount = Random.Range(0, 1f);
        }

        private void UpdateAnimator()
        {
            if(_animator == null) return;
            if (!IsServer) return;

            _animator.SetBool(_isActiveParam, IsFullyFixed());
        }

        private bool IsFullyFixed()
        {
            float fixCount = 0f;
            float netFixAmount = 0f;

            AddFixAmount(_turbine.Fixable, ref fixCount, ref netFixAmount);
            AddFixAmount(_shaft.Fixable, ref fixCount, ref netFixAmount);
            AddFixAmount(_generator.Fixable, ref fixCount, ref netFixAmount);

            if(fixCount <= 0f) return false;
            return (netFixAmount / fixCount) >= 1f;
        }

        private void AddFixAmount(FixableController fixable, ref float fixCount, ref float netFixAmount)
        {
            if(fixable == null) return;

            fixCount += 1f;
            netFixAmount += fixable.FixedAmount;
        }

        private void FixAmountUpdate(float fixAmount, bool fullyFixed) 
        { 
            UpdateDescriptions();
            UpdateAnimator();
            UpdateLights();

            if (!IsServer) return;

            bool totallyFixed = IsFullyFixed();
            if(IsLocallyActive.Value != totallyFixed) 
                IsLocallyActive.Value = totallyFixed;
        }

        private void UpdateDescriptions()
        {
            UpdateDescription(_shaft.Description, "Generator Shaft", "Transfers rotation motion from turbine to generator.", _shaft.Fixable);
            UpdateDescription(_turbine.Description, "Turbine", "Creates rotational motion from high pressure fluids.", _turbine.Fixable);
            UpdateDescription(_generator.Description, "Generator", "Converts rotational motion to electrical power.", _generator.Fixable);
        }

        private void UpdateDescription(DescriptionController descriptionController, string title, string description, FixableController fixable)
        {
            if(descriptionController == null) return;

            descriptionController.Title = title;

            string desc = description;
            if(fixable != null)
            {
                string fixDesc = "\nStatus: ";
                if (fixable.IsFixed) fixDesc += "Running";
                else fixDesc += $"Broken ({UIUtility.GetPercentage(fixable.FixedAmount)})";

                desc += fixDesc;
            }

            descriptionController.Description = desc;
        }

        private void UpdateLights()
        {
            if(_lightsParent == null) return;

            bool fullyFixed = IsFullyFixed();   
            if(_lightsParent.activeSelf != fullyFixed)
                _lightsParent.SetActive(fullyFixed);
        }

        [System.Serializable]
        private struct Part
        {
            public DescriptionController Description;
            public FixableController Fixable;
        }

        private IEnumerator InitializeServer()
        {
            if(!IsServer) yield break;

            while(!_turbine.Fixable.IsSpawned || !_shaft.Fixable.IsSpawned || !_generator.Fixable.IsSpawned)
            {
                yield return null;
            }

            SetInitialRepairState(_turbine.Fixable);
            SetInitialRepairState(_shaft.Fixable);
            SetInitialRepairState(_generator.Fixable);

            UpdateAnimator();

            IsLocallyActive.Value = IsFullyFixed();
        }

        private IEnumerator InitializeClient()
        {
            while (!_turbine.Fixable.IsSpawned || !_shaft.Fixable.IsSpawned || !_generator.Fixable.IsSpawned)
            {
                yield return null;
            }

            if (_turbine.Fixable != null)
                _turbine.Fixable.OnFixAmountUpdate += FixAmountUpdate;
            if (_shaft.Fixable != null)
                _shaft.Fixable.OnFixAmountUpdate += FixAmountUpdate;
            if (_generator.Fixable != null)
                _generator.Fixable.OnFixAmountUpdate += FixAmountUpdate;

            UpdateDescriptions();
            UpdateLights();
        }
    }
}
