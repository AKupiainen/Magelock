using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NamespaceChanger : EditorWindow
{
    private string sourceNamespace = "";
    private string targetNamespace = "";
    private string searchPath = "Assets";
    private bool includeSubfolders = true;
    private bool previewChanges = true;
    private Vector2 scrollPosition;
    private List<FileChangeInfo> previewFiles = new List<FileChangeInfo>();
    
    private class FileChangeInfo
    {
        public string filePath;
        public string oldContent;
        public string newContent;
        public bool hasChanges;
        public int changeCount;
    }
    
    [MenuItem("Tools/Namespace Changer")]
    public static void ShowWindow()
    {
        GetWindow<NamespaceChanger>("Namespace Changer");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Unity Namespace Changer", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Input fields
        GUILayout.Label("Source Namespace (will find all occurrences in code):");
        sourceNamespace = EditorGUILayout.TextField(sourceNamespace);
        
        GUILayout.Space(5);
        GUILayout.Label("Target Namespace:");
        targetNamespace = EditorGUILayout.TextField(targetNamespace);
        
        GUILayout.Space(10);
        
        // Path settings
        GUILayout.Label("Search Settings:", EditorStyles.boldLabel);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search Path:", GUILayout.Width(80));
        searchPath = EditorGUILayout.TextField(searchPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Folder", searchPath, "");
            if (!string.IsNullOrEmpty(path))
            {
                // Convert absolute path to relative path from project root
                string projectPath = Application.dataPath.Replace("/Assets", "");
                if (path.StartsWith(projectPath))
                {
                    searchPath = path.Substring(projectPath.Length + 1);
                }
            }
        }
        GUILayout.EndHorizontal();
        
        includeSubfolders = EditorGUILayout.Toggle("Include Subfolders", includeSubfolders);
        previewChanges = EditorGUILayout.Toggle("Preview Changes", previewChanges);
        
        GUILayout.Space(10);
        
        // Action buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview Changes", GUILayout.Height(30)))
        {
            PreviewNamespaceChanges();
        }
        
        GUI.enabled = !string.IsNullOrEmpty(sourceNamespace) && !string.IsNullOrEmpty(targetNamespace);
        if (GUILayout.Button("Apply Changes", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Confirm Changes", 
                $"Are you sure you want to replace all occurrences of '{sourceNamespace}' with '{targetNamespace}'?\n\nThis will affect namespace declarations, using statements, type references, etc.\n\nThis action cannot be undone!", 
                "Yes", "Cancel"))
            {
                ApplyNamespaceChanges();
            }
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
        
        GUILayout.Space(10);
        
        // Preview results
        if (previewFiles.Count > 0)
        {
            GUILayout.Label($"Preview Results ({previewFiles.Count} files found):", EditorStyles.boldLabel);
            
            int totalChanges = previewFiles.Sum(f => f.changeCount);
            GUILayout.Label($"Total changes: {totalChanges}");
            
            GUILayout.Space(5);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            foreach (var file in previewFiles)
            {
                if (file.hasChanges)
                {
                    EditorGUILayout.BeginVertical("box");
                    GUILayout.Label($"📁 {file.filePath} ({file.changeCount} changes)", EditorStyles.boldLabel);
                    
                    if (previewChanges)
                    {
                        GUILayout.Label("Changes:", EditorStyles.miniLabel);
                        string[] oldLines = file.oldContent.Split('\n');
                        string[] newLines = file.newContent.Split('\n');
                        
                        for (int i = 0; i < Math.Min(oldLines.Length, newLines.Length); i++)
                        {
                            if (oldLines[i] != newLines[i])
                            {
                                GUI.color = Color.red;
                                GUILayout.Label($"- {oldLines[i]}", EditorStyles.miniLabel);
                                GUI.color = Color.green;
                                GUILayout.Label($"+ {newLines[i]}", EditorStyles.miniLabel);
                                GUI.color = Color.white;
                            }
                        }
                    }
                    
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(5);
                }
            }
            
            GUILayout.EndScrollView();
        }
        
        GUILayout.Space(10);
        
        // Help text
        EditorGUILayout.HelpBox(
            "Instructions:\n" +
            "1. Enter the source namespace to find and target namespace to replace with\n" +
            "2. This will replace ALL occurrences of the source namespace in the code\n" +
            "3. Includes: namespace declarations, using statements, type references, etc.\n" +
            "4. Choose search path (default is entire Assets folder)\n" +
            "5. Click 'Preview Changes' to see what will be modified\n" +
            "6. Click 'Apply Changes' to execute the replacement\n\n" +
            "Examples:\n" +
            "- Source: 'OldNamespace' → Target: 'NewNamespace'\n" +
            "- Source: 'MyGame.Old' → Target: 'MyGame.New'\n\n" +
            "Note: Uses whole-word matching to prevent partial replacements. Backup your project!",
            MessageType.Info);
    }
    
    private void PreviewNamespaceChanges()
    {
        previewFiles.Clear();
        
        string[] files = GetCSharpFiles();
        
        foreach (string file in files)
        {
            try
            {
                string content = File.ReadAllText(file);
                string modifiedContent = ProcessFileContent(content);
                
                var fileInfo = new FileChangeInfo
                {
                    filePath = file,
                    oldContent = content,
                    newContent = modifiedContent,
                    hasChanges = content != modifiedContent,
                    changeCount = CountChanges(content, modifiedContent)
                };
                
                previewFiles.Add(fileInfo);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing file {file}: {e.Message}");
            }
        }
        
        Debug.Log($"Preview completed. Found {previewFiles.Count(f => f.hasChanges)} files with changes.");
    }
    
    private void ApplyNamespaceChanges()
    {
        int changedFiles = 0;
        
        try
        {
            string[] files = GetCSharpFiles();
            
            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                string modifiedContent = ProcessFileContent(content);
                
                if (content != modifiedContent)
                {
                    File.WriteAllText(file, modifiedContent, Encoding.UTF8);
                    changedFiles++;
                }
            }
            
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Namespace Change Complete", 
                $"Successfully updated {changedFiles} files.\n\nDon't forget to check for compilation errors!", 
                "OK");
            
            Debug.Log($"Namespace change completed. Modified {changedFiles} files.");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"An error occurred: {e.Message}", "OK");
            Debug.LogError($"Error during namespace change: {e.Message}");
        }
    }
    
    private string[] GetCSharpFiles()
    {
        string fullPath = Path.Combine(Application.dataPath, searchPath.Replace("Assets/", "").Replace("Assets", ""));
        
        if (!Directory.Exists(fullPath))
        {
            Debug.LogError($"Directory not found: {fullPath}");
            return new string[0];
        }
        
        SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        return Directory.GetFiles(fullPath, "*.cs", searchOption);
    }
    
    private string ProcessFileContent(string content)
    {
        // Skip if source namespace is empty or content doesn't contain the source namespace
        if (string.IsNullOrEmpty(sourceNamespace) || !content.Contains(sourceNamespace))
        {
            return content;
        }
        
        // Replace all occurrences of source namespace with target namespace
        return ReplaceAllNamespaceOccurrences(content);
    }
    
    private string ReplaceAllNamespaceOccurrences(string content)
    {
        if (string.IsNullOrEmpty(sourceNamespace) || string.IsNullOrEmpty(targetNamespace))
            return content;
            
        // Create a pattern that matches the source namespace as a whole word
        // This prevents partial matches (e.g., "MyNamespace" matching "MyNamespaceExtended")
        string pattern = @"\b" + Regex.Escape(sourceNamespace) + @"\b";
        
        // Replace all occurrences
        return Regex.Replace(content, pattern, targetNamespace, RegexOptions.Multiline);
    }
    
    private int CountChanges(string oldContent, string newContent)
    {
        if (oldContent == newContent || string.IsNullOrEmpty(sourceNamespace)) 
            return 0;
        
        // Count all occurrences of the source namespace that will be replaced
        string pattern = @"\b" + Regex.Escape(sourceNamespace) + @"\b";
        return Regex.Matches(oldContent, pattern, RegexOptions.Multiline).Count;
    }
}