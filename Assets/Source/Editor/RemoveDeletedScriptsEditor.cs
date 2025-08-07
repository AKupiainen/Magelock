using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace BrawlLine.Editor
{
    public class RemoveDeletedScripts : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool includePrefabs = true;
        private bool includeScenes = true;
        private bool showProgress;
        private readonly List<string> processedAssets = new();
        private readonly List<string> assetsWithMissingScripts = new();
        
        [MenuItem("BrawlLine/Remove Deleted Scripts")]
        public static void ShowWindow()
        {
            GetWindow<RemoveDeletedScripts>("Remove Deleted Scripts");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Remove Deleted Scripts from Assets", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool will scan through all assets and remove missing/deleted script components. " +
                "Make sure to backup your project before running this operation.", 
                MessageType.Warning);
            
            GUILayout.Space(10);
            
            includePrefabs = EditorGUILayout.Toggle("Include Prefabs", includePrefabs);
            includeScenes = EditorGUILayout.Toggle("Include Scenes", includeScenes);
            
            GUILayout.Space(10);
            
            EditorGUI.BeginDisabledGroup(showProgress);
            
            if (GUILayout.Button("Remove Deleted Scripts", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm Operation", 
                    "This will remove all missing script components from selected asset types. Continue?", 
                    "Yes", "Cancel"))
                {
                    RemoveDeletedScriptsFromAssets();
                }
            }
            
            EditorGUI.EndDisabledGroup();
            
            if (showProgress)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Processing...", EditorStyles.boldLabel);
            }
            
            if (processedAssets.Count > 0)
            {
                GUILayout.Space(20);
                EditorGUILayout.LabelField($"Processed Assets: {processedAssets.Count}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Assets with Missing Scripts: {assetsWithMissingScripts.Count}", EditorStyles.boldLabel);
                
                if (assetsWithMissingScripts.Count > 0)
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Assets that had missing scripts:", EditorStyles.boldLabel);
                    
                    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                    
                    foreach (string assetPath in assetsWithMissingScripts)
                    {
                        EditorGUILayout.SelectableLabel(assetPath, GUILayout.Height(16));
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
            }
        }
        
        private void RemoveDeletedScriptsFromAssets()
        {
            showProgress = true;
            processedAssets.Clear();
            assetsWithMissingScripts.Clear();
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                if (includePrefabs)
                {
                    ProcessPrefabs();
                }
                
                if (includeScenes)
                {
                    ProcessScenes();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                showProgress = false;
            }
            
            Debug.Log($"Removed deleted scripts operation completed. Processed {processedAssets.Count} assets, " +
                     $"found missing scripts in {assetsWithMissingScripts.Count} assets.");
        }
        
        private void ProcessPrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                
                if (EditorUtility.DisplayCancelableProgressBar("Processing Prefabs", 
                    $"Processing: {assetPath}", (float)i / prefabGuids.Length))
                {
                    break;
                }
                
                ProcessPrefab(assetPath);
            }
            
            EditorUtility.ClearProgressBar();
        }
        
        private void ProcessPrefab(string assetPath)
        {
            processedAssets.Add(assetPath);
            
            if (AssetDatabase.IsOpenForEdit(assetPath) == false)
            {
                Debug.LogWarning($"Skipping read-only asset: {assetPath}");
                return;
            }
            
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            if (prefabRoot == null) return;
            
            try
            {
                GameObject[] allObjects = prefabRoot.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject).ToArray();
                
                bool hasDeletedScripts = false;
                
                foreach (GameObject obj in allObjects)
                {
                    if (RemoveDeletedScriptsFromGameObject(obj))
                    {
                        hasDeletedScripts = true;
                    }
                }
                
                if (hasDeletedScripts)
                {
                    assetsWithMissingScripts.Add(assetPath);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
        
        private void ProcessScenes()
        {
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                
                if (EditorUtility.DisplayCancelableProgressBar("Processing Scenes", 
                    $"Processing: {assetPath}", (float)i / sceneGuids.Length))
                {
                    break;
                }
                
                ProcessScene(assetPath);
            }
            
            // Restore the original scene
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath);
            }
            
            EditorUtility.ClearProgressBar();
        }
        
        private void ProcessScene(string scenePath)
        {
            if (AssetDatabase.IsOpenForEdit(scenePath) == false)
            {
                Debug.LogWarning($"Skipping read-only asset: {scenePath}");
                return;
            }
            
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;
            
            processedAssets.Add(scenePath);
            
            GameObject[] rootObjects = scene.GetRootGameObjects();
            bool hasDeletedScripts = false;
            
            foreach (GameObject rootObj in rootObjects)
            {
                GameObject[] allObjects = rootObj.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject).ToArray();
                
                foreach (GameObject obj in allObjects)
                {
                    if (RemoveDeletedScriptsFromGameObject(obj))
                    {
                        hasDeletedScripts = true;
                    }
                }
            }
            
            if (hasDeletedScripts)
            {
                assetsWithMissingScripts.Add(scenePath);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        
        private bool RemoveDeletedScriptsFromGameObject(GameObject obj)
        {
            if (obj == null) return false;
            
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
            
            if (missingCount > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                return true;
            }
            
            return false;
        }
    }
    
    // Alternative static method approach for batch processing
    public static class DeletedScriptCleaner
    {
        [MenuItem("BrawlLine/Quick Clean All Assets")]
        public static void QuickCleanAllAssets()
        {
            if (!EditorUtility.DisplayDialog("Quick Clean", 
                "This will remove all missing scripts from ALL prefabs and scenes. Continue?", 
                "Yes", "Cancel"))
            {
                return;
            }
            
            int totalCleaned = 0;
            
            try
            {
                AssetDatabase.StartAssetEditing();
                
                // Find all prefabs - this was the missing line!
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                    EditorUtility.DisplayProgressBar("Cleaning Prefabs", path, (float)i / prefabGuids.Length);
                    
                    if (CleanAsset(path))
                        totalCleaned++;
                }
                
                // Clean scenes
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");
                string currentScene = EditorSceneManager.GetActiveScene().path;
                
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    EditorUtility.DisplayProgressBar("Cleaning Scenes", path, (float)i / sceneGuids.Length);
                    
                    if (CleanScene(path))
                        totalCleaned++;
                }
                
                if (!string.IsNullOrEmpty(currentScene))
                    EditorSceneManager.OpenScene(currentScene);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
            
            Debug.Log($"Quick clean completed. Cleaned {totalCleaned} assets.");
            EditorUtility.DisplayDialog("Complete", $"Cleaned {totalCleaned} assets with missing scripts.", "OK");
        }
        
        private static bool CleanAsset(string assetPath)
        {
            if (AssetDatabase.IsOpenForEdit(assetPath) == false)
            {
                Debug.LogWarning($"Skipping read-only asset: {assetPath}");
                return false;
            }
            
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
            if (prefabRoot == null) return false;
            
            bool cleaned = false;
            
            try
            {
                cleaned = CleanGameObjectHierarchy(prefabRoot);
                
                if (cleaned)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
            
            return cleaned;
        }
        
        private static bool CleanScene(string scenePath)
        {
            if (AssetDatabase.IsOpenForEdit(scenePath) == false)
            {
                Debug.LogWarning($"Skipping read-only asset: {scenePath}");
                return false;
            }
            
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return false;
            
            bool cleaned = false;
            GameObject[] rootObjects = scene.GetRootGameObjects();
            
            foreach (GameObject root in rootObjects)
            {
                if (CleanGameObjectHierarchy(root))
                    cleaned = true;
            }
            
            if (cleaned)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            
            return cleaned;
        }
        
        private static bool CleanGameObjectHierarchy(GameObject root)
        {
            GameObject[] allObjects = root.GetComponentsInChildren<Transform>(true)
                .Select(t => t.gameObject).ToArray();
            
            bool cleaned = false;
            foreach (GameObject obj in allObjects)
            {
                if (RemoveMissingScripts(obj))
                    cleaned = true;
            }
            
            return cleaned;
        }
        
        private static bool RemoveMissingScripts(GameObject obj)
        {
            if (obj == null) return false;
            
            int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
            
            if (missingCount > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                return true;
            }
            
            return false;
        }
    }
}