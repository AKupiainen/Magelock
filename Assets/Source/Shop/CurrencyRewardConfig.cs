using MageLock.Player;
using UnityEngine;

namespace MageLock.Shop
{
    [CreateAssetMenu(fileName = "CurrencyRewardConfig", menuName = "MageLock/Shop/Rewards/Currency Reward Config")]
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