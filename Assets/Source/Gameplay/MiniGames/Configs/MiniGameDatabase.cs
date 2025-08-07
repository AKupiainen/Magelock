using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BrawlLine.GameModes
{
    [CreateAssetMenu(fileName = "MiniGameDatabase", menuName = "BrawlLine/Mini Game Database")]
    public class MiniGameDatabase : ScriptableObject
    {
        [Header("All Mini Games")]
        [SerializeField] private MiniGameConfig[] miniGames;
        
        private Dictionary<string, MiniGameConfig> miniGameDict;
        
        private void OnEnable()
        {
            BuildDictionary();
        }
        
        private void BuildDictionary()
        {
            miniGameDict = new Dictionary<string, MiniGameConfig>();
            
            if (miniGames != null)
            {
                foreach (var game in miniGames)
                {
                    if (game != null && !string.IsNullOrEmpty(game.miniGameId))
                    {
                        miniGameDict[game.miniGameId] = game;
                    }
                }
            }
        }
        
        public MiniGameConfig GetMiniGame(string miniGameId)
        {
            if (string.IsNullOrEmpty(miniGameId))
                throw new ArgumentException("miniGameId cannot be null or empty.", nameof(miniGameId));

            if (miniGameDict == null)
                BuildDictionary();

            if (miniGameDict == null || !miniGameDict.TryGetValue(miniGameId, out var config))
                throw new KeyNotFoundException($"MiniGame with ID '{miniGameId}' was not found.");

            return config;
        }

        private MiniGameConfig[] GetAvailableMiniGames()
        {
            return miniGames
                .Where(game => game != null && game.GetTotalPhases() > 0)
                .ToArray();
        }
        
        public MiniGameConfig SelectRandomMiniGame()
        {
            var availableGames = GetAvailableMiniGames();
            return availableGames.Length == 0 ? null : availableGames[Random.Range(0, availableGames.Length)];
        }
    }
}