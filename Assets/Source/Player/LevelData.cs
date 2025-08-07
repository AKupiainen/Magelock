using UnityEngine;
using System.Collections.Generic;
using System;
using BrawlLine.JsonScriptableObject;

namespace BrawlLine.Player
{
    [Serializable]
    public class LevelInfo
    {
        public int level;
        public int experienceRequired;

        public LevelInfo() { }
        
        public LevelInfo(int level, int experienceRequired)
        {
            this.level = level;
            this.experienceRequired = experienceRequired;
        }
    }
    
    [CreateAssetMenu(fileName = "LevelData", menuName = "BrawlLine/Level Data")]
    public class LevelData : JsonScriptableObjectBase
    {
        [SerializeField] private List<LevelInfo> levels = new();
        
        public LevelInfo GetLevelInfo(int level)
        {
            foreach (var levelInfo in levels)
            {
                if (levelInfo.level == level)
                {
                    return levelInfo;
                }
            }
            
            return new LevelInfo(level, level * 100);
        }
        
        public int GetExperienceForLevel(int level)
        {
            var levelInfo = GetLevelInfo(level);
            return levelInfo.experienceRequired;
        }
        
        public int GetMaxLevel()
        {
            if (levels.Count == 0) return 1;
            
            int maxLevel = 1;
            foreach (var level in levels)
            {
                if (level.level > maxLevel)
                {
                    maxLevel = level.level;
                }
            }
            return maxLevel;
        }
        
        public List<LevelInfo> GetAllLevels()
        {
            return new List<LevelInfo>(levels);
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            levels.Sort((a, b) => a.level.CompareTo(b.level));
        }
#endif
    }
}