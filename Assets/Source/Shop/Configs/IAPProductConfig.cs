using UnityEngine;

namespace MageLock.Shop
{
    public abstract class IAPProductConfig : ShopProductBaseConfig
    {
        [Header("IAP Info")]
        [SerializeField] private string productId;

        public string ProductId => productId;
    }
}