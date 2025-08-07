using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrawlLine.Player;
using BrawlLine.Assets;
using BrawlLine.DependencyInjection;

namespace BrawlLine.Shop
{
    public class SingleCurrencyProductItem : ShopProductItem
    {
        [SerializeField] private TextMeshProUGUI currencyAmountText;
        [SerializeField] private Image currencyIcon;
        [SerializeField] private bool useProductImage;

        [Inject] private readonly IAssetManager assetManager;

        private CurrencyType currencyType;
        private int currencyAmount;

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

            currencyType = currencyReward.CurrencyType;
            currencyAmount = currencyReward.Amount;

            if (currencyAmountText != null)
            {
                currencyAmountText.text = currencyAmount.ToString();
            }

            if (currencyIcon != null && assetManager != null)
            {
                if (useProductImage && ProductConfig.ProductIcon != null)
                {
                    currencyIcon.sprite = ProductConfig.ProductIcon;
                }
                else
                {
                    currencyIcon.sprite = assetManager.GetCurrencyIcon(currencyType);
                }
            }
        }
    }
}