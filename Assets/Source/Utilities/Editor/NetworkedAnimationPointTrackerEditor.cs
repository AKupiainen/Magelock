using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;


namespace MageLock.Utilities.Editor
{
    [CustomEditor(typeof(NetworkedAnimationPointTracker))]
    public class NetworkedAnimationPointTrackerEditor : UnityEditor.Editor
    {
        private NetworkedAnimationPointTracker tracker;
        private string[] availableBones;
        private bool showBoneList = false;
        
        private void OnEnable()
        {
            tracker = (NetworkedAnimationPointTracker)target;
            RefreshBoneList();
        }
        
        private void RefreshBoneList()
        {
            availableBones = tracker.GetAvailableBoneNames();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetRenderer"));
            
            EditorGUILayout.Space();
            
            if (Application.isPlaying && NetworkManager.Singleton != null)
            {
                EditorGUILayout.HelpBox(
                    $"Network Status: {(tracker.IsServer ? "Server" : "Client")}\n" +
                    $"Owner: {(tracker.IsOwner ? "Yes" : "No")}", 
                    MessageType.Info);
            }
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Refresh Bones", GUILayout.Height(25)))
            {
                RefreshBoneList();
                tracker.SetupBonesInEditor();
                SceneView.RepaintAll();
            }
            
            if (GUILayout.Button("Add Point", GUILayout.Height(25)))
            {
                Undo.RecordObject(tracker, "Add Tracked Point");
                var points = serializedObject.FindProperty("trackedPoints");
                points.InsertArrayElementAtIndex(points.arraySize);
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (availableBones != null && availableBones.Length > 0)
            {
                EditorGUILayout.Space();
                showBoneList = EditorGUILayout.Foldout(showBoneList, 
                    $"Available Bones ({availableBones.Length})", true);
                
                if (showBoneList)
                {
                    EditorGUI.indentLevel++;
                    
                    string search = EditorGUILayout.TextField("Search", "");
                    
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    int shown = 0;
                    foreach (string boneName in availableBones)
                    {
                        if (string.IsNullOrEmpty(search) || 
                            boneName.ToLower().Contains(search.ToLower()))
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField(boneName);
                            if (GUILayout.Button("Copy", GUILayout.Width(50)))
                            {
                                EditorGUIUtility.systemCopyBuffer = boneName;
                            }
                            EditorGUILayout.EndHorizontal();
                            
                            shown++;
                            if (shown > 20 && string.IsNullOrEmpty(search))
                            {
                                EditorGUILayout.LabelField("...", EditorStyles.centeredGreyMiniLabel);
                                break;
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                    
                    EditorGUI.indentLevel--;
                }
            }
            
            EditorGUILayout.Space();
            
            var trackedPoints = serializedObject.FindProperty("trackedPoints");
            
            EditorGUILayout.LabelField("Tracked Points", EditorStyles.boldLabel);
            
            if (trackedPoints.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No tracked points. Click 'Add Point' to create one.", 
                    MessageType.Info);
            }
            
            for (int i = 0; i < trackedPoints.arraySize; i++)
            {
                var point = trackedPoints.GetArrayElementAtIndex(i);
                
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                
                point.isExpanded = EditorGUILayout.Foldout(point.isExpanded, 
                    $"Point {i}: {point.FindPropertyRelative("name").stringValue}", true);
                
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    trackedPoints.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    break;
                }
                
                EditorGUILayout.EndHorizontal();
                
                if (point.isExpanded)
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.PropertyField(point.FindPropertyRelative("name"));
                    
                    var boneNameProp = point.FindPropertyRelative("boneName");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(boneNameProp);
                    
                    if (availableBones != null && availableBones.Length > 0)
                    {
                        if (GUILayout.Button("▼", GUILayout.Width(25)))
                        {
                            GenericMenu menu = new GenericMenu();
                            foreach (string bone in availableBones)
                            {
                                string boneName = bone;
                                menu.AddItem(new GUIContent(boneName), 
                                    boneNameProp.stringValue == boneName,
                                    () => {
                                        boneNameProp.stringValue = boneName;
                                        serializedObject.ApplyModifiedProperties();
                                        tracker.SetupBonesInEditor();
                                        SceneView.RepaintAll();
                                    });
                            }
                            menu.ShowAsContext();
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.PropertyField(point.FindPropertyRelative("localOffset"));
                    EditorGUILayout.PropertyField(point.FindPropertyRelative("localRotationOffset"));
                    
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("syncOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("debugNetworking"));
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showGizmos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gizmoSize"));
            
            serializedObject.ApplyModifiedProperties();
            
            if (GUI.changed)
            {
                tracker.SetupBonesInEditor();
                SceneView.RepaintAll();
            }
        }
    }
}