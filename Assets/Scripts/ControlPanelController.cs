using DG.Tweening;
using Drawing;
using NaughtyAttributes;
using Starport.Characters;
using Starport.PlayerState;
using Starport.UI.ControlPanel;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Starport
{
    [RequireComponent (typeof (OwnershipController))]
    public class ControlPanelController : NaughtyNetworkBehaviour
    {
        protected OwnershipController Ownership
        {
            get
            {
                if (_ownership == null)
                    _ownership = GetComponent<OwnershipController>();
                return _ownership;
            }
        }
        private OwnershipController _ownership;

        [SerializeField, Required]
        private InteractableController _interactable;
        private CharacterNetworkManager _currentCharacterServer, _currentCharacterClient;

        public Transform Seat
        {
            get
            {
                if (_seat == null) return transform;
                return _seat;
            }

        }
        [SerializeField] private Transform _seat;
        [SerializeField, Required] private Transform _resetPosition;
        [SerializeField] private UIControlPanelBase _controlPanelUi;
        [SerializeField, Required] private PlayerStateBase _playerControlPanelBaseState, _playerControlPanelLocomotionState;
        [SerializeField, Required] private CinemachineCamera _camera;

        private Sequence _serverWaitOwnership, _clientWaitOwnership;

        private static readonly float _serverOwnershipWaitDuration = 3f, _clientOwnershipWaitDuration = 1.5f;

        protected PlayerInputActions InputActions
        {
            get
            {
                if(_inputActions == null)
                {
                    _inputActions = new PlayerInputActions();
                    _inputActions.Disable();
                }

                return _inputActions;
            }
        }
        private PlayerInputActions _inputActions;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            EnableUI(false);

            _interactable.OnInteractAttemptResultServer += ServerSuccessInteract;
            _interactable.OnInteractAttemptResultClient += ClientSuccessInteract;
        }

        public override void OnNetworkDespawn()
        {
            _interactable.OnInteractAttemptResultServer -= ServerSuccessInteract;
            _interactable.OnInteractAttemptResultClient -= ClientSuccessInteract;

            Ownership.OnServerOwnershipRequestSuccess -= ServerSuccessChangeOwnership;
            Ownership.OnServerOwnershipRequestFailure -= ServerFailChangeOwnership;

            Ownership.OnOwnershipRequestSuccess -= ClientSuccessOwnership;
            Ownership.OnServerOwnershipRequestFailure -= ClientFailOwnership;

            Ownership.OnServerOwnershipReset -= ServerResetOwner;

            InputActions.ControlPanel.Quit.performed -= ExitInput;

            KillSequence(ref _serverWaitOwnership);
            KillSequence(ref _clientWaitOwnership);
            base.OnNetworkDespawn();
        }

        public void ExitControlPanel()
        {
            if (!Ownership.HasOwner(out _))
            {
                Debug.LogError($"[ControlPanelController] {gameObject.name} has no owner! Unable to Exit control panel");
                return;
            }

            if (_currentCharacterClient != null)
            {
                _currentCharacterClient.StateManager.CloseControlPanel();
                _currentCharacterClient.NetworkObject.TryRemoveParent(true);
            }

            Ownership.ResetOwnership();
        }

        private void ServerSuccessInteract(bool success, CharacterNetworkManager requestingCharacter)
        {
            if (!IsServer)
            {
                Debug.LogError($"[ControlPanelController {gameObject.name}] ServerSuccessInteract failed: Not the server");
                return;
            }

            if (!success)
            {
                Debug.LogError($"[ControlPanelController {gameObject.name}] ServerSuccessInteract failed: Interaction failed");
                return;
            }

            if (Ownership.HasOwner(out ulong clientID))
            {
                Debug.LogError($"[ControlPanelController {gameObject.name}] ServerSuccessInteract failed: Already has owner, clientID {clientID}");
                return;
            }

            // Disable interactibility
            _interactable.SetInteractionAllowed(false);

            // Cache the requesting character and parent it to the seat
            _currentCharacterServer = requestingCharacter;
            if (_currentCharacterServer != null)
            {
                _currentCharacterServer.NetworkObject.TrySetParent(Seat, true);
                Debug.Log($"[ControlPanelController {gameObject.name}] ServerSuccessInteract: Attempting to set parent {_currentCharacterServer.gameObject.name} ({_currentCharacterServer.NetworkObject.OwnerClientId}) to seat {Seat.gameObject.name}");
            }

            Ownership.OnServerOwnershipRequestSuccess += ServerSuccessChangeOwnership;
            Ownership.OnServerOwnershipRequestFailure += ServerFailChangeOwnership;

            // Safety, wait for ownership change
            KillSequence(ref _serverWaitOwnership);
            _serverWaitOwnership = DOTween.Sequence().
                AppendInterval(_serverOwnershipWaitDuration).
                AppendCallback(ServerFailChangeOwnership);

            Debug.Log($"[ControlPanelController {gameObject.name}] ServerSuccessInteract: Complete! Waiting for Ownership change..");
        }

        private void ServerFailChangeOwnership()
        {
            if (!IsServer)
            {
                Debug.LogError($"[ControlPanelController {gameObject.name}] ServerFailChangeOwnership failed: Not the server");
                return;
            }

            KillSequence(ref _serverWaitOwnership);

            Ownership.OnServerOwnershipRequestSuccess -= ServerSuccessChangeOwnership;
            Ownership.OnServerOwnershipRequestFailure -= ServerFailChangeOwnership;

            // Reenable interactibility
            _interactable.SetInteractionAllowed(true);
            
            // Remove the parent of character
            if (_currentCharacterServer != null)
            {
                Debug.Log($"[ControlPanelController {gameObject.name}] ServerSuccessInteract: Attempting to remove parent {_currentCharacterServer.gameObject.name} ({_currentCharacterServer.NetworkObject.OwnerClientId})");
                _currentCharacterServer.NetworkObject.TryRemoveParent();
            }
             
            _currentCharacterServer = null;

            Debug.LogError($"[ControlPanelController {gameObject.name}] ServerFailChangeOwnership completed!");
        }

        private void ServerSuccessChangeOwnership()
        {
            if (!IsServer)
            {
                Debug.LogError($"[ControlPanelController {gameObject.name}] ServerSuccessChangeOwnership failed: Not the server");
                return;
            }

            KillSequence(ref _serverWaitOwnership);

            Ownership.OnServerOwnershipRequestSuccess -= ServerSuccessChangeOwnership;
            Ownership.OnServerOwnershipRequestFailure -= ServerFailChangeOwnership;

            Ownership.OnServerOwnershipReset += ServerResetOwner;

            Debug.Log($"[ControlPanelController {gameObject.name}] ServerSuccessChangeOwnership completed!");
        }

        private void ClientSuccessInteract(bool success, CharacterNetworkManager clientsideCharacter)
        {
            if(!success)
            {
                Debug.LogError($"[ControlPanelController {gameObject.name}] ClientSuccessInteract failed: Interact failed");
                return;
            }

            _currentCharacterClient = clientsideCharacter;

            Ownership.OnOwnershipRequestSuccess += ClientSuccessOwnership;
            Ownership.OnServerOwnershipRequestFailure += ClientFailOwnership;

            KillSequence(ref _clientWaitOwnership);
            _clientWaitOwnership = DOTween.Sequence().
                AppendInterval(_clientOwnershipWaitDuration).
                AppendCallback(ClientFailOwnership);

            Debug.Log($"[ControlPanelController {gameObject.name}] ClientSuccessInteract completed! Awaiting Ownership change...");

            // Request for ownership change
            Ownership.RequestOwnership();
        }

        private void ClientSuccessOwnership()
        {
            Debug.Log($"[ControlPanelController {gameObject.name}] ClientSuccessOwnership started!");

            KillSequence(ref _clientWaitOwnership);

            Ownership.OnOwnershipRequestSuccess -= ClientSuccessOwnership;
            Ownership.OnServerOwnershipRequestFailure -= ClientFailOwnership;

            if(_currentCharacterClient != null)
            {
                _currentCharacterClient.StateManager.MotionController.TeleportInstant(Seat.position);
                _currentCharacterClient.StateManager.MotionController.RotateInstant(Seat.rotation);

                _currentCharacterClient.StateManager.OpenControlPanel();

                // Force change the player state
                _currentCharacterClient.StateManager.ChangeBaseState(_playerControlPanelBaseState);
                _currentCharacterClient.StateManager.ChangeLocomotionState(_playerControlPanelLocomotionState);

                string strBase = "Null base state", strLoco = "Null locomotion state";
                
                if(_playerControlPanelBaseState != null) 
                    strBase = _playerControlPanelBaseState.ToString();

                if (_playerControlPanelLocomotionState != null)
                    strLoco = _playerControlPanelLocomotionState.ToString();

                Cursor.visible = true;

                Debug.Log($"[ControlPanelController {gameObject.name}] ClientSuccessOwnership: Changing states... {strBase}, {strLoco}");
            }

            // Change camera
            _camera.Prioritize();

            if(_controlPanelUi != null)
            {
                EnableUI(true);
                Debug.Log($"[ControlPanelController {gameObject.name}] ClientSuccessOwnership: Enabling control panel ui {_controlPanelUi.gameObject.name}");
            }

            InputActions.ControlPanel.Quit.performed += ExitInput;
            InputActions.Enable();
            Ownership.OnOwnershipReset += ClientResetOwner;

            Debug.Log($"[ControlPanelController {gameObject.name}] ClientSuccessOwnership completed!");
        }

        private void ClientFailOwnership()
        {
            KillSequence(ref _clientWaitOwnership);

            Ownership.OnOwnershipRequestSuccess -= ClientSuccessOwnership;
            Ownership.OnServerOwnershipRequestFailure -= ClientFailOwnership;

            _currentCharacterClient = null;

            Debug.LogError($"[ControlPanelController {gameObject.name}] ClientFailOwnership failed!");
        }

        // Reset ownership success, as client
        private void ClientResetOwner()
        {
            Debug.Log($"[ControlPanelController {gameObject.name}] ClientResetOwner started!");

            Ownership.OnOwnershipReset -= ClientResetOwner;

            if (_currentCharacterClient != null)
            {
                if (_currentCharacterClient.StateManager.MotionController != null && _resetPosition != null)
                {
                    _currentCharacterClient.transform.SetPositionAndRotation(_resetPosition.position, _resetPosition.rotation);

                    _currentCharacterClient.StateManager.MotionController.TeleportInstant(_resetPosition.position);
                    _currentCharacterClient.StateManager.MotionController.RotateInstant(_resetPosition.rotation);

                    Debug.Log($"[ControlPanelController {gameObject.name}] ClientResetOwner: {_currentCharacterClient.gameObject.name} ({_currentCharacterClient.NetworkObject.OwnerClientId}) repositioned to {_resetPosition.gameObject.name}");
                }
                    
                // Default the states
                _currentCharacterClient.StateManager.ChangeToDefaultBaseState();
                _currentCharacterClient.StateManager.ChangeToDefaultLocomotionState();

                Debug.Log($"[ControlPanelController {gameObject.name}] ClientResetOwner: {_currentCharacterClient.gameObject.name} ({_currentCharacterClient.NetworkObject.OwnerClientId}) changed to default states");
            }

            _currentCharacterClient = null;

            if (_controlPanelUi != null)
            {
                EnableUI(false);
                Debug.Log($"[ControlPanelController {gameObject.name}] ClientResetOwner: Control panel deactivated");
            }

            InputActions.Disable();
            InputActions.ControlPanel.Quit.performed -= ExitInput;

            Debug.Log($"[ControlPanelController {gameObject.name}] ClientResetOwner completed!");
        }

        // Reset ownership as server
        private void ServerResetOwner()
        {
            Debug.Log($"[ControlPanelController {gameObject.name}] ServerResetOwner started!");

            if (!IsServer)
            {
                Debug.LogError($"[ControlPanelController {gameObject.name}] ServerResetOwner failed: Not the server");
                return;
            }

            Ownership.OnServerOwnershipReset -= ServerResetOwner;
            _interactable.SetInteractionAllowed(true);

            if (_currentCharacterServer != null)
            {
                Debug.Log($"[ControlPanelController {gameObject.name}] ServerResetOwner: {_currentCharacterServer.gameObject.name} ({_currentCharacterServer.NetworkObject.OwnerClientId}) trying to remove parent...");
                _currentCharacterServer.NetworkObject.TryRemoveParent();
            }

            _currentCharacterServer = null;

            Debug.Log($"[ControlPanelController {gameObject.name}] ServerResetOwner completed!");
        }

        private void KillSequence(ref Sequence sequence)
        {
            if(sequence == null) return;
            sequence.Kill();
            sequence = null;
        }

        private void ExitInput(InputAction.CallbackContext ctx)
        {
            ExitControlPanel();
        }

        [Button("Force Exit", EButtonEnableMode.Playmode)]
        private void DebugExitControlPanel() => ExitControlPanel();

        private void OnDrawGizmos()
        {
            //DebugExtension.DrawCapsule(Seat.position, Seat.position + (Seat.up * 2f), Color.aliceBlue, 0.5f);
            //DebugExtension.DrawArrow(Seat.position, Seat.forward * 0.5f, Color.green);

            //Draw.WireCapsule(Seat.position, Seat.position + (Seat.up * 2f), 0.5f, Color.aliceBlue);
        }

        private void EnableUI(bool enable)
        {
            if (_controlPanelUi == null) return;

            if (enable) _controlPanelUi.EnableUI();
            else _controlPanelUi.DisableUI();
        }
    }
}
