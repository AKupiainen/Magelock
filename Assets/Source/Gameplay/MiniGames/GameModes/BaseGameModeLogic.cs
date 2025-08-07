using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MageLock.DependencyInjection;

namespace MageLock.GameModes
{
    [Serializable]
    public abstract class BaseGameModeLogic : IGameModeLogic
    {
        protected GameStateManager GameStateManager;
        protected PlayerSpawnManager PlayerSpawnManager;

        public virtual void Initialize(GameStateManager stateManager, PlayerSpawnManager playerSpawnManager)
        {
            GameStateManager = stateManager;
            PlayerSpawnManager = playerSpawnManager;
            
            OnInitialize();
        }

        protected virtual void OnInitialize() { }

        public virtual void OnPhaseStart(int phaseNumber, int totalPlayers, int qualifyingPlayers)
        {
            Debug.Log($"[{GetType().Name}] Phase {phaseNumber} started with {totalPlayers} players, {qualifyingPlayers} qualifying");
        }

        public virtual void OnPlayerFinished(ulong clientId, int position)
        {
            Debug.Log($"[{GetType().Name}] Player {clientId} finished at position {position}");
        }

        public virtual void OnPlayerEliminated(ulong clientId, string reason)
        {
            Debug.Log($"[{GetType().Name}] Player {clientId} eliminated: {reason}");
        }

        public virtual void OnPlayerDisconnected(ulong clientId)
        {
            Debug.Log($"[{GetType().Name}] Player {clientId} disconnected");
        }

        public virtual bool ShouldEndPhase(int finishedCount, int qualifyingCount, int totalPlayers)
        {
            return finishedCount >= qualifyingCount;
        }

        public virtual List<ulong> GetPlayersToEliminate(List<ulong> finishedPlayers, int qualifyingCount, List<ulong> allConnectedPlayers)
        {
            var qualifiedPlayers = finishedPlayers.Take(qualifyingCount).ToList();
            return allConnectedPlayers.Where(p => !qualifiedPlayers.Contains(p)).ToList();
        }

        public virtual bool ShouldEndGame(int playersAlive, int currentPhase, int totalPhases)
        {
            return playersAlive <= 1 || currentPhase >= totalPhases;
        }

        public virtual float GetRespawnDelay(ulong clientId, float defaultDelay)
        {
            return defaultDelay;
        }

        public virtual void Update() { }

        public virtual void OnPhaseEnd(int phaseNumber, List<ulong> qualifiedPlayers, List<ulong> eliminatedPlayers)
        {
            Debug.Log($"[{GetType().Name}] Phase {phaseNumber} ended. Qualified: {qualifiedPlayers.Count}, Eliminated: {eliminatedPlayers.Count}");
        }

        public virtual void OnGameEnd(ulong winnerClientId)
        {
            Debug.Log($"[{GetType().Name}] Game ended. Winner: {winnerClientId}");
        }
    }
}