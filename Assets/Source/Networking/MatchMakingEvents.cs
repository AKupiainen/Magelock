using MageLock.Events;

namespace MageLock.Networking
{
    public struct MatchmakingStartedEvent : IEventData
    {
        public NetworkMode NetworkMode { get; }

        public MatchmakingStartedEvent(NetworkMode networkMode)
        {
            NetworkMode = networkMode;
        }
    }

    public struct MatchmakingCancelledEvent : IEventData
    {
        public string Reason { get; }

        public MatchmakingCancelledEvent(string reason = "User cancelled")
        {
            Reason = reason;
        }
    }

    public struct MatchmakingFailedEvent : IEventData
    {
        public string ErrorMessage { get; }
        public NetworkMode AttemptedMode { get; }

        public MatchmakingFailedEvent(string errorMessage, NetworkMode attemptedMode)
        {
            ErrorMessage = errorMessage;
            AttemptedMode = attemptedMode;
        }
    }

    public struct MatchmakingSuccessEvent : IEventData
    {
        public NetworkMode NetworkMode { get; }
        public bool IsHost { get; }
        public int MaxPlayers { get; }

        public MatchmakingSuccessEvent(NetworkMode networkMode, bool isHost, int maxPlayers)
        {
            NetworkMode = networkMode;

            IsHost = isHost;
            MaxPlayers = maxPlayers;
        }
    }

    public struct ServerStartedEvent : IEventData
    {
        public NetworkMode NetworkMode { get; }
        public int MaxPlayers { get; }

        public ServerStartedEvent(NetworkMode networkMode, int maxPlayers)
        {
            NetworkMode = networkMode;
            MaxPlayers = maxPlayers;
        }
    }

    public struct ClientConnectedEvent : IEventData
    {
        public ulong ClientId { get; }
        public int TotalClients { get; }
        public int MaxPlayers { get; }
        public bool IsLobbyFull { get; }

        public ClientConnectedEvent(ulong clientId, int totalClients, int maxPlayers)
        {
            ClientId = clientId;
            TotalClients = totalClients;
            MaxPlayers = maxPlayers;
            IsLobbyFull = totalClients >= maxPlayers;
        }
    }

    public struct ClientDisconnectedEvent : IEventData
    {
        public ulong ClientId { get; }
        public int RemainingClients { get; }
        public string DisconnectReason { get; }
        public bool WasHost { get; }

        public ClientDisconnectedEvent(ulong clientId, int remainingClients, string disconnectReason = "", bool wasHost = false)
        {
            ClientId = clientId;
            RemainingClients = remainingClients;
            DisconnectReason = disconnectReason;
            WasHost = wasHost;
        }
    }

    public struct LobbyFullEvent : IEventData
    {
        public NetworkMode NetworkMode { get; }
        public int PlayerCount { get; }

        public LobbyFullEvent(NetworkMode networkMode, int playerCount)
        {
            NetworkMode = networkMode;
            PlayerCount = playerCount;
        }
    }

    public struct GameStartingEvent : IEventData
    {
        public string GameSceneName { get; }
        public int PlayerCount { get; }

        public GameStartingEvent(string gameSceneName, int playerCount)
        {
            GameSceneName = gameSceneName;
            PlayerCount = playerCount;
        }
    }

    public struct ConnectionApprovalEvent : IEventData
    {
        public ulong ClientId { get; }
        public bool IsApproved { get; }
        public string Reason { get; }
        public int CurrentPlayers { get; }
        public int MaxPlayers { get; }

        public ConnectionApprovalEvent(ulong clientId, bool isApproved, string reason, int currentPlayers, int maxPlayers)
        {
            ClientId = clientId;
            IsApproved = isApproved;
            Reason = reason;
            CurrentPlayers = currentPlayers;
            MaxPlayers = maxPlayers;
        }
    }

    public struct NetworkStatusChangedEvent : IEventData
    {
        public NetworkMode NetworkMode { get; }
        public bool IsConnected { get; }
        public bool IsHost { get; }
        public bool IsMatchmaking { get; }
        public string LobbyId { get; }

        public NetworkStatusChangedEvent(NetworkMode networkMode, bool isConnected, bool isHost, bool isMatchmaking, string lobbyId = "")
        {
            NetworkMode = networkMode;
            IsConnected = isConnected;
            IsHost = isHost;
            IsMatchmaking = isMatchmaking;
            LobbyId = lobbyId;
        }
    }
}