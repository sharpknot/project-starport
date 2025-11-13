using NaughtyAttributes;
using Starport.Characters;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Starport
{
    public class GameSessionManager : MonoBehaviour
    {
        private RelayManager _relayManager;
        private GameStateManager _stateManager;

        [SerializeField] private NetworkObject _characterObject;
        [SerializeField] private Transform[] _spawnPoints;

        [SerializeField] private NetworkObject _prespawnObject;
        [SerializeField] private int _hostConnectionCount = 5;
        [SerializeField] private bool _startOnAwake = false;
        private Dictionary<ulong, NetworkObject> _clientObjects;
        private NetworkObject _hostObject;
        [field: SerializeField, ReadOnly]
        public string HostJoinCode { get; private set; } = "";

        public void Disconnect()
        {
            if (_relayManager != null)
            {
                _relayManager.OnClientConnectedAsHost -= ClientConnectedAsHost;
                _relayManager.OnClientDisconnectedAsHost -= ClientDisconnectedAsHost;

                _relayManager.Disconnect();
            }
        }


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _relayManager = RelayManager.Instance;
            _stateManager = GameStateManager.Instance;

            if (_startOnAwake)
            {
                if (_stateManager.IsAttemptingToJoinHost(out string targetJoinCode))
                    _ = StartJoiningHost(targetJoinCode);
                else if (_stateManager.IsAttemptingToHost())
                    _ = StartHosting();
                else
                    StartOffline();
            }
        }

        private void OnDestroy()
        {
            if(_relayManager != null)
            {
                _relayManager.OnDisconnectAsHost -= DisconnectAsHost;
                _relayManager.OnDisconnectAsClient -= DisconnectAsClient;
            }

            Disconnect();    
        }

        private void OnValidate()
        {
            _hostConnectionCount = Mathf.Max(1, _hostConnectionCount);
        }

        private async Task StartHosting()
        {
            bool success = await _relayManager.StartHosting(_hostConnectionCount);
            _stateManager.StopHostAttempt();

            if (success) HostingSuccess();
            else HostingFailed();
        }

        private void HostingSuccess()
        {
            SpawnHostCharacter();

            _relayManager.IsHosting(out string joinCode);
            HostJoinCode = joinCode;

            _relayManager.OnClientConnectedAsHost += ClientConnectedAsHost;
            _relayManager.OnClientDisconnectedAsHost += ClientDisconnectedAsHost;      
            _relayManager.OnDisconnectAsHost += DisconnectAsHost;
        }

        private void SpawnHostCharacter()
        {
            Transform spawnPoint = GetRandomSpawnPosition();
            if (spawnPoint == null) return;

            _hostObject = SpawnCharacter(NetworkManager.ServerClientId, spawnPoint.position, spawnPoint.rotation);
        }

        private NetworkObject SpawnCharacter(ulong playerId, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (_characterObject == null) return null;
            GameObject g = Instantiate(_characterObject.gameObject, spawnPosition, spawnRotation);
            NetworkObject netObj = g.GetComponent<NetworkObject>();
            netObj.SpawnAsPlayerObject(playerId, true);
            return netObj;
        }

        private void HostingFailed()
        {

        }

        private void ClientConnectedAsHost(ulong clientId)
        {
            _clientObjects ??= new();
            DespawnClient(clientId);

            Transform spawnPoint = GetRandomSpawnPosition();
            if(spawnPoint == null) return;
            NetworkObject netObj = SpawnCharacter(clientId, spawnPoint.position, spawnPoint.rotation);
            if(netObj == null) return;

            _clientObjects.Add(clientId, netObj);
        }

        private void ClientDisconnectedAsHost(ulong clientId)
        {
            _clientObjects ??= new();
            DespawnClient(clientId);
        }

        private void DespawnClient(ulong clientId)
        {
            _clientObjects ??= new();
            if (!_clientObjects.ContainsKey(clientId))
                return;

            NetworkObject netObj = _clientObjects[clientId];
            if(netObj != null)
            {
                HandleDespawningCharacter(netObj);
                if(netObj.IsSpawned)
                    netObj.Despawn(true);
            }

            _clientObjects.Remove(clientId);
        }

        private void HandleDespawningCharacter(NetworkObject netObj)
        {
            // Remove owned connected objects
        }

        private async Task StartJoiningHost(string joinCode)
        {
            bool success = await _relayManager.StartJoining(joinCode);
            _stateManager.StopJoinHostAttempt();

            if (success) JoiningSuccess();
            else JoiningFailed();
        }

        private void JoiningSuccess()
        {
            if (_relayManager != null)
                _relayManager.OnDisconnectAsClient += DisconnectAsClient;
        }

        private void JoiningFailed()
        {

        }

        private void StartOffline()
        {

        }

        private void DisconnectAsHost()
        {
            if(_relayManager != null)
                _relayManager.OnDisconnectAsHost -= DisconnectAsHost;

            Debug.Log("[GameSessionManager] DisconnectAsHost");
        }

        private void DisconnectAsClient()
        {
            if (_relayManager != null)
                _relayManager.OnDisconnectAsClient -= DisconnectAsClient;

            Debug.Log("[GameSessionManager] DisconnectAsClient");
        }


        private Transform GetRandomSpawnPosition()
        {
            if(_spawnPoints == null) return null;

            List<Transform> points = new(_spawnPoints);
            points.RemoveAll(t => t == null);
            return points[Random.Range(0, points.Count)];
        }

        public static NetworkObject SpawnObjectAsHost(NetworkObject networkObject, Vector3 worldPosition, Quaternion worldRotation, Transform parent = null)
        {
            if (networkObject == null) return null;

            GameObject g = Instantiate(networkObject.gameObject, worldPosition, worldRotation);
            if (parent != null)
                g.transform.SetParent(parent, true);

            NetworkObject netObj = g.GetComponent<NetworkObject>();

            if(NetworkManager.Singleton != null 
                && NetworkManager.Singleton.IsListening 
                && NetworkManager.Singleton.IsHost)
            {
                netObj.Spawn();
            }

            return netObj;
        }

        [SerializeField] private string _debugHostJoinCode = "";
        private bool _isRunningDebugProcess = false;
        [Button("Host", EButtonEnableMode.Playmode)]
        private async void DebugHost()
        {
            if (_isRunningDebugProcess) return;

            _isRunningDebugProcess = true;
            await StartHosting();
            _isRunningDebugProcess = false;
        }

        [Button("Join", EButtonEnableMode.Playmode)]
        private async void DebugJoinHost()
        {
            if (_isRunningDebugProcess) return;

            _isRunningDebugProcess = true;
            await StartJoiningHost(_debugHostJoinCode);
            _isRunningDebugProcess = false;
        }

        [Button("Disconnect", EButtonEnableMode.Playmode)]
        private void DebugDisconnect() => Disconnect();

        [Button("Simulate Disconnect", EButtonEnableMode.Playmode)]
        private void DebugSimulateDisconnect()
        {
            if(NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.Shutdown();
        }

    }
}
