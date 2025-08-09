using UnityEngine;
using System.Collections.Generic;
using MageLock.Player;

namespace MageLock.Assets
{
    public class AssetManager : IAssetManager
    {
        private readonly Dictionary<CurrencyType, Sprite> _currencyIcons;

        public AssetManager(CurrencyIconMap currencyIconMap)
        {
            _currencyIcons = currencyIconMap.ToDictionary();
        }

        public Sprite GetCurrencyIcon(CurrencyType currencyType)
        {
            if (_currencyIcons.TryGetValue(currencyType, out var sprite))
            {
                return sprite;
            }

            Debug.LogWarning($"Missing currency icon for {currencyType}");
            return null;
        }
    }
}
