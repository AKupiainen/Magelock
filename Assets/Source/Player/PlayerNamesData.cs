using UnityEngine;
using System.Collections.Generic;
using MageLock.JsonScriptableObject;

namespace MageLock.Player
{
    [CreateAssetMenu(fileName = "PlayerNamesData", menuName = "MageLock/Player Names Data")]
    public class PlayerNamesData : JsonScriptableObjectBase
    {
        [SerializeField] private List<string> playerNames = new();
        
        public string GetRandomName()
        {
            if (playerNames.Count == 0)
            {
                return "Player";
            }
            
            int randomIndex = Random.Range(0, playerNames.Count);
            return playerNames[randomIndex];
        }
        
        public List<string> GetAllNames()
        {
            return new List<string>(playerNames);
        }
    }
}