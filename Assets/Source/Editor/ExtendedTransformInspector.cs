namespace BrawlLine.Editor
{
    using UnityEngine;
    using UnityEditor;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Transform))]
    public class TransformInspector : Editor
    {
        private Transform targetTransform;

        private void OnEnable()
        {
            targetTransform = (Transform)target;
        }

        public override void OnInspectorGUI()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("T", GUILayout.Width(20)))
            {
                if (Selection.transforms.Length > 1)
                {
                    foreach (Transform trans in Selection.transforms)
                    {
                        trans.localPosition = Reset(new Vector3(0f, 0f, 0f));
                    }
                }

                else
                {
                    targetTransform.localPosition = Reset(new Vector3(0f, 0f, 0f));
                }
            }

            Vector3 position = EditorGUILayout.Vector3Field("Position", targetTransform.localPosition);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("R", GUILayout.Width(20)))
            {
                if (Selection.transforms.Length > 1)
                {
                    foreach (Transform trans in Selection.transforms)
                    {
                        trans.localEulerAngles = Reset(new Vector3(0f, 0f, 0f));
                    }
                }

                else
                {
                    targetTransform.transform.localEulerAngles = Reset(new Vector3(0f, 0f, 0f));
                }
            }

            Vector3 eulerAngles = EditorGUILayout.Vector3Field("Rotation", targetTransform.localEulerAngles);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("S", GUILayout.Width(20)))
            {
                if (Selection.transforms.Length > 1)
                {
                    foreach (Transform trans in Selection.transforms)
                    {
                        trans.localScale = Reset(new Vector3(1f, 1f, 1f));
                    }
                }

                else
                {
                    targetTransform.localScale = Reset(new Vector3(1f, 1f, 1f));
                }
            }

            Vector3 scale = EditorGUILayout.Vector3Field("Scale", targetTransform.localScale);

            GUILayout.EndHorizontal();

            if (GUI.changed)
            {
                if (Selection.transforms.Length > 1)
                {
                    foreach (Transform trans in Selection.transforms)
                    {
                        trans.localPosition = FixIfNaN(position);
                        trans.localEulerAngles = FixIfNaN(eulerAngles);
                        trans.localScale = FixIfNaN(scale);
                    }
                }

                else
                {
                    Undo.RegisterCompleteObjectUndo(targetTransform, "Transform Change");
                    targetTransform.localPosition = FixIfNaN(position);
                    targetTransform.localEulerAngles = FixIfNaN(eulerAngles);
                    targetTransform.localScale = FixIfNaN(scale);
                }

                EditorUtility.SetDirty(targetTransform);
            }
        }

        private Vector3 FixIfNaN(Vector3 v)
        {
            if (float.IsNaN(v.x))
            {
                v.x = 0;
            }

            if (float.IsNaN(v.y))
            {
                v.y = 0;
            }

            if (float.IsNaN(v.z))
            {
                v.z = 0;
            }
            return v;
        }

        private Vector3 Reset(Vector3 v)
        {
            v = new Vector3(v.x, v.y, v.z);

            return v;
        }
    }
}