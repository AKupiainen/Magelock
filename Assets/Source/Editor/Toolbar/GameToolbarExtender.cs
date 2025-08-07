using System;
using System.IO;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace BrawlLine.Editor
{
    [InitializeOnLoad]
    public class GameToolbarExtender
    {
        private static string _restoreEditingLevelAfterPlayMode;
        private static string[] _sceneNames;
        private static string[] _scenePaths;
        private static int _previousSceneIndex = -1;
        
        private static RecorderController _recorderController;

        static GameToolbarExtender()
        {
            ToolbarExtender.RightToolbarGUI.Add(OnRightToolbarGUI);
            ToolbarExtender.LeftToolbarGUI.Add(OnLeftToolbarGUI);

            EditorBuildSettings.sceneListChanged += UpdateSceneList;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;

            UpdateSceneList();
        }

        private static void OnRightToolbarGUI()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Open Scene", GUILayout.ExpandWidth(false));
            int sceneIndex = EditorGUILayout.Popup(Array.IndexOf(_scenePaths, SceneManager.GetActiveScene().path),
                _sceneNames, GUILayout.MaxWidth(150f));

            EditorGUILayout.EndHorizontal();

            if (sceneIndex != _previousSceneIndex && sceneIndex != -1)
            {
                OpenSceneInEditor(_scenePaths[sceneIndex]);
                _previousSceneIndex = sceneIndex;
            }
        }

        private static void OnLeftToolbarGUI()
        {
            GUILayout.FlexibleSpace();

            Texture recordTexture = GetRecordTexture();
            GUIContent recordButtonContent = new(recordTexture);

            using (new EditorGUI.DisabledScope(!Application.isPlaying || _recorderController != null && _recorderController.IsRecording()))
            {
                if (GUILayout.Button(recordButtonContent, GUILayout.MaxWidth(25)))
                {
                    StartRecording();
                }
            }
        }

        private static Texture GetRecordTexture()
        {
            return EditorGUIUtility.IconContent("Animation.Record").image;
        }

        private static void PlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (_recorderController != null && _recorderController.IsRecording())
                {
                    _recorderController.StopRecording();
                }

                RestoreLevelAfterPlayMode();
            }
        }

        private static void RestoreLevelAfterPlayMode()
        {
            if (!string.IsNullOrEmpty(_restoreEditingLevelAfterPlayMode))
            {
                EditorSceneManager.OpenScene(_restoreEditingLevelAfterPlayMode, OpenSceneMode.Single);
            }

            _restoreEditingLevelAfterPlayMode = null;
        }

        private static void UpdateSceneList()
        {
            _scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && File.Exists(scene.path)) 
                .Select(scene => scene.path)
                .ToArray();

            _sceneNames = _scenePaths
                .Where(path => !string.IsNullOrEmpty(path)) 
                .Select(Path.GetFileNameWithoutExtension)
                .ToArray();
        }

        private static void OpenSceneInEditor(string scene)
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scene);
            }
        }

        private static void StartRecording()
        {
            RecorderControllerSettings controllerSettings =
                ScriptableObject.CreateInstance<RecorderControllerSettings>();
            
            _recorderController = new RecorderController(controllerSettings);
            
            DirectoryInfo mediaOutputFolder = new(Path.Combine(Application.dataPath, "..", "SampleRecordings"));

            MovieRecorderSettings movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieRecorderSettings.name = "My Video Recorder";
            movieRecorderSettings.Enabled = true;

            movieRecorderSettings.EncoderSettings = new CoreEncoderSettings
            {
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
                Codec = CoreEncoderSettings.OutputCodec.MP4
            };

            movieRecorderSettings.CaptureAlpha = true;
            
            PlayModeWindow.GetRenderingResolution(out var width, out var height);

            movieRecorderSettings.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = (int)width,
                OutputHeight = (int)height
            };

            string currentDate = DateTime.Now.ToString("yyyy.MM.dd");
            string currentTime = DateTime.Now.ToString("HH.mm.ss");
            string outputFile = $"{mediaOutputFolder.FullName}/video_{currentDate}_{currentTime}";

            movieRecorderSettings.OutputFile = outputFile;

            controllerSettings.AddRecorderSettings(movieRecorderSettings);
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 60.0f;

            RecorderOptions.VerboseMode = false;
            
            _recorderController.PrepareRecording();
            _recorderController.StartRecording();
        }
    }
}