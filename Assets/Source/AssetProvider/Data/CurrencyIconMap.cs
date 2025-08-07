using UnityEngine;
using System.Collections.Generic;
using BrawlLine.Player;

namespace BrawlLine.Assets
{
    [CreateAssetMenu(menuName = "BrawlLine/Assets/Currency Icon Map")]
    public class CurrencyIconMap : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public CurrencyType currencyType;
            public Sprite icon;
        }

        [SerializeField] private List<Entry> entries;

        public Dictionary<CurrencyType, Sprite> ToDictionary()
        {
            Dictionary<CurrencyType, Sprite> dict = new Dictionary<CurrencyType, Sprite>();
            foreach (var entry in entries)
            {
                if (!dict.ContainsKey(entry.currencyType))
                {
                    dict[entry.currencyType] = entry.icon;
                }
            }
            return dict;
        }
    }
}