using UnityEngine;

namespace MageLock.Shop
{
    public abstract class RewardConfig : ScriptableObject
    {
        public abstract void GrantReward();
    }
}