using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MageLock.Player
{
    [CreateAssetMenu(fileName = "CharacterDatabase", menuName = "MageLock/Character Database")]
    public class CharacterDatabase : ScriptableObject
    {
        [SerializeField] private List<CharacterData> characters = new();
        
        public List<CharacterData> Characters => characters;
        
        public CharacterData GetCharacter(string id)
        {
            return characters.FirstOrDefault(c => c.id == id);
        }
    }
}