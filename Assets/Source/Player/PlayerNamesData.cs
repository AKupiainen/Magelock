using UnityEngine;
using System.Collections.Generic;
using BrawlLine.JsonScriptableObject;

namespace BrawlLine.Player
{
    [CreateAssetMenu(fileName = "PlayerNamesData", menuName = "BrawlLine/Player Names Data")]
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