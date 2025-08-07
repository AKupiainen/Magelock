using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BrawlLine.Localization.Editor
{
    public static class LocalizationValidator
    {
        [MenuItem("Tools/Localization/Validate Localizations")]
        private static void ValidateLocalizations()
        {
            string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
            List<LocalizationData> localizationDatas = guids
                .Select(guid => AssetDatabase.LoadAssetAtPath<LocalizationData>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(data => data != null)
                .ToList();

            if (localizationDatas.Count == 0)
            {
                Debug.LogWarning("No LocalizationData assets found!");
                return;
            }

            HashSet<string> referenceKeys = new(localizationDatas[0].LocalizedStrings.Select(s => s.Key));
            bool isValid = true;
            
            foreach (LocalizationData data in localizationDatas)
            {
                HashSet<string> currentKeys = new(data.LocalizedStrings.Select(s => s.Key));
                if (!referenceKeys.SetEquals(currentKeys))
                {
                    Debug.LogError($"Mismatch in keys for language {data.LocalizationLanguage} in {AssetDatabase.GetAssetPath(data)}");
                    isValid = false;
                }
                
                foreach (LocalizedString localizedString in data.LocalizedStrings)
                {
                    if (string.IsNullOrWhiteSpace(localizedString.Value))
                    {
                        Debug.LogError($"Empty value for key '{localizedString.Key}' in language {data.LocalizationLanguage} ({AssetDatabase.GetAssetPath(data)})");
                        isValid = false;
                    }
                }
            }

            Debug.Log(isValid ? "All localization data is valid!" : "Localization validation failed. See errors above.");
        }
    }
}