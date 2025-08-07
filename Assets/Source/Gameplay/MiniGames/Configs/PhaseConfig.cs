using System;
using UnityEngine;

namespace BrawlLine.GameModes
{
    [Serializable]
    public class PhaseConfig
    {
        [Header("Phase Settings")]
        public GameObject levelPrefab;
        public int qualifyingPlayers = 20; 

        [Header("Gameplay")]
        public float respawnDelay = 2f;
    }
}