using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.UI;

namespace Magelock.UI.Editor
{
    [CustomEditor(typeof(CircularImage), true)]
    [CanEditMultipleObjects]
    public class CircularImageEditor : ImageEditor
    {
        private SerializedProperty segments;
        private SerializedProperty filled;
        private SerializedProperty fillAmount;
        private SerializedProperty thickness;
        private SerializedProperty zoom;
        private SerializedProperty fillMethod;
        private SerializedProperty fillClockwise;
        private SerializedProperty fillOrigin;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            segments = serializedObject.FindProperty("segments");
            filled = serializedObject.FindProperty("filled");
            fillAmount = serializedObject.FindProperty("fillAmount");
            thickness = serializedObject.FindProperty("thickness");
            zoom = serializedObject.FindProperty("zoom");
            fillMethod = serializedObject.FindProperty("fillMethod");
            fillClockwise = serializedObject.FindProperty("fillClockwise");
            fillOrigin = serializedObject.FindProperty("fillOrigin");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            base.OnInspectorGUI();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Circular Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(segments);
            EditorGUILayout.PropertyField(filled);
            EditorGUILayout.PropertyField(zoom);
            
            if (!filled.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(thickness);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fill Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(fillMethod);
            
            if (fillMethod.enumValueIndex != 0) 
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(fillAmount);
                EditorGUILayout.PropertyField(fillClockwise);
                
                string[] originOptions = new string[] { "Top", "Right", "Bottom", "Left" };
                fillOrigin.intValue = EditorGUILayout.Popup("Fill Origin", fillOrigin.intValue, originOptions);
                
                EditorGUI.indentLevel--;
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                
                foreach (var obj in targets)
                {
                    CircularImage circularImage = obj as CircularImage;
                    if (circularImage != null)
                    {
                        circularImage.SetVerticesDirty();
                    }
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        [MenuItem("GameObject/UI/Circular Image", false, 2003)]
        static void CreateCircularImage(MenuCommand menuCommand)
        {
            GameObject go = new GameObject("Circular Image");
            RectTransform rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 100);
            
            CircularImage image = go.AddComponent<CircularImage>();
            image.color = Color.white;
            image.raycastTarget = true;
            
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
            
            Canvas canvas = GameObject.FindObjectOfType<Canvas>();
            if (canvas != null && go.transform.parent == null)
            {
                go.transform.SetParent(canvas.transform, false);
            }
            
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}