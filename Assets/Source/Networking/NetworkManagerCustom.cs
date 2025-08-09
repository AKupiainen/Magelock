using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using MageLock.Events;
using UnityEngine.SceneManagement;


#if NETCODE_UGS
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
#endif

namespace MageLock.Networking
{
    public class NetworkManagerCustom : NetworkManager
    {
        [Header("Game Settings")]
        [SerializeField] private int maxPlayers = 12;

        [Header("Lobby Settings")]
        [SerializeField] private float lobbyHeartbeatInterval = 25f;
        
        private NetworkMode _currentNetworkMode;
        private CancellationTokenSource _matchmakingCancellationToken;
        private CancellationTokenSource _heartbeatCancellationToken;
        private string _currentLobbyId;
        public int MaxPlayers => maxPlayers;
        
        #region Unity Lifecycle

        private void Start()
        {
            RegisterNetworkEvents();
        }

        private void OnDestroy()
        {
            UnregisterNetworkEvents();
            CancelMatchmaking();
            StopHeartbeat();
        }

        #endregion

        #region Network Event Registration

        private void RegisterNetworkEvents()
        {
            OnServerStarted += HandleServerStarted;
            ConnectionApprovalCallback += HandleConnectionApproval;
            OnClientConnectedCallback += HandleClientConnected;
            OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void UnregisterNetworkEvents()
        {
            OnServerStarted -= HandleServerStarted;
            ConnectionApprovalCallback -= HandleConnectionApproval;
            OnClientConnectedCallback -= HandleClientConnected;
            OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        #endregion

        #region Public Methods

        public void StartMatch(NetworkMode mode)
        {
            _currentNetworkMode = mode;

            EventsBus.Trigger(new MatchmakingStartedEvent(mode));

            switch (mode)
            {
                case NetworkMode.Online:
                    StartOnlineClient();
                    break;
                case NetworkMode.Offline:
                    StartOfflineHost();
                    break;
                default:
                    Debug.LogError($"Unknown network mode: {mode}");
                    var errorMsg = $"Unknown network mode: {mode}";
                    EventsBus.Trigger(new MatchmakingFailedEvent(errorMsg, mode));
                    break;
            }
        }

        public void CancelMatchmaking()
        {
            Debug.Log("Cancelling matchmaking...");

            _matchmakingCancellationToken?.Cancel();
            _matchmakingCancellationToken?.Dispose();
            _matchmakingCancellationToken = null;

            StopHeartbeat();

            string previousLobbyId = _currentLobbyId;
            _currentLobbyId = null;

            if (IsClient || IsHost)
            {
                Shutdown(true);
            }

            EventsBus.Trigger(new MatchmakingCancelledEvent("User cancelled matchmaking"));
            EventsBus.Trigger(new NetworkStatusChangedEvent(_currentNetworkMode, false, false, false, previousLobbyId));

            Debug.Log("Matchmaking cancelled successfully.");
        }

        #endregion

        #region Connection Approval

        private void HandleConnectionApproval(ConnectionApprovalRequest request, ConnectionApprovalResponse response)
        {
            bool isApproved = true;
            string reason = string.Empty;

            if (!IsServer && _currentNetworkMode == NetworkMode.Offline)
            {
                isApproved = false;
                reason = "Private Match - Server not accepting connections";
            }

            if (ConnectedClientsIds.Count >= MaxPlayers)
            {
                isApproved = false;
                reason = "Lobby Full";

                EventsBus.Trigger(new LobbyFullEvent(_currentNetworkMode, ConnectedClientsIds.Count));
            }

            response.Approved = isApproved;
            response.Reason = reason;

            EventsBus.Trigger(new ConnectionApprovalEvent(
                request.ClientNetworkId,
                isApproved,
                reason,
                ConnectedClientsIds.Count,
                MaxPlayers
            ));
        }

        #endregion

        #region Network Mode Implementations

        private async void StartOnlineClient()
        {
#if NETCODE_UGS
            _matchmakingCancellationToken = new CancellationTokenSource();

            EventsBus.Trigger(new NetworkStatusChangedEvent(_currentNetworkMode, false, false, true));

            await ConnectToOnlineLobbyAsync(_matchmakingCancellationToken.Token);
#endif
        }

#if NETCODE_UGS
        private async Task ConnectToOnlineLobbyAsync(CancellationToken cancellationToken)
        {
            try
            {
                var lobbyOptions = CreateQuickJoinOptions();

                try
                {
                    Debug.Log("Searching for existing lobbies...");
                    var existingLobby = await LobbyService.Instance.QuickJoinLobbyAsync(lobbyOptions);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.Log("Matchmaking was cancelled during lobby search.");
                        return;
                    }

                    await JoinExistingLobby(existingLobby, cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        StartClient();

                        EventsBus.Trigger(new MatchmakingSuccessEvent(_currentNetworkMode, false, maxPlayers));
                        EventsBus.Trigger(new NetworkStatusChangedEvent(_currentNetworkMode, true, false, false, _currentLobbyId));
                    }
                }
                catch (LobbyServiceException ex) when (ex.Reason == LobbyExceptionReason.LobbyNotFound ||
                                                       ex.Reason == LobbyExceptionReason.NoOpenLobbies)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        Debug.Log("Matchmaking was cancelled during lobby creation.");
                        return;
                    }

                    Debug.Log("No available lobbies found. Creating new lobby and starting as host...");
                    await CreateNewLobbyAndStartHost(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        EventsBus.Trigger(new MatchmakingSuccessEvent(_currentNetworkMode, true, maxPlayers));
                        EventsBus.Trigger(new NetworkStatusChangedEvent(_currentNetworkMode, true, true, false, _currentLobbyId));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Matchmaking was cancelled by user.");
                EventsBus.Trigger(new MatchmakingCancelledEvent("Operation cancelled"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error in online connection: {ex}");

                EventsBus.Trigger(new MatchmakingFailedEvent(ex.Message, _currentNetworkMode));
                EventsBus.Trigger(new NetworkStatusChangedEvent(_currentNetworkMode, false, false, false));
            }
        }

        private async Task JoinExistingLobby(Lobby lobby, CancellationToken cancellationToken)
        {
            var allocation = await RelayService.Instance.JoinAllocationAsync(lobby.Data["RelayCode"].Value);

            if (cancellationToken.IsCancellationRequested)
                return;

            var playerOptions = new UpdatePlayerOptions
            {
                AllocationId = allocation.AllocationId.ToString()
            };

            await LobbyService.Instance.UpdatePlayerAsync(lobby.Id, AuthenticationService.Instance.PlayerId, playerOptions);

            if (cancellationToken.IsCancellationRequested)
                return;

            SetupRelayServerData(allocation.ToRelayServerData(GetConnectionType()));
            _currentLobbyId = lobby.Id;
        }

        private async Task CreateNewLobbyAndStartHost(CancellationToken cancellationToken)
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);

            if (cancellationToken.IsCancellationRequested)
                return;

            var relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            if (cancellationToken.IsCancellationRequested)
                return;

            SetupRelayServerData(allocation.ToRelayServerData(GetConnectionType()));

            var lobbyOptions = new CreateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            };

            var lobby = await LobbyService.Instance.CreateLobbyAsync(
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                maxPlayers,
                lobbyOptions
            );

            if (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to clean up lobby after cancellation: {ex.Message}");
                }
                return;
            }

