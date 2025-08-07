using BrawlLine.Events;
using System.Collections.Generic;

namespace BrawlLine.GameModes
{
    public class RaceStartedEvent : IEventData
    {
        public int PlayersTotal;
        public int QualifyingPlayers;
    }

    public class RaceEndedEvent : IEventData
    {
        public List<ulong> QualifiedPlayers;
        public int TotalFinished;
        public int TotalEliminated;
    }

    public class PlayerFinishedRaceEvent : IEventData
    {
        public ulong ClientId;
        public int Position;
        public bool IsQualified;
    }

    public class PlayerEliminatedEvent : IEventData
    {
        public ulong ClientId;
        public string Reason;
        public int PlayersRemaining;
    }

    public class PlayersFinishedChangedEvent : IEventData
    {
        public int PreviousCount;
        public int CurrentCount;
        public int QualifyingPlayers;
    }

    public class PlayersAliveChangedEvent : IEventData
    {
        public int PreviousCount;
        public int CurrentCount;
    }

    public class RaceErrorEvent : IEventData
    {
        public string ErrorMessage;
    }

    public class GameEndedEvent : IEventData
    {
        public ulong WinnerClientId;
        public int TotalEliminated;
    }
}