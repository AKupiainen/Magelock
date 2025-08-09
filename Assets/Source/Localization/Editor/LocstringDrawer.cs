using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace MageLock.Localization.Editor
{
    [CustomPropertyDrawer(typeof(LocString))]
    public class LocStringDrawer : PropertyDrawer
    {
        private static string[] _availableKeys;
        private static bool _keysLoaded;
        private static double _lastLoadTime;
        private static readonly Dictionary<string, string> PreviewCache = new Dictionary<string, string>();
        
        private const double CacheRefreshInterval = 2.0;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty keyProperty = property.FindPropertyRelative("key");

            if (keyProperty != null)
            {
                if (!_keysLoaded || (EditorApplication.timeSinceStartup - _lastLoadTime > CacheRefreshInterval))
                {
                    if (!EditorApplication.isPlaying && !EditorApplication.isCompiling)
                    {
                        LoadAvailableKeys();
                        _keysLoaded = true;
                        _lastLoadTime = EditorApplication.timeSinceStartup;
                    }
                }

                Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
                Rect dropdownRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y,
                                           position.width - EditorGUIUtility.labelWidth - 80, EditorGUIUtility.singleLineHeight);
                Rect previewRect = new Rect(position.x + position.width - 75, position.y, 75, EditorGUIUtility.singleLineHeight);

                EditorGUI.LabelField(labelRect, label);

                if (_availableKeys != null && _availableKeys.Length > 0)
                {
                    string[] options = new string[_availableKeys.Length + 1];
                    options[0] = "None";
                    for (int i = 0; i < _availableKeys.Length; i++)
                    {
                        options[i + 1] = _availableKeys[i];
                    }

                    int currentIndex = 0;
                    if (!string.IsNullOrEmpty(keyProperty.stringValue))
                    {
                        currentIndex = System.Array.IndexOf(_availableKeys, keyProperty.stringValue) + 1;
                    }

                    EditorGUI.BeginChangeCheck();
                    int newIndex = EditorGUI.Popup(dropdownRect, currentIndex, options);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (newIndex == 0)
                        {
                            keyProperty.stringValue = "";
                        }
                        else if (newIndex > 0 && newIndex <= _availableKeys.Length)
                        {
                            keyProperty.stringValue = _availableKeys[newIndex - 1];
                        }
                    }
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    string newKey = EditorGUI.TextField(dropdownRect, keyProperty.stringValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        keyProperty.stringValue = newKey;
                    }
                }

                if (!string.IsNullOrEmpty(keyProperty.stringValue) && _keysLoaded)
                {
                    string localizedValue = GetLocalizedPreview(keyProperty.stringValue);

                    GUIStyle previewStyle = new GUIStyle(EditorStyles.helpBox)
                    {
                        fontSize = 9,
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.gray }
                    };

                    string displayValue = localizedValue.Length > 10 ?
                        localizedValue.Substring(0, 10) + "..." : localizedValue;

                    GUI.Label(previewRect, new GUIContent(displayValue, localizedValue), previewStyle);
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
            }

            EditorGUI.EndProperty();
        }

        private void LoadAvailableKeys()
        {
            try
            {
                List<string> allKeys = new List<string>();

                string[] guids = AssetDatabase.FindAssets("t:LocalizationData");

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    if (string.IsNullOrEmpty(path))
                        continue;
                        
                    LocalizationData asset = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);

                    if (asset != null && asset.LocalizationLanguage == SystemLanguage.English)
                    {
                        if (asset.LocalizedStrings != null)
                        {
                            allKeys.AddRange(asset.LocalizedStrings
                                .Where(ls => !string.IsNullOrEmpty(ls.Key))
                                .Select(ls => ls.Key));
                        }
                    }
                }

                _availableKeys = allKeys.Distinct().OrderBy(k => k).ToArray();
                
                PreviewCache.Clear();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error loading localization keys: {e.Message}");
                _availableKeys = new string[0];
            }
        }

        private string GetLocalizedPreview(string key)
        {
            if (PreviewCache.ContainsKey(key))
            {
                return PreviewCache[key];
            }

            try
            {
                string[] guids = AssetDatabase.FindAssets("t:LocalizationData");

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    
                    if (string.IsNullOrEmpty(path))
                        continue;
                        
                    LocalizationData asset = AssetDatabase.LoadAssetAtPath<LocalizationData>(path);

                    if (asset != null && asset.LocalizationLanguage == SystemLanguage.English)
                    {
                        var localizedString = asset.LocalizedStrings?.FirstOrDefault(ls => ls.Key == key);
                        if (localizedString != null)
                        {
                            string result = string.IsNullOrEmpty(localizedString.Value) ? "❌ Empty" : localizedString.Value;
                            PreviewCache[key] = result;
                            return result;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error getting localized preview for key '{key}': {e.Message}");
            }

            string notFoundResult = "❓ Not Found";
            PreviewCache[key] = notFoundResult;
            return notFoundResult;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}