using System;
using System.Collections.Generic;
using BrawlLine.Localization;
using UnityEngine;

namespace BrawlLine.Player
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