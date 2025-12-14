using NaughtyAttributes;
using Starport.Characters;
using Starport.Pickups;
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
        
        [SerializeField, BoxGroup("Animator")] private Animator _animator;
        [SerializeField, BoxGroup("Animator"), AnimatorParam("_animator", AnimatorControllerParameterType.Bool)]
        private string _isOpenParam;

        private NetworkVariable<float> _minEnergy = new(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Owner
            );
        private NetworkVariable<bool> _enclosureOpened = new(
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

            StartCoroutine(InitializeServer());
            StartCoroutine(InitializeClient());
        }

        public override void OnNetworkDespawn()
        {
            StopAllCoroutines();

            _minEnergy.OnValueChanged -= MinEnergyUpdate;
            OnPercentageUpdate -= CurrentEnergyUpdate;
            _enclosureOpened.OnValueChanged -= EnclosureUpdate;

            if (_interactable != null)
                _interactable.OnInteractAttemptResultServer -= ServerToggleEnclosure;

            base.OnNetworkDespawn();
        }

        public override void InitializeSubSystem(float completionAmount = 1)
        {
            base.InitializeSubSystem(completionAmount);

            if (!IsServer) return;

            if (_coreSocket == null || !_coreSocket.NetworkObject.IsSpawned)
                return;

            _minEnergy.Value = Random.Range(0f, 1f);

            float finalEnergy = Random.Range(_minEnergy.Value, 1f);
            if (completionAmount < 1f)
                finalEnergy = Random.Range(0f, _minEnergy.Value);

            _coreSocket.SpawnPickupInSocket(new() { CapacityPercent = finalEnergy });
            Percent.Value = finalEnergy;
            _enclosureOpened.Value = false;
        }

        public override void Deinitialize()
        {
            base.Deinitialize();
            if (!IsServer) return;

            _minEnergy.Value = 0f;
            Percent.Value = 0f;
            _enclosureOpened.Value = true;

            if (_coreSocket == null || !_coreSocket.NetworkObject.IsSpawned)
                return;

            _coreSocket.ClearSocket();

        }

        private void EnclosureUpdate(bool prev, bool current)
        {
            UpdateDescription();

            if (!IsServer) return;

            _animator.SetBool(_isOpenParam, _enclosureOpened.Value);
            UpdateInteractable();

            // Update the percentage
            float finalPercentage = 0f;
            if(_coreSocket != null && _coreSocket.NetworkObject.IsSpawned && _coreSocket.CurrentPickup != null)
            {
                finalPercentage = Mathf.Clamp01(_coreSocket.CurrentPickup.CurrentState.CapacityPercent);
            }
            Percent.Value = finalPercentage;
        }

        private void CurrentEnergyUpdate(float current)
        {
            UpdateDescription();
            UpdateLocallyActive();
            OnEnergyUpdate?.Invoke(CurrentEnergy, MinEnergy);
        }

        private void MinEnergyUpdate(float prev, float current) => CurrentEnergyUpdate(Percent.Value);

        private void UpdateDescription()
        {
            if (_descriptions == null) return;

            float energy = Mathf.Clamp01(CurrentEnergy);
            float minEnergy = Mathf.Clamp01(_minEnergy.Value);
            string title = "Reactor";
            string desc = $"Heats up reactor fluid for power generation. Needs to be closed to operate." +
                $"\nStatus: ";

            string status = "";
            if(_enclosureOpened.Value)
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

        private void UpdateInteractable()
        {
            if (!IsServer) return;
            if (_interactable == null || !_interactable.NetworkObject.IsSpawned)
                return;

            string text = _openEnclosureText;
            if(_enclosureOpened.Value)
                text = _closeEnclosureText;

            _interactable.SetDescription(text);
        }

        private void UpdateLocallyActive()
        {
            if (!IsServer) return;

            IsLocallyActive.Value = Percent.Value >= _minEnergy.Value;
        }

        private IEnumerator InitializeServer()
        {
            if(!IsServer)
                yield break;

            if(_interactable != null)
            {
                while(!_interactable.NetworkObject.IsSpawned)
                    yield return null;

                _interactable.OnInteractAttemptResultServer += ServerToggleEnclosure;
            }

            if(_coreSocket != null)
            {
                while(!_coreSocket.NetworkObject.IsSpawned)
                    yield return null;
            }

            _enclosureOpened.OnValueChanged += EnclosureUpdate;
        }

        private IEnumerator InitializeClient()
        {
            if (_interactable != null)
            {
                while (!_interactable.NetworkObject.IsSpawned)
                    yield return null;
            }

            CurrentEnergyUpdate(Percent.Value);

            _minEnergy.OnValueChanged += MinEnergyUpdate;
            OnPercentageUpdate += CurrentEnergyUpdate;
        }

        private void ServerToggleEnclosure(bool interactResult, CharacterNetworkManager characterNetworkManager)
        {
            if (!IsServer) return;
            _enclosureOpened.Value = !_enclosureOpened.Value;
        }
    }
}
