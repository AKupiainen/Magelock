using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MageLock.Localization.Editor
{
    [CustomEditor(typeof(LocalizedText))]
    public class LocalizedTextEditor : UnityEditor.Editor
    {
        private SerializedProperty _localizationKeyProperty;
        private List<string> _availableKeys = new();
        private string[] _keyArray;
        private int _selectedKeyIndex = -1;

        private void OnEnable()
        {
            _localizationKeyProperty = serializedObject.FindProperty("localizationKey");
            RefreshAvailableKeys();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Localization Settings", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(_localizationKeyProperty, new GUIContent("Current Key"));
            EditorGUI.EndDisabledGroup();

            if (_keyArray != null && _keyArray.Length > 0)
            {
                string currentKey = _localizationKeyProperty.stringValue;
                _selectedKeyIndex = System.Array.IndexOf(_keyArray, currentKey);
                
                EditorGUI.BeginChangeCheck();
                _selectedKeyIndex = EditorGUILayout.Popup("Select Key", _selectedKeyIndex, _keyArray);
                if (EditorGUI.EndChangeCheck() && _selectedKeyIndex >= 0 && _selectedKeyIndex < _keyArray.Length)
                {
                    _localizationKeyProperty.stringValue = _keyArray[_selectedKeyIndex];
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
            _availableKeys.Clear();
            
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
                
                _availableKeys = uniqueKeys.ToList();
                _availableKeys.Sort();
                _keyArray = _availableKeys.ToArray();
                
                string currentKey = _localizationKeyProperty.stringValue;
                _selectedKeyIndex = System.Array.IndexOf(_keyArray, currentKey);
            }
        }
    }
}