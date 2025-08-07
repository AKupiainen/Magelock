using UnityEngine;
using System.Collections.Generic;
using BrawlLine.Player;

namespace BrawlLine.Assets
{
    public class AssetManager : IAssetManager
    {
        private readonly Dictionary<CurrencyType, Sprite> currencyIcons;

        public AssetManager(CurrencyIconMap currencyIconMap)
        {
            currencyIcons = currencyIconMap.ToDictionary();
        }

        public Sprite GetCurrencyIcon(CurrencyType currencyType)
        {
            if (currencyIcons.TryGetValue(currencyType, out var sprite))
            {
                return sprite;
            }

            Debug.LogWarning($"Missing currency icon for {currencyType}");
            return null;
        }
    }
}