            _currentLobbyId = lobby.Id;
            
            StartHeartbeat(lobby.Id);
            StartHost();
        }

        private QuickJoinLobbyOptions CreateQuickJoinOptions()
        {
            return new QuickJoinLobbyOptions
            {
                Filter = new List<QueryFilter>
                {
                    new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                }
            };
        }

        private string GetConnectionType()
        {
#if UNITY_WEBGL
            return "wss";
#else
            return "udp";
#endif
        }

        private void SetupRelayServerData(RelayServerData serverData)
        {
            var transport = NetworkConfig.NetworkTransport as UnityTransport;

            if (transport != null)
            {
                transport.SetRelayServerData(serverData);
            }
            else
            {
                Debug.LogError("UnityTransport not found!");
            }
        }

        private async void StartHeartbeat(string lobbyId)
        {
            StopHeartbeat();
            _heartbeatCancellationToken = new CancellationTokenSource();
            await HeartbeatAsync(lobbyId, _heartbeatCancellationToken.Token);
        }

        private void StopHeartbeat()
        {
            _heartbeatCancellationToken?.Cancel();
            _heartbeatCancellationToken?.Dispose();
            _heartbeatCancellationToken = null;
        }

        private async Task HeartbeatAsync(string lobbyId, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                        Debug.Log($"Heartbeat sent for lobby: {lobbyId}");
                    }
                    catch (LobbyServiceException lobbyEx)
                    {
                        Debug.LogWarning($"Heartbeat failed for lobby {lobbyId}: {lobbyEx.Message}");

                        if (lobbyEx.Reason is LobbyExceptionReason.LobbyNotFound or LobbyExceptionReason.ValidationError)
                        {
                            Debug.LogError("Lobby no longer exists. Stopping heartbeat.");
                            break;
                        }

                        Debug.LogWarning($"Lobby service error (will retry): {lobbyEx.Reason}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"General heartbeat error for lobby {lobbyId}: {ex.Message}");
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(lobbyHeartbeatInterval), cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log($"Heartbeat stopped for lobby: {lobbyId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error in heartbeat for lobby {lobbyId}: {ex}");
            }
        }
#endif

