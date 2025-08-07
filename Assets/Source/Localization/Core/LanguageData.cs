using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrawlLine.Localization
{
    [Serializable]
    public class LanguageInfo
    {
        [SerializeField] private SystemLanguage language;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite languageImage;

        public SystemLanguage Language => language;
        public string DisplayName => displayName;
        public Sprite LanguageImage => languageImage;
    }

    [CreateAssetMenu(fileName = "LanguageData", menuName = "BrawlLine/Localization/Language Data")]
    public class LanguageData : ScriptableObject
    {
        [SerializeField] private List<LanguageInfo> supportedLanguages = new List<LanguageInfo>();
        [SerializeField] private SystemLanguage defaultLanguage = SystemLanguage.English;

        public List<LanguageInfo> SupportedLanguages => supportedLanguages;
        public SystemLanguage DefaultLanguage => defaultLanguage;

        public SystemLanguage GetLanguageByIndex(int index)
        {
            if (index >= 0 && index < supportedLanguages.Count)
            {
                return supportedLanguages[index].Language;
            }

            return defaultLanguage;
        }

        public int GetIndexByLanguage(SystemLanguage language)
        {
            for (int i = 0; i < supportedLanguages.Count; i++)
            {
                if (supportedLanguages[i].Language == language)
                {
                    return i;
                }
            }

            return 0;
        }

        public string GetDisplayName(int index)
        {
            if (index >= 0 && index < supportedLanguages.Count)
            {
                return supportedLanguages[index].DisplayName;
            }

            return "Unknown";
        }

        public string GetDisplayName(SystemLanguage language)
        {
            foreach (var langInfo in supportedLanguages)
            {
                if (langInfo.Language == language)
                {
                    return langInfo.DisplayName;
                }
            }

            return "Unknown";
        }
        
        public Sprite GetLanguageImage(int index)
        {
            if (index >= 0 && index < supportedLanguages.Count)
            {
                return supportedLanguages[index].LanguageImage;
            }

            return null;
        }

        public Sprite GetLanguageImage(SystemLanguage language)
        {
            foreach (var langInfo in supportedLanguages)
            {
                if (langInfo.Language == language)
                {
                    return langInfo.LanguageImage;
                }
            }

            return null;
        }
    }
}