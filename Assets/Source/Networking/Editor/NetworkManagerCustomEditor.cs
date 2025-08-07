using Unity.Netcode.Editor;
using UnityEditor;

namespace BrawlLine.Networking.Editor
{
    [CustomEditor(typeof(NetworkManagerCustom), true)]
    public class NetworkManagerCustomEditor : NetworkManagerEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            base.OnInspectorGUI();
        }
    }
}