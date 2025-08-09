using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MageLock.Player;
using MageLock.Assets;
using MageLock.DependencyInjection;

namespace MageLock.Shop
{
    public class SingleCurrencyProductItem : ShopProductItem
    {
        [SerializeField] private TextMeshProUGUI currencyAmountText;
        [SerializeField] private Image currencyIcon;
        [SerializeField] private bool useProductImage;

        [Inject] private readonly IAssetManager _assetManager;

        private CurrencyType _currencyType;
        private int _currencyAmount;

        protected override void SetupProductSpecificUI()
        {
            SetupCurrencyDisplay();
        }

        private void SetupCurrencyDisplay()
        {
            CurrencyRewardConfig currencyReward = null;

            if (ProductConfig is SoftCurrencySingleRewardProductConfig softConfig)
            {
                currencyReward = softConfig.Reward as CurrencyRewardConfig;
            }
            else if (ProductConfig is IAPSingleRewardProductConfig iapConfig)
            {
                currencyReward = iapConfig.Reward as CurrencyRewardConfig;
            }

            if (currencyReward == null)
            {
                Debug.LogError("SingleCurrencyProductItem requires a CurrencyRewardConfig as reward.");
                return;
            }

            _currencyType = currencyReward.CurrencyType;
            _currencyAmount = currencyReward.Amount;

            if (currencyAmountText != null)
            {
                currencyAmountText.text = _currencyAmount.ToString();
            }

            if (currencyIcon != null && _assetManager != null)
            {
                if (useProductImage && ProductConfig.ProductIcon != null)
                {
                    currencyIcon.sprite = ProductConfig.ProductIcon;
                }
                else
                {
                    currencyIcon.sprite = _assetManager.GetCurrencyIcon(_currencyType);
                }
            }
        }
    }
}