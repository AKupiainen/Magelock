using System.Collections.Generic;
using UnityEngine;

namespace BrawlLine.Localization
{
    public interface ILocalizationService
    {
        /// <summary>
        /// Initializes the localization service with the specified language.
        /// </summary>
        /// <param name="localizationLanguage">The language to initialize with.</param>
        void Initialize(SystemLanguage localizationLanguage);

        /// <summary>
        /// Gets the localized value for the specified key.
        /// </summary>
        /// <param name="key">The key for the localized string.</param>
        /// <returns>The localized string.</returns>
        string GetLocalizedValue(LocString key);

        /// <summary>
        /// Gets the localized value for the specified key and formats it with the provided arguments.
        /// </summary>
        /// <param name="key">The key for the localized string.</param>
        /// <param name="args">The arguments to format the string with.</param>
        /// <returns>The formatted localized string.</returns>
        string FormatLocalizedValue(string key, params object[] args);

        /// <summary>
        /// Sets the current language for the localization service.
        /// </summary>
        /// <param name="language">The language to set.</param>
        void SetLanguage(SystemLanguage language);

        /// <summary>
        /// Gets the current language set in the localization service.
        /// </summary>
        SystemLanguage GetCurrentLanguage { get; }

        /// <summary>
        /// Gets the list of localization data used by the service.
        /// </summary>
        List<LocalizationData> LocalizationDatas { get; }
    }
}