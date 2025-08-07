using UnityEngine;

namespace BrawlLine.Shop
{
    public abstract class IAPProductConfig : ShopProductBaseConfig
    {
        [Header("IAP Info")]
        [SerializeField] private string productId;

        public string ProductId => productId;
    }
}