        private void StartOfflineHost()
        {
            EventsBus.Trigger(new MatchmakingSuccessEvent(NetworkMode.Offline, true, maxPlayers));
            EventsBus.Trigger(new NetworkStatusChangedEvent(NetworkMode.Offline, true, true, false));

            StartHost();
        }

        #endregion

        #region Network Event Handlers

        private void HandleServerStarted()
        {
            EventsBus.Trigger(new ServerStartedEvent(_currentNetworkMode, maxPlayers));
        }

        private void HandleClientConnected(ulong clientId)
        {
            Debug.Log($"Client {clientId} connected. Total clients: {ConnectedClientsIds.Count}/{MaxPlayers}");
            EventsBus.Trigger(new ClientConnectedEvent(clientId, ConnectedClientsIds.Count, MaxPlayers));

            if (IsServer && ConnectedClientsIds.Count >= MaxPlayers)
            {
                Debug.Log("Lobby is now full! Preparing to start game...");
            }
        }

        private async void HandleClientDisconnected(ulong clientId)
        {
            string disconnectReason = DisconnectReason ?? string.Empty;
            bool wasHost = IsHost && LocalClientId == clientId;
            int remainingClients = IsServer ? ConnectedClientsIds.Count : 0;

            EventsBus.Trigger(new ClientDisconnectedEvent(clientId, remainingClients, disconnectReason, wasHost));

            if (!IsServer && !string.IsNullOrEmpty(DisconnectReason))
            {
                Debug.LogWarning($"Connection denied: {DisconnectReason}");
                return;
            }

            if (IsServer && LocalClientId != clientId)
            {
                Debug.Log($"Client {clientId} disconnected");
                return;
            }

            if (IsHost && !string.IsNullOrEmpty(_currentLobbyId))
            {
                await CleanupLobbyAsync(_currentLobbyId);
            }
        }

#if NETCODE_UGS
        private async Task CleanupLobbyAsync(string lobbyId)
        {
            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
                Debug.Log($"Successfully cleaned up lobby: {lobbyId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to cleanup lobby {lobbyId}: {ex.Message}");
            }
        }
#endif
        #endregion

        #region Network Object Management

        private void OnClientDisconnect(NetworkObject networkObject)
        {
            if (networkObject?.IsPlayerObject != true) return;

            try
            {
                networkObject.Despawn();

                if (networkObject.gameObject != null)
                {
                    Destroy(networkObject.gameObject);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Error cleaning up disconnected player object: {ex.Message}");
            }
        }

        public void Destroy(NetworkObject networkObject)
        {
            OnClientDisconnect(networkObject);
        }

        #endregion

        public void LoadSceneForAllClients(string sceneName, Action onSuccess = null, Action<string> onFailure = null)
        {
            if (!IsHost && !IsServer)
            {
                Debug.LogWarning("Only the host or server can load scenes for all clients!");
                onFailure?.Invoke("Only the host or server can load scenes for all clients!");
                return;
            }

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("Scene name cannot be null or empty!");
                onFailure?.Invoke("Scene name cannot be null or empty!");
                return;
            }

            void OnSceneLoadCompleted(string loadedSceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
            {
                if (loadedSceneName == sceneName)
                {
                    SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
                    SceneManager.OnLoadComplete -= OnSceneLoadComplete;

                    if (clientsTimedOut.Count > 0)
                    {
                        string errorMsg = $"Scene '{sceneName}' loading timed out for {clientsTimedOut.Count} clients";
                        Debug.LogError(errorMsg);
                        onFailure?.Invoke(errorMsg);
                    }
                    else
                    {
                        Debug.Log($"Scene '{sceneName}' loaded successfully for all clients");
                        onSuccess?.Invoke();
                    }
                }
            }

            void OnSceneLoadComplete(ulong clientId, string loadedSceneName, LoadSceneMode loadSceneMode)
            {
                if (loadedSceneName == sceneName)
                {
                    Debug.Log($"Client {clientId} completed loading scene '{sceneName}'");
                }
            }

            SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
            SceneManager.OnLoadComplete += OnSceneLoadComplete;

            var sceneEventProgressStatus = SceneManager.LoadScene(sceneName, LoadSceneMode.Single);

            if (sceneEventProgressStatus != SceneEventProgressStatus.Started)
            {
                string errorMsg = $"Failed to start loading scene '{sceneName}'. Status: {sceneEventProgressStatus}";
                Debug.LogError(errorMsg);
                
                SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
                SceneManager.OnLoadComplete -= OnSceneLoadComplete;
                
                onFailure?.Invoke(errorMsg);
            }
            else
            {
                Debug.Log($"Started loading scene '{sceneName}' for all clients...");
            }
        }
    }

    public enum NetworkMode
    {
        Online,
        Offline
    }
}