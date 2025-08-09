using UnityEngine;
using System.IO;

namespace MageLock.JsonScriptableObject
{
    /// <summary>
    /// Base class for ScriptableObjects that can be imported/exported as JSON
    /// </summary>
    public abstract class JsonScriptableObjectBase : ScriptableObject
    {
        /// <summary>
        /// Export this ScriptableObject to JSON format
        /// </summary>
        /// <param name="filePath">Full path where to save the JSON file</param>
        public virtual void ExportToJson(string filePath)
        {
            try
            {
                string json = JsonUtility.ToJson(this, true);                
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    if (directory != null) Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(filePath, json);
                
                Debug.Log($"Successfully exported {name} to: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to export JSON: {e.Message}");
            }
        }
        
        /// <summary>
        /// Import JSON data into this ScriptableObject
        /// </summary>
        /// <param name="filePath">Full path to the JSON file to import</param>
        public virtual void ImportFromJson(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"File not found: {filePath}");
                    return;
                }
                
                string json = File.ReadAllText(filePath);
                
                JsonUtility.FromJsonOverwrite(json, this);
                
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
                
                Debug.Log($"Successfully imported JSON to {name} from: {filePath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to import JSON: {e.Message}");
            }
        }
        
        /// <summary>
        /// Get the default export filename for this ScriptableObject
        /// </summary>
        /// <returns>Default filename with .json extension</returns>
        public virtual string GetDefaultExportFilename()
        {
            return $"{name}_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
        }

        /// <summary>
        /// Validate the imported data. Override this to add custom validation logic.
        /// </summary>
        /// <returns>True if data is valid, false otherwise</returns>
        public virtual bool ValidateImportedData()
        {
            return true;
        }
        
        /// <summary>
        /// Called before exporting to JSON. Override to prepare data for export.
        /// </summary>
        protected virtual void OnBeforeExport()
        {
            // Override in derived classes if needed
        }
        
        /// <summary>
        /// Called after importing from JSON. Override to process imported data.
        /// </summary>
        protected virtual void OnAfterImport() { }
    }
}