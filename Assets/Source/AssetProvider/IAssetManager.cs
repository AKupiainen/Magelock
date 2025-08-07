using UnityEngine;
using MageLock.Player;

namespace MageLock.Assets
{
    public interface IAssetManager
    {
        Sprite GetCurrencyIcon(CurrencyType currencyType);
    }
}