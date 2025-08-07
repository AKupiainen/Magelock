namespace MageLock.Editor
{
    using UnityEditor;
    using UnityEngine;
    using System.IO;

    public static class PlayerDataDeleteMenu
    {
        [MenuItem("MageLock/Delete Player Data File")]
        public static void DeletePlayerDataFile()
        {
            string filePath = Path.Combine(Application.persistentDataPath, "player_data.json");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log("Player data file deleted.");
            }
            else
            {
                Debug.LogWarning("Player data file does not exist.");
            }

            EditorUtility.DisplayDialog("Delete Player Data", "Player data file deletion complete.", "OK");
        }
    }
}