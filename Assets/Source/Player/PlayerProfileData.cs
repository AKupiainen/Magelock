using System;

namespace MageLock.Player
{
    [Serializable]
    public class PlayerProfileData
    {
        public int level = 1;
        public int currentExperience;
        public string playerName;
        
        public PlayerProfileData()
        {
            level = 1;
            currentExperience = 0;
            playerName = string.Empty;
        }
        
        public PlayerProfileData(PlayerProfileData other)
        {
            level = other.level;
            currentExperience = other.currentExperience;
            playerName = other.playerName;
        }
    }
}