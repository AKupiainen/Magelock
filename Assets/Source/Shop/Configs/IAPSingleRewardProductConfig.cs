using UnityEngine;

namespace BrawlLine.Shop
{
    [CreateAssetMenu(fileName = "IAPSingleRewardProduct", menuName = "BrawlLine/Shop/IAP Single Reward Product")]
    public class IAPSingleRewardProductConfig : IAPProductConfig
    {
        [Header("Reward")]
        [SerializeField] private RewardConfig reward;

        public RewardConfig Reward => reward;
    }
}