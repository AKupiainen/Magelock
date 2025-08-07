using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using BrawlLine.Events;
using BrawlLine.DependencyInjection;
using UnityEngine.Serialization;

namespace BrawlLine.Player
{
    public class PlayerLevelChangedEvent : IEventData
    {
        public int NewLevel { get; }
        public int OldLevel { get; }
        
        public PlayerLevelChangedEvent(int oldLevel, int newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }
    
    public class PlayerNameChangedEvent : IEventData
    {
        public string NewName { get; }
        public string OldName { get; }
        
        public PlayerNameChangedEvent(string oldName, string newName)
        {
            OldName = oldName;
            NewName = newName;
        }
    }
    
    public class PlayerExperienceChangedEvent : IEventData
    {
        public int NewExperience { get; }
        public int OldExperience { get; }
        public int ExperienceGained { get; }
        
        public PlayerExperienceChangedEvent(int oldExperience, int newExperience)
        {
            OldExperience = oldExperience;
            NewExperience = newExperience;
            ExperienceGained = newExperience - oldExperience;
        }
    }
    
    public class PlayerLevelUpEvent : IEventData
    {
        public int OldLevel { get; }
        public int NewLevel { get; }
        public int LevelsGained { get; }
        
        public PlayerLevelUpEvent(int oldLevel, int newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
            LevelsGained = newLevel - oldLevel;
        }
    }
    
    public class PlayerCurrencyChangedEvent : IEventData
    {
        public CurrencyType CurrencyType { get; }
        public int NewAmount { get; }
        public int OldAmount { get; }
        public int AmountChanged { get; }
        
        public PlayerCurrencyChangedEvent(CurrencyType type, int oldAmount, int newAmount)
        {
            CurrencyType = type;
            OldAmount = oldAmount;
            NewAmount = newAmount;
            AmountChanged = newAmount - oldAmount;
        }
    }
    
    public class PlayerCurrencyAddedEvent : IEventData
    {
        public CurrencyType CurrencyType { get; }
        public int AmountAdded { get; }
        public int NewTotal { get; }
        
        public PlayerCurrencyAddedEvent(CurrencyType type, int amountAdded, int newTotal)
        {
            CurrencyType = type;
            AmountAdded = amountAdded;
            NewTotal = newTotal;
        }
    }
    
    public class PlayerCurrencySpentEvent : IEventData
    {
        public CurrencyType CurrencyType { get; }
        public int AmountSpent { get; }
        public int NewTotal { get; }
        
        public PlayerCurrencySpentEvent(CurrencyType type, int amountSpent, int newTotal)
        {
            CurrencyType = type;
            AmountSpent = amountSpent;
            NewTotal = newTotal;
        }
    }

    public class CharacterUnlockedEvent : IEventData
    {
        public string CharacterId { get; }
        public CharacterData Character { get; }
        
        public CharacterUnlockedEvent(string characterId, CharacterData character)
        {
            CharacterId = characterId;
            Character = character;
        }
    }
    
    public class CharacterSelectedEvent : IEventData
    {
        public string PreviousCharacterId { get; }
        public string NewCharacterId { get; }
        
        public CharacterSelectedEvent(string previousCharacterId, string newCharacterId)
        {
            PreviousCharacterId = previousCharacterId;
            NewCharacterId = newCharacterId;
        }
    }

    [Serializable]
    public class PlayerData
    {
        public SettingsData settings = new();
        public PlayerProfileData profile = new();
        public CurrencyData currencies = new();
        public CharacterCollectionData characterCollection = new();
    }

    public static class PlayerModel
    {
        private const string PlayerDataFileName = "player_data.json";

        [Inject] private static PlayerData _playerData;
        [Inject] private static PlayerNamesData _playerNamesData;
        [Inject] private static LevelData _levelData;
        [Inject] private static DefaultPlayerConfig _defaultPlayerConfig;
        [Inject] private static CharacterDatabase _characterDatabase;

        public static void Initialize()
        {
            DIContainer.Instance.Inject(typeof(PlayerModel));

            LoadPlayerData();
            Application.quitting += OnApplicationQuit;
        }

        private static void LoadPlayerData()
        {
            string filePath = Path.Combine(Application.persistentDataPath, PlayerDataFileName);
            
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    _playerData = JsonConvert.DeserializeObject<PlayerData>(json);
                    Debug.Log("Player data loaded successfully.");
                    
                    if (string.IsNullOrEmpty(_playerData.profile.playerName))
                    {
                        GenerateRandomPlayerName();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load player data: {ex.Message}");
                    CreateDefaultPlayerData();
                }
            }
            else
            {
                Debug.Log("No player data found. Creating default data.");
                CreateDefaultPlayerData();
            }
        }

