using UnityEngine;
using System.Collections.Generic;

namespace MageLock.Player
{
    [CreateAssetMenu(fileName = "DefaultPlayerConfig", menuName = "MageLock/Default Player Config")]
    public class DefaultPlayerConfig : ScriptableObject
    {
        [Header("Starting Currencies")]
        [SerializeField] private int startingCoins = 100;
        [SerializeField] private int startingGems = 10;
        
        [Header("Starting Characters")]
        [SerializeField] private string defaultCharacterId = "starter_character";
        [SerializeField] private List<string> additionalUnlockedCharacterIds = new List<string>();
        
        [Header("Starting Player Info")]
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private int startingExperience = 0;
        
        public int StartingCoins => startingCoins;
        public int StartingGems => startingGems;
        public string DefaultCharacterId => defaultCharacterId;
        public List<string> AdditionalUnlockedCharacterIds => additionalUnlockedCharacterIds;
        public int StartingLevel => startingLevel;
        public int StartingExperience => startingExperience;
        
        public List<string> GetAllStartingCharacterIds()
        {
            var allIds = new List<string>();
            
            if (!string.IsNullOrEmpty(defaultCharacterId))
            {
                allIds.Add(defaultCharacterId);
            }
            
            foreach (var id in additionalUnlockedCharacterIds)
            {
                if (!string.IsNullOrEmpty(id) && !allIds.Contains(id))
                {
                    allIds.Add(id);
                }
            }
            
            return allIds;
        }
        
        public void ApplyToNewPlayer(PlayerData playerData)
        {
            playerData.currencies.SetCurrency(CurrencyType.Coins, startingCoins);
            playerData.currencies.SetCurrency(CurrencyType.Gems, startingGems);
            
            playerData.profile.currentExperience = startingExperience;
            
            if (playerData.characterCollection == null)
            {
                playerData.characterCollection = new CharacterCollectionData();
            }
            
            playerData.characterCollection.unlockedCharacterIds.Clear();
            playerData.characterCollection.UnlockDates.Clear();
            
            var startingCharacters = GetAllStartingCharacterIds();

            foreach (var characterId in startingCharacters)
            {
                playerData.characterCollection.unlockedCharacterIds.Add(characterId);
                playerData.characterCollection.UnlockDates[characterId] = System.DateTime.Now;
            }
            
            if (!string.IsNullOrEmpty(defaultCharacterId))
            {
                playerData.characterCollection.selectedCharacterId = defaultCharacterId;
            }
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            startingCoins = Mathf.Max(0, startingCoins);
            startingGems = Mathf.Max(0, startingGems);
            startingLevel = Mathf.Max(1, startingLevel);
            startingExperience = Mathf.Max(0, startingExperience);
            
            for (int i = additionalUnlockedCharacterIds.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrEmpty(additionalUnlockedCharacterIds[i]) || 
                    additionalUnlockedCharacterIds[i] == defaultCharacterId)
                {
                    additionalUnlockedCharacterIds.RemoveAt(i);
                }
            }
        }
#endif
    }
}