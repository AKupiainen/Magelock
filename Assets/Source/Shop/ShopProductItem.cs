using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrawlLine.DependencyInjection;
using BrawlLine.Player;
using BrawlLine.Events;

namespace BrawlLine.Shop
{
    public abstract class ShopProductItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] protected TextMeshProUGUI productNameText;
        [SerializeField] protected TextMeshProUGUI priceText;
        [SerializeField] protected Button purchaseButton;
        
        [Header("Price Colors")]
        [SerializeField] protected Color normalPriceColor = Color.white;
        [SerializeField] protected Color notEnoughCurrencyColor = Color.red;

        [Inject] protected ShopManager ShopManager;

        protected ShopProductBaseConfig ProductConfig;

        public virtual void Initialize(ShopProductBaseConfig config)
        {
            ProductConfig = config;

            SetupUI();
            SetupPurchaseButton();
            UpdatePurchaseButtonState();
            
            SubscribeToEvents();
        }

        protected virtual void SetupUI()
        {
            if (ProductConfig == null)
            {
                Debug.LogError("ShopProductItem: Config is null");
                return;
            }

            if (productNameText != null)
            {
                productNameText.text = ProductConfig.ProductName;
            }

            SetupPriceDisplay();
            SetupProductSpecificUI();
        }

        protected abstract void SetupProductSpecificUI();

        protected virtual void SetupPriceDisplay()
        {
            if (priceText == null) return;

            if (ProductConfig is IAPProductConfig iapProduct)
            {
                if (ShopManager != null && ShopManager.IsInitialized())
                {
                    priceText.text = ShopManager.GetProductPrice(iapProduct.ProductId);
                }
            }
            else if (ProductConfig is SoftCurrencySingleRewardProductConfig softSingle)
            {
                priceText.text = softSingle.Cost.ToString();
            }
            else if (ProductConfig is SoftCurrencyMultiRewardProductConfig softMulti)
            {
                priceText.text = softMulti.Cost.ToString();
            }
            else
            {
                priceText.text = string.Empty;
            }
        }

        protected virtual void SetupPurchaseButton()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
                purchaseButton.onClick.AddListener(OnPurchaseClicked);
            }
        }

        protected virtual void UpdatePurchaseButtonState()
        {
            if (purchaseButton == null || ProductConfig == null) return;

            bool canPurchase = CanAffordProduct();
            purchaseButton.interactable = canPurchase;
            
            UpdatePriceTextColor();
        }

        protected virtual void UpdatePriceTextColor()
        {
            if (priceText == null) return;

            bool canAfford = CanAffordProduct();
            
            if (ProductConfig is SoftCurrencySingleRewardProductConfig || 
                ProductConfig is SoftCurrencyMultiRewardProductConfig)
            {
                priceText.color = canAfford ? normalPriceColor : notEnoughCurrencyColor;
            }
            else
            {
                priceText.color = normalPriceColor;
            }
        }

        protected virtual bool CanAffordProduct()
        {
            if (ProductConfig == null) return false;

            if (ProductConfig is IAPProductConfig)
            {
                return true;
            }

            if (ProductConfig is SoftCurrencySingleRewardProductConfig softSingle)
            {
                return PlayerModel.CanAfford(softSingle.CurrencyType, softSingle.Cost);
            }

            if (ProductConfig is SoftCurrencyMultiRewardProductConfig softMulti)
            {
                return PlayerModel.CanAfford(softMulti.CurrencyType, softMulti.Cost);
            }

            return true;
        }

        protected virtual void SubscribeToEvents()
        {
            EventsBus.Subscribe<PlayerCurrencyChangedEvent>(OnCurrencyChanged);
        }

        protected virtual void UnsubscribeFromEvents()
        {
            EventsBus.Unsubscribe<PlayerCurrencyChangedEvent>(OnCurrencyChanged);
        }

        protected virtual void OnCurrencyChanged(PlayerCurrencyChangedEvent currencyEvent)
        {
            UpdatePurchaseButtonState();
        }

        protected virtual void OnPurchaseClicked()
        {
            if (ProductConfig == null)
            {
                Debug.LogWarning("Attempted to purchase without product config.");
                return;
            }

            if (!CanAffordProduct())
            {
                Debug.LogWarning($"Cannot afford product: {ProductConfig.ProductName}");
                return;
            }

            if (ShopManager != null)
            {
                ShopManager.BuyProduct(ProductConfig);
            }
            else
            {
                Debug.LogError("ShopManager instance not found");
            }
        }

        protected virtual void OnDestroy()
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(OnPurchaseClicked);
            }
            
            UnsubscribeFromEvents();
        }
    }
}