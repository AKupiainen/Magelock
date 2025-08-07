using UnityEngine;

namespace BrawlLine.Shop
{
    [CreateAssetMenu(fileName = "SoftSingleRewardProduct", menuName = "BrawlLine/Shop/Soft Currency Single Reward Product")]
    public class SoftCurrencySingleRewardProductConfig : SoftCurrencyProductConfig
    {
        [Header("Reward")]
        [SerializeField] private RewardConfig reward;

        public RewardConfig Reward => reward;
    }
}