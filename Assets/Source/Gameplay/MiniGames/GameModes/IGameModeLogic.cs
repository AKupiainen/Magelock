using System.Collections.Generic;


namespace BrawlLine.GameModes
{
    public interface IGameModeLogic
    {
        /// <summary>
        /// Initialize the game mode logic with required dependencies
        /// </summary>
        void Initialize(GameStateManager gameStateManager, PlayerSpawnManager playerSpawnManager);

        /// <summary>
        /// Called when a phase starts
        /// </summary>
        void OnPhaseStart(int phaseNumber, int totalPlayers, int qualifyingPlayers);

        /// <summary>
        /// Called when a player finishes the phase
        /// </summary>
        void OnPlayerFinished(ulong clientId, int position);

        /// <summary>
        /// Called when a player is eliminated
        /// </summary>
        void OnPlayerEliminated(ulong clientId, string reason);

        /// <summary>
        /// Called when a player disconnects
        /// </summary>
        void OnPlayerDisconnected(ulong clientId);

        /// <summary>
        /// Check if the phase should end based on current conditions
        /// </summary>
        bool ShouldEndPhase(int finishedCount, int qualifyingCount, int totalPlayers);

        /// <summary>
        /// Determine which players should be eliminated at phase end
        /// </summary>
        List<ulong> GetPlayersToEliminate(List<ulong> finishedPlayers, int qualifyingCount, List<ulong> allConnectedPlayers);

        /// <summary>
        /// Check if the entire game should end
        /// </summary>
        bool ShouldEndGame(int playersAlive, int currentPhase, int totalPhases);

        /// <summary>
        /// Get custom respawn delay for a specific player (optional override)
        /// </summary>
        float GetRespawnDelay(ulong clientId, float defaultDelay);

        /// <summary>
        /// Called every frame to handle custom logic updates
        /// </summary>
        void Update();

        /// <summary>
        /// Called when the phase ends
        /// </summary>
        void OnPhaseEnd(int phaseNumber, List<ulong> qualifiedPlayers, List<ulong> eliminatedPlayers);

        /// <summary>
        /// Called when the entire game ends
        /// </summary>
        void OnGameEnd(ulong winnerClientId);
    }
}