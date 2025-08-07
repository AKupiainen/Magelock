using UnityEngine;

namespace BrawlLine.Shop
{
    public abstract class RewardConfig : ScriptableObject
    {
        public abstract void GrantReward();
    }
}