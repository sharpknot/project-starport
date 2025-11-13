using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.Events;

namespace Starport
{
    public class RelayManager : MonoBehaviour
    {
        public static RelayManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<RelayManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("RelayManager");
                        _instance = go.AddComponent<RelayManager>();
                    }
                }
                return _instance;
            }
        }
        private static RelayManager _instance;

        private bool _isDoingRelayProcess = false;
        private JoinAllocation _clientAllocation = null;    // Allocation as client
        private Allocation _hostAllocation = null;      // Allocation as host
        private string _hostJoinCode = "";      // Join code when hosting server
        private string _clientJoinCode = "";    // Join code of the host that the client is currently joined
        private NetworkManager _networkManager = null;
        private UnityTransport _transport = null;

        public event UnityAction OnDisconnectAsHost;
        public event UnityAction OnDisconnectAsClient;

        public event UnityAction<ulong> OnClientConnectedAsHost;
        public event UnityAction<ulong> OnClientDisconnectedAsHost;

        public event UnityAction<bool> OnHostingAttemptComplete;
        public event UnityAction<bool> OnJoiningAttemptComplete;

        public bool IsAttemptingHost { get; private set; } = false;
        public bool IsAttemptingJoin {  get; private set; } = false;

        public bool IsHosting(out string hostingJoinCode)
        {
            hostingJoinCode = "";
            if(_hostAllocation == null)
                return false;
            hostingJoinCode = _hostJoinCode;
            return true;
        }

        public bool IsClient(out string currentlyJoinedHostJoinCode)
        {
            currentlyJoinedHostJoinCode = "";
            if(_clientAllocation == null)
                return false;

            currentlyJoinedHostJoinCode = _clientJoinCode;
            return true;
        }

        private void Awake()
        {
            // If an instance already exists and it's not this, destroy the duplicate
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Assign and make persistent
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public async Task<bool> StartHosting(int maxConnections)
        {
            maxConnections = Mathf.Max(maxConnections, 1);

            if (IsHosting(out string hostingJoinCode))
            {
                Debug.LogError($"[RelayManager] Unable to host: Currently already hosting game with join code {hostingJoinCode}!");
                OnHostingAttemptComplete?.Invoke(false);
                return false;
            }

            if (IsClient(out string currentlyJoinedHostJoinCode))
            {
                Debug.LogError($"[RelayManager] Unable to host: Currently already joined a game as a client! Currently joined host join code {currentlyJoinedHostJoinCode}");
                OnHostingAttemptComplete?.Invoke(false);
                return false;
            }

            if (_isDoingRelayProcess)
            {
                Debug.LogError($"[RelayManager] Unable to start host: Currently in a relay process...");
                OnHostingAttemptComplete?.Invoke(false);
                return false;
            }

            ClearRelayData();

            try
            {
                _isDoingRelayProcess = true;
                IsAttemptingHost = true;

                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[RelayManager] Signed in as: {AuthenticationService.Instance.PlayerId}");
                }

                NetworkManager networkManager = NetworkManager.Singleton;

                if (networkManager == null)
                {
                    Debug.LogError($"[RelayManager] Unable to start host: Network manager singleton is null!");
                    _isDoingRelayProcess = false;
                    IsAttemptingHost = false;
                    OnHostingAttemptComplete?.Invoke(false);
                    return false;
                }

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
                Debug.Log("[RelayManager] Host Relay allocation created.");

                // Get transport
                UnityTransport transport = networkManager.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError($"[RelayManager] Unable to start host: Transport is null!");
                    _isDoingRelayProcess = false;
                    IsAttemptingHost = false;
                    OnHostingAttemptComplete?.Invoke(false);
                    return false;
                }

                RelayServerData serverData = AllocationUtils.ToRelayServerData(allocation, "dtls");
                transport.SetRelayServerData(serverData);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log($"[RelayManager] Host Relay Join Code: {joinCode}");

                if(!networkManager.StartHost())
                {
                    Debug.LogError($"[RelayManager] Unable to start host: Network manager failed to start host!");
                    _isDoingRelayProcess = false;
                    IsAttemptingHost = false;
                    return false;
                }

                _hostAllocation = allocation;
                _hostJoinCode = joinCode;
                _isDoingRelayProcess = false;
                IsAttemptingHost = false;

                _networkManager = networkManager;
                _transport = transport;

                OnHostingAttemptComplete?.Invoke(true);
                SubscribeDisconnectEvents();
            }
            catch (System.Exception e)
            {
                _isDoingRelayProcess = false;
                IsAttemptingHost = false;
                OnHostingAttemptComplete?.Invoke(false);
                Debug.LogError($"[RelayManager] Unable to start host: {e.Message}");
                return false;
            }

            Debug.Log($"[RelayManager] Host Relay created! Join code {_hostJoinCode}");
            return true;
        }

        public async Task<bool> StartJoining(string joinCode)
        {
            if (IsHosting(out string hostingJoinCode))
            {
                Debug.LogError($"[RelayManager] Unable to join: Currently already hosting game with join code {hostingJoinCode}!");
                OnJoiningAttemptComplete?.Invoke(false);
                return false;
            }

            if (IsClient(out string currentlyJoinedHostJoinCode))
            {
                Debug.LogError($"[RelayManager] Unable to join: Currently already joined a game as a client! Currently joined host join code {currentlyJoinedHostJoinCode}");
                OnJoiningAttemptComplete?.Invoke(false);
                return false;
            }

            if (_isDoingRelayProcess)
            {
                Debug.LogError($"[RelayManager] Unable to join host: Currently in a relay process...");
                OnJoiningAttemptComplete?.Invoke(false);
                return false;
            }

            ClearRelayData();

            try
            {
                _isDoingRelayProcess = true;
                IsAttemptingJoin = true;

                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[RelayManager] Signed in as: {AuthenticationService.Instance.PlayerId}");
                }

                NetworkManager networkManager = NetworkManager.Singleton;
                if (networkManager == null)
                {
                    Debug.LogError($"[RelayManager] Unable to join host: Network manager singleton is null!");
                    _isDoingRelayProcess = false;
                    IsAttemptingJoin = false;
                    OnJoiningAttemptComplete?.Invoke(false);
                    return false;
                }

                // Join the host
                JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
                Debug.Log("[RelayManager] Successfully joined Relay allocation");


                UnityTransport transport = networkManager.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("[RelayManager] Unable to join host: UnityTransport is null");
                    _isDoingRelayProcess = false;
                    IsAttemptingJoin = false;
                    OnJoiningAttemptComplete?.Invoke(false);
                    return false;
                }

                RelayServerData serverData = AllocationUtils.ToRelayServerData(joinAlloc, "dtls");
                transport.SetRelayServerData(serverData);

                if (!networkManager.StartClient())
                {
                    Debug.LogError("[RelayManager] Unable to join host: NetworkManager failed to start client");
                    _isDoingRelayProcess = false;
                    IsAttemptingJoin = false;
                    OnJoiningAttemptComplete?.Invoke(false);
                    return false;
                }

                _clientAllocation = joinAlloc;
                _clientJoinCode = joinCode;
                _isDoingRelayProcess = false;
                IsAttemptingJoin = false;

                _networkManager = networkManager;
                _transport = transport;

                OnJoiningAttemptComplete?.Invoke(true);
                SubscribeDisconnectEvents();

            }
            catch (System.Exception e)
            {
                _isDoingRelayProcess = false;
                IsAttemptingJoin = false;
                OnJoiningAttemptComplete?.Invoke(false);
                Debug.LogError($"[RelayManager] Unable to join host: {e.Message}");
                return false;
            }

            Debug.Log($"[RelayManager] Successfully joined host with join code {_clientJoinCode}!");
            return true;
        }

        public bool Disconnect()
        {
            if (_networkManager == null)
                return false;

            UnsubscribeDisconnectEvents();
            _networkManager.Shutdown();

            if (IsHosting(out _))
                OnDisconnectAsHost?.Invoke();

            if (IsClient(out _))
                OnDisconnectAsClient?.Invoke();

            ClearRelayData();
            Debug.Log($"[RelayManager] Disconnected!");
            return true;
        }

        private void UnsubscribeDisconnectEvents()
        {
            if (_networkManager != null)
            {
                _networkManager.OnClientDisconnectCallback -= NetworkManagerDisconnect;

                _networkManager.OnClientConnectedCallback -= ClientConnectedAsHost;
                _networkManager.OnClientDisconnectCallback -= ClientDisconnectedAsHost;
            }
                
            if (_transport != null)
                _transport.OnTransportEvent -= TransportEventDisconnect;

            Application.quitting -= ShutdownDisconnect;
        }

        private void SubscribeDisconnectEvents()
        {
            UnsubscribeDisconnectEvents();

            if (_networkManager != null)
            {
                _networkManager.OnClientDisconnectCallback += NetworkManagerDisconnect;
                if(_networkManager.IsHost)
                {
                    _networkManager.OnClientConnectedCallback += ClientConnectedAsHost;
                    _networkManager.OnClientDisconnectCallback += ClientDisconnectedAsHost;
                }
            }

            if (_transport != null)
                _transport.OnTransportEvent += TransportEventDisconnect;

            Application.quitting += ShutdownDisconnect;
        }
        
        private void SafeDisconnect()
        {
            UnsubscribeDisconnectEvents();

            if(IsHosting(out _))
                OnDisconnectAsHost?.Invoke();

            if(IsClient(out _))
                OnDisconnectAsClient?.Invoke();

            ClearRelayData();

            // Shutdown if host
            if (_networkManager != null && _networkManager.IsListening)
            {
                _networkManager.Shutdown();
            }
        }

        private void NetworkManagerDisconnect(ulong clientId)
        {
            if (_networkManager == null)
                return;

            if (_networkManager.LocalClientId != clientId)
                return;
            
            SafeDisconnect();
        }

        private void TransportEventDisconnect(NetworkEvent netEvent, ulong clientId, ArraySegment<byte> payload, float receiveTime)
        {
            if (netEvent != NetworkEvent.Disconnect)
                return;

            if (_networkManager == null)
                return;

            if (_networkManager.LocalClientId != clientId)
                return;

            SafeDisconnect();
        }

        private void ShutdownDisconnect()
        {
            SafeDisconnect();
        }

        private void ClearRelayData()
        {
            _clientAllocation = null;
            _hostAllocation = null;
            _hostJoinCode = "";
            _clientJoinCode = "";
            _networkManager = null;
            _transport = null;
        }

        private void ClientConnectedAsHost(ulong clientId)
        {
            if(clientId == NetworkManager.ServerClientId) return;
            OnClientConnectedAsHost?.Invoke(clientId);
        }

        private void ClientDisconnectedAsHost(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId) return;
            OnClientDisconnectedAsHost?.Invoke(clientId);
        }
    }
}
