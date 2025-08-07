using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrawlLine.Localization
{
    [Serializable]
    public class LocalizedString
    {
        [SerializeField] private string key;
        [SerializeField] private string value;

        public string Key
        {
            get => key;
            set => key = value;
        }

        public string Value
        {
            get => value;
            set => this.value = value;
        }
    }

    [CreateAssetMenu(fileName = "LocalizationData", menuName = "Localization/Localization Data")]
    public class LocalizationData : ScriptableObject
    {
        [SerializeField] private SystemLanguage localizationLanguage;
        [SerializeField] private List<LocalizedString> localizedStrings;

        public List<LocalizedString> LocalizedStrings => localizedStrings;
        public SystemLanguage LocalizationLanguage => localizationLanguage;
    }
}