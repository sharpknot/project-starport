using NaughtyAttributes;
using System.Collections.Generic;
using System.Collections;
using Starport.Characters;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode.Components;

namespace Starport
{
    [RequireComponent(typeof(NetworkObject), typeof(NetworkTransform))]
    public class DispenserController : NetworkBehaviour
    {
        [SerializeField, Required] private InteractableController _interactable;
        [SerializeField, Required] private TriggerHelper _spawnVolume;
        [SerializeField, Required] private PickupController _pickupToSpawn;

        public event UnityAction OnDispenseFailHasBlockage, OnDispenseFailReachedLimit;
        public event UnityAction<PickupController> OnDispenseSuccess;

        [SerializeField, Range(1, 200)] private int _spawnableLimit = 100;
        private List<PickupController> _spawned;

        [SerializeField] private string _interactDescription = "Dispense item";

        public override void OnNetworkSpawn()
        {
            if (IsServer && _interactable != null)
            {
                if (!_interactable.IsSpawned)
                    _interactable.NetworkObject.Spawn();

                _interactable.SetDescription(_interactDescription);
                _interactable.OnInteractAttemptResultServer += Dispense;
            }
                

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            if (_interactable != null)
                _interactable.OnInteractAttemptResultServer -= Dispense;

            base.OnNetworkDespawn();
        }

        private void Dispense(bool success, CharacterNetworkManager interactingCharacter)
        {
            Debug.Log($"[DispenserController] {gameObject.name} starting to spawn!");
            if (!success) return;
            if(_pickupToSpawn == null)
            {
                Debug.LogError($"[DispenserController] {gameObject.name} is missing pickup to spawn!");
                return;
            }

            if(_spawnVolume == null)
            {
                Debug.LogError($"[DispenserController] {gameObject.name} is missing spawn volume!");
                return;
            }

            _spawned ??= new();
            if(_spawned.Count >= _spawnableLimit)
            {
                Debug.LogError($"[DispenserController] {gameObject.name} has reached spawn limit");
                OnDispenseFailReachedLimit?.Invoke();
                return;
            }

            GameObject[] potentialBlocks = _spawnVolume.CurrentObjects;
            foreach(GameObject p in potentialBlocks)
            {
                if(p == null) continue;
                if (p.GetComponent<PickupController>() == null) continue;

                Debug.LogError($"[DispenserController] {gameObject.name} has a blocking object {p.name}");
                OnDispenseFailHasBlockage?.Invoke();
                return;
            }

            GameObject g = Instantiate(_pickupToSpawn.gameObject, _spawnVolume.transform.position, _spawnVolume.transform.rotation);
            PickupController pc = g.GetComponent<PickupController>();
            pc.NetworkObject.Spawn(false);

            _spawned.Add(pc);
            Debug.Log($"[DispenserController] {gameObject.name} spawned {g.name}!");
            OnDispenseSuccess?.Invoke(pc);
        }
    }
}
