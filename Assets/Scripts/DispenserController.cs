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

        [field: SerializeField, Range(1, 200)] 
        public int SpawnableLimit { get; private set; } = 100;
        private List<PickupController> _spawned;

        [SerializeField] private string _interactDescription = "Dispense item";

        private readonly NetworkVariable<int> _currentSpawnedCount = new(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        public int CurrentSpawnedCount => _currentSpawnedCount.Value;

        public override void OnNetworkSpawn()
        {
            if (IsServer && _interactable != null)
            {
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
            _spawned.RemoveAll(s => s == null || !s.IsSpawned);

            _currentSpawnedCount.Value = _spawned.Count;
            if (_spawned.Count >= SpawnableLimit)
            {
                FailedDispenseReachedLimitClientRpc();
                return;
            }

            GameObject[] potentialBlocks = _spawnVolume.CurrentObjects;
            foreach(GameObject p in potentialBlocks)
            {
                if(p == null) continue;
                if (p.GetComponent<PickupController>() == null) continue;

                FailedDispenseHasBlockageClientRpc();
                return;
            }

            GameObject g = Instantiate(_pickupToSpawn.gameObject, _spawnVolume.transform.position, _spawnVolume.transform.rotation);
            PickupController pc = g.GetComponent<PickupController>();
            pc.NetworkObject.Spawn(false);

            _spawned.Add(pc);
            _currentSpawnedCount.Value = _spawned.Count;
            Debug.Log($"[DispenserController] {gameObject.name} spawned {g.name}!");
            OnDispenseSuccess?.Invoke(pc);
        }

        [ClientRpc]
        private void FailedDispenseReachedLimitClientRpc()
        {
            OnDispenseFailReachedLimit?.Invoke();
            Debug.LogError($"[DispenserController] {gameObject.name} has reached spawn limit");
        }

        [ClientRpc]
        private void FailedDispenseHasBlockageClientRpc()
        {
            Debug.LogError($"[DispenserController] {gameObject.name} has a blocking object!");
            OnDispenseFailHasBlockage?.Invoke();
        }


    }
}
