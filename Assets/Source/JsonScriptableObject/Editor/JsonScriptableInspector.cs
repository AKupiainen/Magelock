using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Reflection;

namespace MageLock.JsonScriptableObject.Editor
{
    [CustomEditor(typeof(JsonScriptableObjectBase), true)]
    public class JsonScriptableObjectInspector : UnityEditor.Editor
    {
        private JsonScriptableObjectBase _jsonScriptableObject;
        private ReorderableList _reorderableList;
        private SerializedProperty _serializedListProperty;

        private void OnEnable()
        {
            _jsonScriptableObject = (JsonScriptableObjectBase)target;

            var type = _jsonScriptableObject.GetType();
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (typeof(IList).IsAssignableFrom(field.FieldType))
                {
                    _serializedListProperty = serializedObject.FindProperty(field.Name);
                    
                    if (_serializedListProperty != null && _serializedListProperty.isArray)
                    {
                        SetupReorderableList();
                        break;
                    }
                }
            }
        }

        private void SetupReorderableList()
        {
            if (_serializedListProperty == null || !_serializedListProperty.isArray) return;

            serializedObject.Update();

            _reorderableList = new ReorderableList(serializedObject, _serializedListProperty,
                draggable: true, displayHeader: true, displayAddButton: true, displayRemoveButton: true);

            _reorderableList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, ObjectNames.NicifyVariableName(_serializedListProperty.displayName));
            };

            _reorderableList.drawElementCallback = (rect, index, _, _) =>
            {
                if (index >= _serializedListProperty.arraySize) return;

                SerializedProperty elementProperty = _serializedListProperty.GetArrayElementAtIndex(index);
                if (elementProperty == null)
                {
                    EditorGUI.LabelField(rect, $"Element {index}: null");
                    return;
                }

                rect.y += 2;
                rect.x += 10;
                rect.height = EditorGUI.GetPropertyHeight(elementProperty, true);

                EditorGUI.PropertyField(rect, elementProperty, true);
            };

            _reorderableList.elementHeightCallback = index =>
            {
                if (index >= _serializedListProperty.arraySize) 
                    return EditorGUIUtility.singleLineHeight;

                SerializedProperty elementProperty = _serializedListProperty.GetArrayElementAtIndex(index);
                if (elementProperty == null) 
                    return EditorGUIUtility.singleLineHeight;

                return EditorGUI.GetPropertyHeight(elementProperty, true) + 4; 
            };

            _reorderableList.onReorderCallback = _ =>
            {
                EditorUtility.SetDirty(_jsonScriptableObject);
                serializedObject.ApplyModifiedProperties();
            };

            _reorderableList.onAddCallback = _ =>
            {
                _serializedListProperty.arraySize++;
                
                SerializedProperty newElement = _serializedListProperty.GetArrayElementAtIndex(_serializedListProperty.arraySize - 1);
                
                if (newElement != null)
                {
                    ResetSerializedPropertyToDefault(newElement);
                }

                EditorUtility.SetDirty(_jsonScriptableObject);
            };

            _reorderableList.onRemoveCallback = list =>
            {
                if (EditorUtility.DisplayDialog("Remove Element", "Are you sure you want to remove this element?", "Yes", "No"))
                {
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    EditorUtility.SetDirty(_jsonScriptableObject);
                }
            };
        }

        private void ResetSerializedPropertyToDefault(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = 0f;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = "";
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.Generic:
                    var iterator = property.Copy();
                    var end = property.GetEndProperty();
                    
                    if (iterator.NextVisible(true))
                    {
                        do
                        {
                            if (SerializedProperty.EqualContents(iterator, end))
                                break;
                                
                            ResetSerializedPropertyToDefault(iterator);
                        }
                        while (iterator.NextVisible(false));
                    }
                    break;
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("JSON Import/Export", EditorStyles.boldLabel);

            if (GUILayout.Button("Copy JSON to Clipboard", GUILayout.Height(25)))
            {
                ExportJsonToClipboard();
            }

            if (GUILayout.Button("Import from JSON File", GUILayout.Height(25)))
            {
                ImportFromJson();
                serializedObject.Update();
                Repaint();
            }

            EditorGUILayout.EndVertical();

            GUILayout.Space(10);

            if (_reorderableList != null && _serializedListProperty != null && _serializedListProperty.isArray)
            {
                EditorGUILayout.BeginVertical("box");
                _reorderableList.DoLayoutList();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No editable IList field found or list is not properly initialized.", MessageType.Info);
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_jsonScriptableObject);
            }
        }

        private void ExportJsonToClipboard()
        {
            string jsonData = JsonUtility.ToJson(_jsonScriptableObject, true);
            
            if (!string.IsNullOrEmpty(jsonData))
            {
                EditorGUIUtility.systemCopyBuffer = jsonData;
                Debug.Log("JSON data copied to clipboard!");
            }
            else
            {
                Debug.LogWarning("No JSON data to copy. Ensure ScriptableObject has serializable fields.");
            }
        }

        private void ImportFromJson()
        {
            string filePath = EditorUtility.OpenFilePanel("Import JSON to ScriptableObject", Application.dataPath, "json");

            if (!string.IsNullOrEmpty(filePath))
            {
                _jsonScriptableObject.ImportFromJson(filePath);
                EditorUtility.SetDirty(_jsonScriptableObject);
                AssetDatabase.SaveAssets();
            }
        }
    }
}