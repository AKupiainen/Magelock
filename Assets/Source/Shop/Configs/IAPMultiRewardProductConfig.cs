using System.Collections.Generic;
using UnityEngine;

namespace BrawlLine.Shop
{
    [CreateAssetMenu(fileName = "IAPMultiRewardProduct", menuName = "BrawlLine/Shop/IAP Multi Reward Product")]
    public class IAPMultiRewardProductConfig : IAPProductConfig
    {
        [Header("Rewards")]
        [SerializeField] private List<RewardConfig> rewards;

        public List<RewardConfig> Rewards => rewards;
    }
}