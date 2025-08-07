using System;
using System.Collections.Generic;
using MageLock.Audio;
using MageLock.Localization;
using UnityEngine;

namespace MageLock.GameModes
{
    [Serializable]
    public class MiniGameConfig
    {
        [Header("Basic Info")]
        public string miniGameId;
        public LocString displayName;
        public LocString description;
    
        [Header("Phases")]
        [SerializeField] private List<PhaseConfig> phases = new();
        
        [Header("Visual")]
        public BGM backgroundMusic;
        
        [Header("Game Mode Logic")]
        [SerializeReference, SubclassSelector]
        private IGameModeLogic gameModeLogic;
        
        public IGameModeLogic GameModeLogic => gameModeLogic;
        
        public PhaseConfig GetPhase(int phaseNumber)
        {
            if (phaseNumber < 1 || phaseNumber > phases.Count)
                return null;
                
            return phases[phaseNumber - 1];
        }
        
        public int GetTotalPhases()
        {
            return phases?.Count ?? 0;
        }
        
        public int GetQualifyingPlayersForPhase(int phaseNumber, int currentPlayers, int maxPlayers)
        {
            var phase = GetPhase(phaseNumber);

            if (phase == null) return 0;
            
            if (phaseNumber >= GetTotalPhases())
                return 1;
            
            float ratio = (float)phase.qualifyingPlayers / maxPlayers;
            return Mathf.Max(1, Mathf.RoundToInt(currentPlayers * ratio));
        }

        public void InitializeGameModeLogic(GameStateManager gameStateManager, PlayerSpawnManager playerSpawnManager)
        {
            if (gameModeLogic != null)
            {
                gameModeLogic.Initialize(gameStateManager, playerSpawnManager);
                Debug.Log($"[MiniGameConfig] Initialized game mode logic: {gameModeLogic.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"[MiniGameConfig] No game mode logic assigned for mini-game: {miniGameId}");
            }
        }
    }
}