        private static void SavePlayerData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_playerData, Formatting.Indented);
                string filePath = Path.Combine(Application.persistentDataPath, PlayerDataFileName);
                File.WriteAllText(filePath, json);
                Debug.Log("Player data saved successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save player data: {ex.Message}");
            }
        }

        private static void CreateDefaultPlayerData()
        {
            _playerData = new PlayerData();
            
            if (_defaultPlayerConfig != null)
            {
                _defaultPlayerConfig.ApplyToNewPlayer(_playerData);
                Debug.Log($"Applied default player configuration: {_defaultPlayerConfig.StartingCoins} coins, {_defaultPlayerConfig.StartingGems} gems, default character: {_defaultPlayerConfig.DefaultCharacterId}");
            }
            
            GenerateRandomPlayerName();
        }

        public static PlayerData GetPlayerData()
        {
            return _playerData;
        }

        public static void SetPlayerData(PlayerData data)
        {
            _playerData = data;
            SavePlayerData();
        }

        public static SettingsData GetSettings()
        {
            return _playerData.settings;
        }

        public static void SetSettings(SettingsData settings)
        {
            _playerData.settings = settings;
            SavePlayerData(); 
        }

        public static int GetPlayerLevel()
        {
            return _playerData.profile.level;
        }

        public static void SetPlayerLevel(int level)
        {
            int oldLevel = _playerData.profile.level;
            int newLevel = Mathf.Max(1, level);
            _playerData.profile.level = newLevel;
            
            if (oldLevel != newLevel)
            {
                EventsBus.Trigger(new PlayerLevelChangedEvent(oldLevel, newLevel));
                SavePlayerData(); 
            }
        }

        public static int GetCurrentExperience()
        {
            return _playerData.profile.currentExperience;
        }

        public static void SetCurrentExperience(int experience)
        {
            int oldExp = _playerData.profile.currentExperience;
            int newExp = Mathf.Max(0, experience);
            _playerData.profile.currentExperience = newExp;
            
            if (oldExp != newExp)
            {
                EventsBus.Trigger(new PlayerExperienceChangedEvent(oldExp, newExp));
                SavePlayerData(); 
            }
        }

        public static void AddExperience(int amount)
        {
            int oldExp = _playerData.profile.currentExperience;
            _playerData.profile.currentExperience += amount;
            
            if (oldExp != _playerData.profile.currentExperience)
            {
                EventsBus.Trigger(new PlayerExperienceChangedEvent(oldExp, _playerData.profile.currentExperience));
            }
            
            CheckLevelUp();
            SavePlayerData(); 
        }

        private static void CheckLevelUp()
        {
            if (_levelData == null) return;

            int currentLevel = _playerData.profile.level;
            int currentExp = _playerData.profile.currentExperience;
            int expForNextLevel = _levelData.GetExperienceForLevel(currentLevel + 1);
            
            int levelsGained = 0;
            int originalLevel = currentLevel;

            while (currentExp >= expForNextLevel && currentLevel < _levelData.GetMaxLevel())
            {
                currentLevel++;
                levelsGained++;
                currentExp -= expForNextLevel;
                expForNextLevel = _levelData.GetExperienceForLevel(currentLevel + 1);
            }

            if (levelsGained > 0)
            {
                _playerData.profile.level = currentLevel;
                _playerData.profile.currentExperience = currentExp;
                
                EventsBus.Trigger(new PlayerLevelUpEvent(originalLevel, currentLevel));
                EventsBus.Trigger(new PlayerLevelChangedEvent(originalLevel, currentLevel));
                EventsBus.Trigger(new PlayerExperienceChangedEvent(_playerData.profile.currentExperience, currentExp));
            }
        }

        public static string GetPlayerName()
        {
            return _playerData.profile.playerName;
        }

        public static void SetPlayerName(string name)
        {
            string oldName = _playerData.profile.playerName;
            _playerData.profile.playerName = name;
            
            if (oldName != name)
            {
                EventsBus.Trigger(new PlayerNameChangedEvent(oldName, name));
                SavePlayerData();
            }
        }

        public static void GenerateRandomPlayerName()
        {
            if (_playerNamesData != null)
            {
                string oldName = _playerData.profile.playerName;
                string newName = _playerNamesData.GetRandomName();
                _playerData.profile.playerName = newName;
                
                if (oldName != newName)
                {
                    EventsBus.Trigger(new PlayerNameChangedEvent(oldName, newName));
                    SavePlayerData(); 
                }
            }
        }

        public static int GetExperienceForCurrentLevel()
        {
            if (_levelData != null)
            {
                return _levelData.GetExperienceForLevel(_playerData.profile.level);
            }

            return _playerData.profile.level * 100;
        }

        public static int GetExperienceForNextLevel()
        {
            if (_levelData != null)
            {
                return _levelData.GetExperienceForLevel(_playerData.profile.level + 1);
            }

            return (_playerData.profile.level + 1) * 100;
        }

        public static float GetLevelProgressPercentage()
        {
            int currentExp = _playerData.profile.currentExperience;
            int expForNextLevel = GetExperienceForNextLevel();

            if (expForNextLevel <= 0) return 1f;

            return Mathf.Clamp01((float)currentExp / expForNextLevel);
        }

        public static int GetCurrency(CurrencyType type)
        {
            return _playerData.currencies.GetCurrency(type);
        }

        public static void SetCurrency(CurrencyType type, int amount)
        {
            int oldAmount = _playerData.currencies.GetCurrency(type);
            _playerData.currencies.SetCurrency(type, amount);
            
            if (oldAmount != amount)
            {
                EventsBus.Trigger(new PlayerCurrencyChangedEvent(type, oldAmount, amount));
                SavePlayerData(); 
            }
        }

        public static void AddCurrency(CurrencyType type, int amount)
        {
            int oldAmount = _playerData.currencies.GetCurrency(type);
            _playerData.currencies.AddCurrency(type, amount);
            int newAmount = _playerData.currencies.GetCurrency(type);
            
            if (oldAmount != newAmount)
            {
                EventsBus.Trigger(new PlayerCurrencyAddedEvent(type, amount, newAmount));
                EventsBus.Trigger(new PlayerCurrencyChangedEvent(type, oldAmount, newAmount));
                SavePlayerData(); 
            }
        }

        public static bool CanAfford(CurrencyType type, int amount)
        {
            return _playerData.currencies.CanAfford(type, amount);
        }

        public static bool SpendCurrency(CurrencyType type, int amount)
        {
            int oldAmount = _playerData.currencies.GetCurrency(type);
            bool success = _playerData.currencies.SpendCurrency(type, amount);
            
            if (success)
            {
                int newAmount = _playerData.currencies.GetCurrency(type);
                EventsBus.Trigger(new PlayerCurrencySpentEvent(type, amount, newAmount));
                EventsBus.Trigger(new PlayerCurrencyChangedEvent(type, oldAmount, newAmount));
                SavePlayerData(); 
            }
            
            return success;
        }

        public static CharacterCollectionData GetCharacterCollection()
        {
            return _playerData.characterCollection;
        }
        
        public static void SetCharacterCollection(CharacterCollectionData characterCollection)
        {
            _playerData.characterCollection = characterCollection;
            SavePlayerData();
        }
        
        public static List<CharacterData> GetUnlockedCharacters()
        {
            if (_characterDatabase == null) return new List<CharacterData>();
            
            return _characterDatabase.Characters
                .Where(c => _playerData.characterCollection.unlockedCharacterIds.Contains(c.id))
                .ToList();
        }
        
        public static CharacterData GetSelectedCharacter()
        {
            if (_characterDatabase == null) return null;
            return _characterDatabase.GetCharacter(_playerData.characterCollection.selectedCharacterId);
        }
        
        public static bool IsCharacterUnlocked(string characterId)
        {
            return _playerData.characterCollection.unlockedCharacterIds.Contains(characterId);
        }
        
        public static bool CanPurchaseCharacter(string characterId)
        {
            var character = _characterDatabase?.GetCharacter(characterId);
            if (character == null || IsCharacterUnlocked(characterId))
                return false;
            
            return CanAfford(character.purchaseCurrency, character.purchasePrice);
        }
        
        public static bool PurchaseCharacter(string characterId)
        {
            var character = _characterDatabase?.GetCharacter(characterId);
            if (character == null || IsCharacterUnlocked(characterId))
                return false;
            
            if (!SpendCurrency(character.purchaseCurrency, character.purchasePrice))
                return false;
            
            UnlockCharacter(characterId);
            return true;
        }
        
        public static void UnlockCharacter(string characterId)
        {
            var character = _characterDatabase?.GetCharacter(characterId);
            if (character == null || IsCharacterUnlocked(characterId))
                return;
            
            _playerData.characterCollection.unlockedCharacterIds.Add(characterId);
            _playerData.characterCollection.UnlockDates[characterId] = DateTime.Now;
            
            SavePlayerData();
            
            EventsBus.Trigger(new CharacterUnlockedEvent(characterId, character));
        }
        
        public static bool SelectCharacter(string characterId)
        {
            var character = _characterDatabase?.GetCharacter(characterId);
            if (character == null || !IsCharacterUnlocked(characterId))
                return false;
            
            string previousCharacterId = _playerData.characterCollection.selectedCharacterId;
            _playerData.characterCollection.selectedCharacterId = characterId;
            
            SavePlayerData();
            
            EventsBus.Trigger(new CharacterSelectedEvent(previousCharacterId, characterId));
            return true;
        }
        
        public static int GetUnlockedCharacterCount()
        {
            return _playerData.characterCollection.unlockedCharacterIds.Count;
        }
        
        public static int GetTotalCharacterCount()
        {
            return _characterDatabase?.Characters.Count ?? 0;
        }
        
        public static float GetCollectionProgress()
        {
            int total = GetTotalCharacterCount();
            if (total == 0) return 0f;
            return (float)GetUnlockedCharacterCount() / total;
        }

        private static void OnApplicationQuit()
        {
            SavePlayerData();
        }
    }
}