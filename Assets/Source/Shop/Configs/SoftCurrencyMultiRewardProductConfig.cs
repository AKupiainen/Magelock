using System.Collections.Generic;
using UnityEngine;

namespace MageLock.Shop
{
    [CreateAssetMenu(fileName = "SoftMultiRewardProduct", menuName = "MageLock/Shop/Soft Currency Multi Reward Product")]
    public class SoftCurrencyMultiRewardProductConfig : SoftCurrencyProductConfig
    {
        [Header("Rewards")]
        [SerializeField] private List<RewardConfig> rewards;

        public List<RewardConfig> Rewards => rewards;
    }
}