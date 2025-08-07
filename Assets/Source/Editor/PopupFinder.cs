using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection;
using BrawlLine.UI;

namespace BrawlLine.Editor
{
    public class PopupFinder : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<Type> popupTypesInProject;
        private Dictionary<Type, GameObject> popupPrefabs;
        
        [MenuItem("BrawlLine/Find All Popups")]
        public static void ShowWindow()
        {
            PopupFinder window = GetWindow<PopupFinder>("Popup Finder");
            window.RefreshPopupList();
        }
        
        void OnGUI()
        {
            GUILayout.Label("Popup Types in Project", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Refresh List"))
            {
                RefreshPopupList();
            }
            
            if (popupTypesInProject == null || popupTypesInProject.Count == 0)
            {
                GUILayout.Label("No popup types found inheriting from Popup class.");
                return;
            }
            
            GUILayout.Space(10);
            GUILayout.Label($"Found {popupTypesInProject.Count} popup type(s):", EditorStyles.boldLabel);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            
            for (int i = 0; i < popupTypesInProject.Count; i++)
            {
                GUILayout.BeginHorizontal();
                
                string namespaceName = popupTypesInProject[i].Namespace ?? "Global";
                GUILayout.Label($"{i + 1}. {popupTypesInProject[i].Name} ({namespaceName})", GUILayout.ExpandWidth(true));
                
                if (popupPrefabs.ContainsKey(popupTypesInProject[i]))
                {
                    GUILayout.Label("✓ Prefab", GUILayout.Width(60));
                }
                else
                {
                    GUILayout.Label("No Prefab", GUILayout.Width(60));
                }
                
                if (GUILayout.Button("Assign to Prefab", GUILayout.Width(100)))
                {
                    AssignToPrefab(popupTypesInProject[i]);
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndScrollView();
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Assign to All Prefabs"))
            {
                AssignToAllPrefabs();
            }
        }
        
        void RefreshPopupList()
        {
            popupTypesInProject = new List<Type>();
            popupPrefabs = new Dictionary<Type, GameObject>();
            
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Type[] types = assembly.GetTypes();
                    foreach (Type type in types)
                    {
                        if (typeof(Popup).IsAssignableFrom(type) && !type.IsAbstract && type != typeof(Popup))
                        {
                            popupTypesInProject.Add(type);
                            
                            GameObject prefab = FindPrefabWithComponent(type);

                            if (prefab != null)
                            {
                                popupPrefabs[type] = prefab;
                            }
                        }
                    }
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }
            }
            
            popupTypesInProject = popupTypesInProject.OrderBy(t => t.Name).ToList();
            Repaint();
        }
        
        GameObject FindPrefabWithComponent(Type componentType)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                
                if (prefab != null && prefab.GetComponent(componentType) != null)
                {
                    return prefab;
                }
            }
            
            return null;
        }
        
        void AssignToPrefab(Type popupType)
        {
            if (popupPrefabs.ContainsKey(popupType))
            {
                GameObject prefab = popupPrefabs[popupType];
                
                if (prefab.GetComponent<CameraCanvasAssigner>() == null)
                {
                    prefab.AddComponent<CameraCanvasAssigner>();
                    EditorUtility.SetDirty(prefab);
                    Debug.Log($"Added CameraCanvasAssigner to prefab: {prefab.name}");
                }
                else
                {
                    Debug.Log($"Prefab {prefab.name} already has CameraCanvasAssigner");
                }
            }
            else
            {
                Debug.LogWarning($"No prefab found for {popupType.Name}");
            }
        }
        
        void AssignToAllPrefabs()
        {
            int addedCount = 0;
            
            foreach (var kvp in popupPrefabs)
            {
                GameObject prefab = kvp.Value;
                
                if (prefab.GetComponent<CameraCanvasAssigner>() == null)
                {
                    prefab.AddComponent<CameraCanvasAssigner>();
                    EditorUtility.SetDirty(prefab);
                    addedCount++;
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log($"Added CameraCanvasAssigner to {addedCount} prefab(s).");
        }
    }
}