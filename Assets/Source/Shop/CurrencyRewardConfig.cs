using BrawlLine.Player;
using UnityEngine;

namespace BrawlLine.Shop
{
    [CreateAssetMenu(fileName = "CurrencyRewardConfig", menuName = "BrawlLine/Shop/Rewards/Currency Reward Config")]
    public class CurrencyRewardConfig : RewardConfig
    {
        [SerializeField] private CurrencyType currencyType;
        [SerializeField] private int amount;

        public CurrencyType CurrencyType => currencyType;
        public int Amount => amount;

        public override void GrantReward()
        {
            PlayerModel.AddCurrency(currencyType, amount); 
        }
    }
}