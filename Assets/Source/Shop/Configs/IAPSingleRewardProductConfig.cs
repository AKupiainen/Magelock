using UnityEngine;

namespace MageLock.Shop
{
    [CreateAssetMenu(fileName = "IAPSingleRewardProduct", menuName = "MageLock/Shop/IAP Single Reward Product")]
    public class IAPSingleRewardProductConfig : IAPProductConfig
    {
        [Header("Reward")]
        [SerializeField] private RewardConfig reward;

        public RewardConfig Reward => reward;
    }
}