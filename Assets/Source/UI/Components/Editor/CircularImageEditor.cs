using UnityEngine;
using UnityEditor;
using UnityEditor.UI;

namespace Magelock.UI.Editor
{
    [CustomEditor(typeof(CircularImage), true)]
    [CanEditMultipleObjects]
    public class CircularImageEditor : ImageEditor
    {
        private SerializedProperty _segments;
        private SerializedProperty _filled;
        private SerializedProperty _fillAmount;
        private SerializedProperty _thickness;
        private SerializedProperty _zoom;
        private SerializedProperty _fillMethod;
        private SerializedProperty _fillClockwise;
        private SerializedProperty _fillOrigin;
        
        protected override void OnEnable()
        {
            base.OnEnable();
            
            _segments = serializedObject.FindProperty("segments");
            _filled = serializedObject.FindProperty("filled");
            _fillAmount = serializedObject.FindProperty("fillAmount");
            _thickness = serializedObject.FindProperty("thickness");
            _zoom = serializedObject.FindProperty("zoom");
            _fillMethod = serializedObject.FindProperty("fillMethod");
            _fillClockwise = serializedObject.FindProperty("fillClockwise");
            _fillOrigin = serializedObject.FindProperty("fillOrigin");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            base.OnInspectorGUI();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Circular Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(_segments);
            EditorGUILayout.PropertyField(_filled);
            EditorGUILayout.PropertyField(_zoom);
            
            if (!_filled.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_thickness);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Fill Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(_fillMethod);
            
            if (_fillMethod.enumValueIndex != 0) 
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_fillAmount);
                EditorGUILayout.PropertyField(_fillClockwise);
                
                string[] originOptions = { "Top", "Right", "Bottom", "Left" };
                _fillOrigin.intValue = EditorGUILayout.Popup("Fill Origin", _fillOrigin.intValue, originOptions);
                
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
            
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null && go.transform.parent == null)
            {
                go.transform.SetParent(canvas.transform, false);
            }
            
            rectTransform.anchoredPosition = Vector2.zero;
        }
    }
}