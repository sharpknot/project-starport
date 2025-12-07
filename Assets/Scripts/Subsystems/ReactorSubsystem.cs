using NaughtyAttributes;
using Starport.Characters;
using Starport.Sockets;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Starport.Subsystems
{
    public class ReactorSubsystem : SubsystemBase
    {
        [SerializeField] private DescriptionController[] _descriptions;
        [SerializeField, Required] private ReactorCoreSocketController _coreSocket;
        [SerializeField] private InteractableController _interactable;
        [SerializeField] private bool _randomizeMinEnergy = true;
        
        [SerializeField, BoxGroup("Animator")] private Animator _animator;
        [SerializeField, BoxGroup("Animator"), AnimatorParam("_animator", AnimatorControllerParameterType.Bool)]
        private string _isOpenParam;

        private NetworkVariable<float> _minEnergy = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
            );
        private NetworkVariable<bool> _isClosed = new(
            false, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Owner
            );

        public float CurrentEnergy => Percent.Value;
        public float MinEnergy => _minEnergy.Value;
        public event UnityAction<float, float> OnEnergyUpdate;

        private static string _openEnclosureText = "Open enclosure", _closeEnclosureText = "Close enclosure";

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _isClosed.OnValueChanged += EnclosureUpdate;
            OnPercentageUpdate += CurrentEnergyUpdate;
            _minEnergy.OnValueChanged += MinEnergyUpdate;

            StartCoroutine(InitializeServer());

            EnclosureUpdate(false, false);  // Force value update
            CurrentEnergyUpdate(0f);
        }

        public override void OnNetworkDespawn()
        {
            _isClosed.OnValueChanged -= EnclosureUpdate;
            OnPercentageUpdate -= CurrentEnergyUpdate;
            _minEnergy.OnValueChanged -= MinEnergyUpdate;

            if (_interactable != null)
                _interactable.OnInteractAttemptResultServer -= ToggleEnclosure;

            StopAllCoroutines();

            base.OnNetworkDespawn();
        }
        
        private void EnclosureUpdate(bool prev, bool current)
        {
            if (IsServer)
            {
                if (_coreSocket != null)
                {
                    if(_coreSocket.CurrentPickup != null)
                        _coreSocket.CurrentPickup.SetAllowPickup(!_isClosed.Value);

                    if (!_isClosed.Value)
                        Percent.Value = 0f;
                    else
                        Percent.Value = _coreSocket.GetCurrentEnergy();
                }

                if(_interactable != null)
                {
                    if (_isClosed.Value)
                        _interactable.SetDescription(_openEnclosureText);
                    else
                        _interactable.SetDescription(_closeEnclosureText);
                }
            }

            UpdateDescription();
        }

        private void CurrentEnergyUpdate(float current)
        {
            OnEnergyUpdate?.Invoke(CurrentEnergy, MinEnergy);
        }

        private void MinEnergyUpdate(float prev, float current)
        {
            OnEnergyUpdate?.Invoke(CurrentEnergy, MinEnergy);
        }

        private void UpdateDescription()
        {
            if (_descriptions == null) return;

            float energy = Mathf.Clamp01(CurrentEnergy);
            float minEnergy = Mathf.Clamp01(_minEnergy.Value);
            string title = "Reactor";
            string desc = $"Heats up reactor fluid for power generation. Needs to be closed to operate." +
                $"\nStatus: ";

            string status = "";
            if(!_isClosed.Value)
            {
                status = "Unpowered (Enclosure opened)";
            }
            else
            {
                if(CurrentEnergy >= _minEnergy.Value)
                {
                    status = "Powered";
                }
                else
                {
                    status = $"Unpowered (Not enough energy)" +
                        $"\nCurrent Energy: {UIUtility.GetPercentage(CurrentEnergy)}" +
                        $"\nMinimum Required Energy: {UIUtility.GetPercentage(_minEnergy.Value)}";
                }
            }

            desc += status;

            foreach (var description in _descriptions)
            {
                if (description == null) continue;
                description.Title = title;
                description.Description = desc;
            }
        }

        private IEnumerator InitializeServer()
        {
            if(!IsServer)
                yield break;

            Percent.Value = 0f;
            _minEnergy.Value = 0f;

            if (_coreSocket == null)
            {
                OpenEnclosure(false);
                yield break;
            }

            while(!_coreSocket.NetworkObject.IsSpawned)
                yield return null;

            if (_interactable != null)
            {
                _interactable.OnInteractAttemptResultServer += ToggleEnclosure;
                _interactable.SetDescription(_openEnclosureText);
            }

            float minEnergy = 0f;
            if (_randomizeMinEnergy)
                minEnergy = Random.Range(0f, 1f);

            _minEnergy.Value = minEnergy;

            Percent.Value = _coreSocket.GetCurrentEnergy();
            OpenEnclosure(false);
        }

        private void OpenEnclosure(bool open)
        {
            if (!IsServer) return;

            _isClosed.Value = !open;

            if(_animator  != null)
                _animator.SetBool(_isOpenParam, open);

            IsLocallyActive.Value = (CurrentEnergy >= _minEnergy.Value);
        }

        private void ToggleEnclosure(bool interactResult, CharacterNetworkManager characterNetworkManager)
        {
            if (!IsServer) return;
            if (!interactResult) return;

            OpenEnclosure(_isClosed.Value);
        }

        [Button]
        private void DebugOpenEnclosure() => OpenEnclosure(true);
        [Button]
        private void DebugCloseEnclosure() => OpenEnclosure(false);
    }
}
