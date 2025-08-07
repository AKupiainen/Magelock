using UnityEngine;
using BrawlLine.Player;

namespace BrawlLine.Assets
{
    public interface IAssetManager
    {
        Sprite GetCurrencyIcon(CurrencyType currencyType);
    }
}