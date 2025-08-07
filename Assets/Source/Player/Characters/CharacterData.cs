using System;
using System.Collections.Generic;
using MageLock.Localization;
using UnityEngine;

namespace MageLock.Player
{
    [Serializable]
    public class CharacterData
    {
        public string id;
        public LocString name;
        public CurrencyType purchaseCurrency;
        public int purchasePrice;
        public GameObject menuPrefab;
        public GameObject inGamePrefab;

    }

    [Serializable]
    public class CharacterCollectionData
    {
        public List<string> unlockedCharacterIds = new();
        public string selectedCharacterId = string.Empty;
        public Dictionary<string, DateTime> UnlockDates = new();
    }
}