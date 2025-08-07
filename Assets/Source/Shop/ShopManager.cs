using MageLock.Player;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace MageLock.Shop
{
    public class ShopManager : MonoBehaviour, IDetailedStoreListener
    {
        [Header("Shop Configuration")]
        [SerializeField] private List<ShopProductBaseConfig> allProducts;

        private IStoreController storeController;
        private IExtensionProvider extensionProvider;

        private Dictionary<string, IAPProductConfig> iapProductMap;

        public System.Action<ShopProductBaseConfig> OnPurchaseCompleted;
        public System.Action<ShopProductBaseConfig> OnPurchaseFailedEvent;

        public void InitializeIAP()
        {
            if (IsInitialized())
            {
                return;
            }

            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            iapProductMap = new Dictionary<string, IAPProductConfig>();

            foreach (var product in allProducts)
            {
                if (product is IAPProductConfig iapProduct)
                {
                    builder.AddProduct(iapProduct.ProductId, ProductType.Consumable);
                    iapProductMap[iapProduct.ProductId] = iapProduct;
                }
            }

            UnityPurchasing.Initialize(this, builder);
        }

        public bool IsInitialized()
        {
            return storeController != null && extensionProvider != null;
        }

        public void BuyProduct(ShopProductBaseConfig product)
        {
            switch (product)
            {
                case IAPProductConfig iap:
                    BuyIAPProduct(iap.ProductId);
                    break;

                case SoftCurrencySingleRewardProductConfig softSingle:
                    if (PlayerModel.SpendCurrency(softSingle.CurrencyType, softSingle.Cost))
                    {
                        Debug.Log($"Purchase successful for {softSingle.ProductName}.");
                        softSingle.Reward.GrantReward();
                        OnPurchaseCompleted?.Invoke(product);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not afford {softSingle.ProductName}. Purchase failed.");
                        OnPurchaseFailedEvent?.Invoke(product);
                    }
                    break;

                case SoftCurrencyMultiRewardProductConfig softMulti:
                    if (PlayerModel.SpendCurrency(softMulti.CurrencyType, softMulti.Cost))
                    {
                        Debug.Log($"Purchase successful for {softMulti.ProductName}.");
                        foreach (var reward in softMulti.Rewards)
                        {
                            reward.GrantReward();
                        }
                        OnPurchaseCompleted?.Invoke(product);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not afford {softMulti.ProductName}. Purchase failed.");
                        OnPurchaseFailedEvent?.Invoke(product);
                    }
                    break;

                default:
                    Debug.LogWarning("Unsupported product type passed to BuyProduct.");
                    break;
            }
        }

        private void BuyIAPProduct(string productId)
        {
            if (!IsInitialized())
            {
                Debug.LogError("IAP is not initialized. Cannot buy product.");
                return;
            }

            storeController.InitiatePurchase(productId);
        }

        public List<ShopProductBaseConfig> GetAllProducts()
        {
            return allProducts ?? new List<ShopProductBaseConfig>();
        }

        public ShopProductBaseConfig GetProductById(string productId)
        {
            return iapProductMap != null && iapProductMap.TryGetValue(productId, out var product)
                ? product
                : null;
        }

        public string GetProductPrice(string productId)
        {
            if (!IsInitialized()) return string.Empty;

            var product = storeController.products.WithID(productId);

            return product != null && product.availableToPurchase
                ? product.metadata.localizedPriceString
                : string.Empty;
        }

        public bool IsProductAvailable(string productId)
        {
            if (!IsInitialized()) return false;

            var product = storeController.products.WithID(productId);
            return product != null && product.availableToPurchase;
        }

        #region IDetailedStoreListener Implementation

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            Debug.Log("Unity IAP Initialized Successfully.");
            storeController = controller;
            extensionProvider = extensions;
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogError($"Unity IAP initialization failed: {error} - {message}");
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            Debug.LogError($"Unity IAP initialization failed: {error}");
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            string productId = args.purchasedProduct.definition.id;

            if (iapProductMap.TryGetValue(productId, out var product))
            {
                Debug.Log($"Processing IAP purchase: {product.ProductName}");

                switch (product)
                {
                    case IAPSingleRewardProductConfig single:
                        single.Reward.GrantReward();
                        break;

                    case IAPMultiRewardProductConfig multi:
                        foreach (var reward in multi.Rewards)
                        {
                            reward.GrantReward();
                        }
                        break;
                }

                OnPurchaseCompleted?.Invoke(product);
                return PurchaseProcessingResult.Complete;
            }

            Debug.LogError($"Unknown IAP product ID: {productId}");
            return PurchaseProcessingResult.Pending;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            Debug.LogError($"Purchase of product {product.definition.id} failed: {failureReason}");

            if (iapProductMap.TryGetValue(product.definition.id, out var productConfig))
            {
                OnPurchaseFailedEvent?.Invoke(productConfig);
            }
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            Debug.LogError($"Purchase of product {product.definition.id} failed: {failureDescription.reason} - {failureDescription.message}");

            if (iapProductMap.TryGetValue(product.definition.id, out var productConfig))
            {
                OnPurchaseFailedEvent?.Invoke(productConfig);
            }
        }

        #endregion
    }
}