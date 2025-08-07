using System;
using System.Collections.Generic;
using System.Linq;

namespace BrawlLine.GameModes
{
    [Serializable]
    public class ImmediateEliminationLogic : BaseGameModeLogic
    {
        private readonly HashSet<ulong> eliminatedPlayers = new();

        public override void OnPhaseStart(int phaseNumber, int totalPlayers, int qualifyingPlayers)
        {
            base.OnPhaseStart(phaseNumber, totalPlayers, qualifyingPlayers);
            eliminatedPlayers.Clear();
        }

        public override bool ShouldEndPhase(int finishedCount, int qualifyingCount, int totalPlayers)
        {
            int playersAlive = totalPlayers - eliminatedPlayers.Count;
            return playersAlive <= 1 || finishedCount > 0;
        }

        public override bool ShouldEndGame(int playersAlive, int currentPhase, int totalPhases)
        {
            return playersAlive <= 1 || currentPhase >= totalPhases;
        }

        public override List<ulong> GetPlayersToEliminate(List<ulong> finishedPlayers, int qualifyingCount, List<ulong> allConnectedPlayers)
        {
            return allConnectedPlayers
                .Where(p => !finishedPlayers.Contains(p) && !eliminatedPlayers.Contains(p))
                .ToList();
        }

        public override void OnPlayerEliminated(ulong clientId, string reason)
        {
            base.OnPlayerEliminated(clientId, reason);
            eliminatedPlayers.Add(clientId);
        }

        public override float GetRespawnDelay(ulong clientId, float defaultDelay)
        {
            return float.MaxValue;
        }
    }
}