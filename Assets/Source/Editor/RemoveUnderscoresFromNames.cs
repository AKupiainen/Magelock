using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace BrawlLine.Editor
{
    public static class RemoveUnderscoresFromNames
    {
        [MenuItem("BrawlLine/Remove Underscores From Names")]
        private static void RemoveUnderscores()
        {
            GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            int renameCount = 0;

            foreach (GameObject root in rootObjects)
            {
                renameCount += ProcessGameObjectRecursive(root);
            }

            Debug.Log($"Removed underscores from {renameCount} GameObjects.");
        }

        private static int ProcessGameObjectRecursive(GameObject obj)
        {
            int count = 0;

            if (obj.name.Contains("_"))
            {
                Undo.RecordObject(obj, "Rename GameObject");
                obj.name = obj.name.Replace("_", "");
                count++;
            }

            foreach (Transform child in obj.transform)
            {
                count += ProcessGameObjectRecursive(child.gameObject);
            }

            return count;
        }
    }
}