using System;
using UnityEngine;
using MageLock.DependencyInjection;

namespace MageLock.Localization
{
    [Serializable]
    public class LocString
    {
        [SerializeField] private string key;

        public static implicit operator string(LocString locString)
        {
            if (locString == null)
            {
                return null;
            }

            var localizationService = DIContainer.Instance.GetService<LocalizationService>();
            return localizationService.GetLocalizedValue(locString.Key);
        }

        public override string ToString()
        {
            var localizationService = DIContainer.Instance.GetService<LocalizationService>();
            return localizationService.GetLocalizedValue(Key);
        }

        public string Key => key;
    }
}