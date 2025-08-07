using System.Collections.Generic;
using UnityEngine;

namespace MageLock.Shop
{
    [CreateAssetMenu(fileName = "IAPMultiRewardProduct", menuName = "MageLock/Shop/IAP Multi Reward Product")]
    public class IAPMultiRewardProductConfig : IAPProductConfig
    {
        [Header("Rewards")]
        [SerializeField] private List<RewardConfig> rewards;

        public List<RewardConfig> Rewards => rewards;
    }
}