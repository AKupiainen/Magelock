using UnityEngine;

namespace MageLock.Shop
{
    [CreateAssetMenu(fileName = "SoftSingleRewardProduct", menuName = "MageLock/Shop/Soft Currency Single Reward Product")]
    public class SoftCurrencySingleRewardProductConfig : SoftCurrencyProductConfig
    {
        [Header("Reward")]
        [SerializeField] private RewardConfig reward;

        public RewardConfig Reward => reward;
    }
}