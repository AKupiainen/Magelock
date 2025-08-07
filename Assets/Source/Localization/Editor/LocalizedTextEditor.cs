using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace BrawlLine.Localization.Editor
{
    [CustomEditor(typeof(LocalizedText))]
    public class LocalizedTextEditor : UnityEditor.Editor
    {
        private SerializedProperty localizationKeyProperty;
        private List<string> availableKeys = new();
        private string[] keyArray;
        private int selectedKeyIndex = -1;

        private void OnEnable()
        {
            localizationKeyProperty = serializedObject.FindProperty("localizationKey");
            RefreshAvailableKeys();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Localization Settings", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(localizationKeyProperty, new GUIContent("Current Key"));
            EditorGUI.EndDisabledGroup();

            if (keyArray != null && keyArray.Length > 0)
            {
                string currentKey = localizationKeyProperty.stringValue;
                selectedKeyIndex = System.Array.IndexOf(keyArray, currentKey);
                
                EditorGUI.BeginChangeCheck();
                selectedKeyIndex = EditorGUILayout.Popup("Select Key", selectedKeyIndex, keyArray);
                if (EditorGUI.EndChangeCheck() && selectedKeyIndex >= 0 && selectedKeyIndex < keyArray.Length)
                {
                    localizationKeyProperty.stringValue = keyArray[selectedKeyIndex];
                    serializedObject.ApplyModifiedProperties();
                }

                if (!string.IsNullOrEmpty(currentKey))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
                    
                    EditorGUILayout.LabelField("Language: English");
                    EditorGUILayout.LabelField("Localized Text:", EditorStyles.boldLabel);

                    string localizedValue = GetLocalizedValueFromAssets(currentKey, SystemLanguage.English);
                       
                    EditorGUILayout.TextArea(localizedValue, EditorStyles.wordWrappedLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No localization keys found. Make sure you have localization data loaded.", MessageType.Warning);
                
                if (GUILayout.Button("Refresh Keys"))
                {
                    RefreshAvailableKeys();
                }
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Refresh Available Keys"))
            {
                RefreshAvailableKeys();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private string GetLocalizedValueFromAssets(string key, SystemLanguage language)
        {
            string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LocalizationData localizationAsset = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
                
                if (localizationAsset != null && 
                    localizationAsset.LocalizationLanguage == language && 
                    localizationAsset.LocalizedStrings != null)
                {
                    foreach (LocalizedString entry in localizationAsset.LocalizedStrings)
                    {
                        if (entry.Key == key)
                        {
                            return entry.Value;
                        }
                    }
                }
            }
            
            return "[Key not found]";
        }

        private void RefreshAvailableKeys()
        {
            availableKeys.Clear();
            
            string[] guids = AssetDatabase.FindAssets("t:LocalizationData");
            if (guids.Length > 0)
            {
                HashSet<string> uniqueKeys = new HashSet<string>();
                
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    LocalizationData localizationAsset = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);
                    
                    if (localizationAsset != null && localizationAsset.LocalizedStrings != null)
                    {
                        foreach (LocalizedString entry in localizationAsset.LocalizedStrings)
                        {
                            uniqueKeys.Add(entry.Key);
                        }
                    }
                }
                
                availableKeys = uniqueKeys.ToList();
                availableKeys.Sort();
                keyArray = availableKeys.ToArray();
                
                string currentKey = localizationKeyProperty.stringValue;
                selectedKeyIndex = System.Array.IndexOf(keyArray, currentKey);
            }
        }
    }
}