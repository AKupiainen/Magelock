using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace BrawlLine.Localization.Editor
{
    public class LocalizationJsonImporter : EditorWindow
    {
        private string jsonFilePath;
        
        [MenuItem("Tools/Localization/Import Localizations from JSON")]
        public static void ShowWindow()
        {
            LocalizationJsonImporter window = GetWindow<LocalizationJsonImporter>(true, "Localization JSON Importer");
            window.Show();
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Localization JSON Importer", EditorStyles.boldLabel);
            jsonFilePath = EditorGUILayout.TextField("JSON File Path:", jsonFilePath);
            
            if (GUILayout.Button("Select JSON File"))
            {
                string selectedPath = EditorUtility.OpenFilePanel("Select Localization JSON", "Assets", "json");
                
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    jsonFilePath = selectedPath.Replace(Application.dataPath, "Assets");
                }
            }
            
            if (GUILayout.Button("Import Localizations") && !string.IsNullOrEmpty(jsonFilePath))
            {
                ImportLocalizations(jsonFilePath);
            }
        }
        
        private static void ImportLocalizations(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
            {
                Debug.LogError("Localization JSON file not found: " + jsonFilePath);
                return;
            }
            
            string json = File.ReadAllText(jsonFilePath);
            Dictionary<string, Dictionary<string, string>> keyToLanguageValues = 
                JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
            
            Dictionary<string, Dictionary<string, string>> languageToKeyValues = new Dictionary<string, Dictionary<string, string>>();
            
            foreach (var keyEntry in keyToLanguageValues)
            {
                string key = keyEntry.Key;
                Dictionary<string, string> languageValues = keyEntry.Value;
                
                foreach (var langEntry in languageValues)
                {
                    string language = langEntry.Key;
                    string value = langEntry.Value;
                    
                    if (!languageToKeyValues.ContainsKey(language))
                    {
                        languageToKeyValues[language] = new Dictionary<string, string>();
                    }
                    
                    languageToKeyValues[language][key] = value;
                }
            }
            
            string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LocalizationData localizationAsset = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
                SystemLanguage language = localizationAsset.LocalizationLanguage;
                string langKey = language.ToString().ToUpper();
                
                if (languageToKeyValues.TryGetValue(langKey, out Dictionary<string, string> value))
                {
                    UpdateLocalizationAsset(localizationAsset, value);
                    EditorUtility.SetDirty(localizationAsset);
                }
                else
                {
                    Debug.Log($"No localization data found for language: {langKey}");
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log("Localization import completed successfully!");
        }
        
        private static void UpdateLocalizationAsset(LocalizationData localizationAsset, Dictionary<string, string> localizedStrings)
        {
            HashSet<string> existingKeys = new HashSet<string>();
            
            foreach (LocalizedString entry in localizationAsset.LocalizedStrings)
            {
                existingKeys.Add(entry.Key);
            }
            
            foreach (KeyValuePair<string, string> kvp in localizedStrings)
            {
                if (!existingKeys.Contains(kvp.Key))
                {
                    localizationAsset.LocalizedStrings.Add(new LocalizedString { Key = kvp.Key, Value = kvp.Value });
                    Debug.Log($"Added new localization: {kvp.Key} -> {kvp.Value}");
                }
            }
        }
    }
}