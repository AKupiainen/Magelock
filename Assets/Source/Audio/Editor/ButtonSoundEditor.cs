using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace MageLock.Audio.Editor
{
    public class ButtonSoundSetupEditor : EditorWindow
    {
        [SerializeField] private Sfx defaultClickSfx;
        private SerializedObject serializedObject;
        private SerializedProperty clickSfxProperty;
        
        private Vector2 scrollPosition;
        private readonly List<ButtonInfo> foundButtons = new();
        
        private struct ButtonInfo
        {
            public Button Button;
            public string ScenePath;
            public string PrefabPath;
            public bool HasComponent;
            public bool IsSelected;
        }
        
        [MenuItem("Tools/MageLock/Setup Button Sounds")]
        public static void ShowWindow()
        {
            ButtonSoundSetupEditor window = GetWindow<ButtonSoundSetupEditor>();
            window.titleContent = new GUIContent("Setup Button Sounds");
            window.Show();
        }
        
        private void OnEnable()
        {
            serializedObject = new SerializedObject(this);
            clickSfxProperty = serializedObject.FindProperty("defaultClickSfx");
            
            FindAllButtons();
        }
        
        private void OnGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.LabelField("Button Sound Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(clickSfxProperty, new GUIContent("Default Click SFX"));
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Refresh Button List"))
            {
                FindAllButtons();
            }
            
            if (GUILayout.Button("Add Component to Selected"))
            {
                AddComponentToSelected();
            }
            
            if (GUILayout.Button("Remove Component from Selected"))
            {
                RemoveComponentFromSelected();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All"))
            {
                SelectAll(true);
            }
            if (GUILayout.Button("Deselect All"))
            {
                SelectAll(false);
            }
            if (GUILayout.Button("Select All Without Component"))
            {
                SelectByComponentStatus(false);
            }
            if (GUILayout.Button("Select All With Component"))
            {
                SelectByComponentStatus(true);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"Found Buttons ({foundButtons.Count})", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < foundButtons.Count; i++)
            {
                ButtonInfo buttonInfo = foundButtons[i];
                
                EditorGUILayout.BeginHorizontal();
                
                buttonInfo.IsSelected = EditorGUILayout.Toggle(buttonInfo.IsSelected, GUILayout.Width(20));
                
                string statusIcon = buttonInfo.HasComponent ? "✓" : "✗";
                Color statusColor = buttonInfo.HasComponent ? Color.green : Color.red;
                
                GUI.color = statusColor;
                EditorGUILayout.LabelField(statusIcon, GUILayout.Width(20));
                GUI.color = Color.white;
                
                string locationInfo = string.Empty;

                if (!string.IsNullOrEmpty(buttonInfo.ScenePath))
                {
                    locationInfo = $" (Scene: {Path.GetFileNameWithoutExtension(buttonInfo.ScenePath)})";
                }
                else if (!string.IsNullOrEmpty(buttonInfo.PrefabPath))
                {
                    locationInfo = $" (Prefab: {Path.GetFileNameWithoutExtension(buttonInfo.PrefabPath)})";
                }
                
                EditorGUILayout.LabelField($"{buttonInfo.Button.name}{locationInfo}");
                
                if (GUILayout.Button("Focus", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = buttonInfo.Button.gameObject;
                    EditorGUIUtility.PingObject(buttonInfo.Button.gameObject);
                }
                
                EditorGUILayout.EndHorizontal();
                
                foundButtons[i] = buttonInfo;
            }
            
            EditorGUILayout.EndScrollView();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void FindAllButtons()
        {
            foundButtons.Clear();
            
            FindButtonsInScene();
            FindButtonsInPrefabs();
        }
        
        private void FindButtonsInScene()
        {
            Scene currentScene = SceneManager.GetActiveScene();

            Button[] sceneButtons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            
            foreach (Button button in sceneButtons)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(button.gameObject))
                    continue;
                    
                ButtonInfo info = new ButtonInfo
                {
                    Button = button,
                    ScenePath = currentScene.path,
                    PrefabPath = "",
                    HasComponent = button.GetComponent<ButtonClickSound>() != null,
                    IsSelected = false
                };
                
                foundButtons.Add(info);
            }
        }
        
        private void FindButtonsInPrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null)
                {
                    Button[] prefabButtons = prefab.GetComponentsInChildren<Button>(true);
                    
                    foreach (Button button in prefabButtons)
                    {
                        ButtonInfo info = new ButtonInfo
                        {
                            Button = button,
                            ScenePath = "",
                            PrefabPath = path,
                            HasComponent = button.GetComponent<ButtonClickSound>() != null,
                            IsSelected = false
                        };
                        
                        foundButtons.Add(info);
                    }
                }
            }
        }
        
        private void AddComponentToSelected()
        {
            if (defaultClickSfx == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a default click SFX first!", "OK");
                return;
            }
            
            int addedCount = 0;
            int skippedCount = 0;
            
            foreach (ButtonInfo buttonInfo in foundButtons)
            {
                if (!buttonInfo.IsSelected)
                    continue;
                    
                if (buttonInfo.HasComponent)
                {
                    skippedCount++;
                    continue;
                }
                
                try
                {
                    bool isPrefab = !string.IsNullOrEmpty(buttonInfo.PrefabPath);
                    
                    if (isPrefab)
                    {
                        string prefabPath = buttonInfo.PrefabPath;
                        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                        
                        if (prefabContents != null)
                        {
                            Button targetButton = FindButtonInHierarchy(prefabContents, buttonInfo.Button);
                            
                            if (targetButton != null && targetButton.GetComponent<ButtonClickSound>() == null)
                            {
                                ButtonClickSound component = targetButton.gameObject.AddComponent<ButtonClickSound>();
                                component.SetClickSfx(defaultClickSfx);
                                
                                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                                addedCount++;
                            }
                            
                            PrefabUtility.UnloadPrefabContents(prefabContents);
                        }
                    }
                    else
                    {
                        if (buttonInfo.Button != null && buttonInfo.Button.GetComponent<ButtonClickSound>() == null)
                        {
                            ButtonClickSound component = buttonInfo.Button.gameObject.AddComponent<ButtonClickSound>();
                            component.SetClickSfx(defaultClickSfx);
                            
                            EditorUtility.SetDirty(buttonInfo.Button.gameObject);
                            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                            addedCount++;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to add component to button {buttonInfo.Button?.name}: {e.Message}");
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            FindAllButtons(); 
            
            string message = $"Added ButtonClickSound component to {addedCount} buttons.";
            if (skippedCount > 0)
            {
                message += $"\nSkipped {skippedCount} buttons that already have the component.";
            }
            
            EditorUtility.DisplayDialog("Complete", message, "OK");
        }
        
        private void RemoveComponentFromSelected()
        {
            int removedCount = 0;
            
            foreach (ButtonInfo buttonInfo in foundButtons)
            {
                if (!buttonInfo.IsSelected || !buttonInfo.HasComponent)
                    continue;
                    
                try
                {
                    bool isPrefab = !string.IsNullOrEmpty(buttonInfo.PrefabPath);
                    
                    if (isPrefab)
                    {
                        string prefabPath = buttonInfo.PrefabPath;
                        GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
                        
                        if (prefabContents != null)
                        {
                            Button targetButton = FindButtonInHierarchy(prefabContents, buttonInfo.Button);
                            
                            if (targetButton != null)
                            {
                                ButtonClickSound component = targetButton.GetComponent<ButtonClickSound>();
                                if (component != null)
                                {
                                    DestroyImmediate(component);
                                    PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                                    removedCount++;
                                }
                            }
                            
                            PrefabUtility.UnloadPrefabContents(prefabContents);
                        }
                    }
                    else
                    {
                        ButtonClickSound component = buttonInfo.Button?.GetComponent<ButtonClickSound>();
                        if (component != null)
                        {
                            DestroyImmediate(component);
                            EditorUtility.SetDirty(buttonInfo.Button.gameObject);
                            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                            removedCount++;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to remove component from button {buttonInfo.Button?.name}: {e.Message}");
                }
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            FindAllButtons(); 
            
            EditorUtility.DisplayDialog("Complete", $"Removed ButtonClickSound component from {removedCount} buttons.", "OK");
        }
        
        private Button FindButtonInHierarchy(GameObject root, Button targetButton)
        {
            if (root == null || targetButton == null)
                return null;
                
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            
            string targetPath = GetHierarchyPath(targetButton.transform);
            
            foreach (Button button in buttons)
            {
                string buttonPath = GetHierarchyPath(button.transform);
                if (buttonPath.EndsWith(targetPath))
                {
                    return button;
                }
            }
            
            foreach (Button button in buttons)
            {
                if (button.name == targetButton.name)
                {
                    return button;
                }
            }
            
            return null;
        }
        
        private string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return "";
                
            string path = transform.name;
            Transform parent = transform.parent;
            
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            
            return path;
        }
        
        private void SelectAll(bool selected)
        {
            for (int i = 0; i < foundButtons.Count; i++)
            {
                ButtonInfo buttonInfo = foundButtons[i];
                buttonInfo.IsSelected = selected;
                foundButtons[i] = buttonInfo;
            }
        }
        
        private void SelectByComponentStatus(bool hasComponent)
        {
            for (int i = 0; i < foundButtons.Count; i++)
            {
                ButtonInfo buttonInfo = foundButtons[i];
                buttonInfo.IsSelected = buttonInfo.HasComponent == hasComponent;
                foundButtons[i] = buttonInfo;
            }
        }
    }
}