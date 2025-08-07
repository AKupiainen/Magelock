using UnityEngine;
using BrawlLine.Player; 

namespace BrawlLine.Shop
{
    public abstract class SoftCurrencyProductConfig : ShopProductBaseConfig
    {
        [Header("Soft Currency Cost")]
        [SerializeField] private CurrencyType currencyType;
        [SerializeField] private int cost;
        
        public CurrencyType CurrencyType => currencyType;
        public int Cost => cost;
    }
}