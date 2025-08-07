using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BrawlLine.Editor
{
    [InitializeOnLoad]
    public static class ToolbarExtender
    {
        private static readonly int ToolCount;
        private static GUIStyle _commandStyle;

        public static readonly List<Action> LeftToolbarGUI = new();
        public static readonly List<Action> RightToolbarGUI = new();

        private const float Space = 8;
        private const float ButtonWidth = 32;
        private const float DropdownWidth = 80;
        private const float PlayPauseStopWidth = 140;
        
        static ToolbarExtender()
        {
            Type toolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
            
            string fieldName = "ktoolCount";
            FieldInfo toolIcons = toolbarType.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            ToolCount = toolIcons != null ? (int) toolIcons.GetValue(null) : 8;
            
            ToolbarCallback.ToolbarGUI = OnGUI;
            ToolbarCallback.ToolbarGUILeft = GUILeft;
            ToolbarCallback.ToolbarGUIRight = GUIRight;
        }

        static void OnGUI()
        {
            _commandStyle ??= new GUIStyle("CommandLeft");

            float screenWidth = EditorGUIUtility.currentViewWidth;
            float playButtonsPosition = Mathf.RoundToInt((screenWidth - PlayPauseStopWidth) / 2);

            Rect leftRect = new Rect(0, 0, screenWidth, Screen.height);
            leftRect.xMin += Space + ButtonWidth * ToolCount + Space + 64 * 2;
            leftRect.xMax = playButtonsPosition;

            Rect rightRect = new Rect(0, 0, screenWidth, Screen.height);
            rightRect.xMin = playButtonsPosition + _commandStyle.fixedWidth * 3;
            rightRect.xMax -= Space + DropdownWidth + Space + DropdownWidth + Space + DropdownWidth + Space + ButtonWidth + Space + 78;

            leftRect.xMin += Space;
            leftRect.xMax -= Space;
            rightRect.xMin += Space;
            rightRect.xMax -= Space;

            leftRect.y = 4;
            leftRect.height = 22;
            rightRect.y = 4;
            rightRect.height = 22;

            DrawToolbar(leftRect, LeftToolbarGUI);
            DrawToolbar(rightRect, RightToolbarGUI);
        }
        
        static void DrawToolbar(Rect rect, List<Action> handlers)
        {
            if (rect.width <= 0)
            {
                return;
            }

            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            
            foreach (Action handler in handlers)
            {
                handler();
            }
            
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private static void GUILeft() 
        {
            DrawHorizontalToolbar(LeftToolbarGUI);
        }

        private static void GUIRight() 
        {
            DrawHorizontalToolbar(RightToolbarGUI);
        }

        private static void DrawHorizontalToolbar(List<Action> handlers)
        {
            GUILayout.BeginHorizontal();
            
            foreach (var handler in handlers)
            {
                handler();
            }
            
            GUILayout.EndHorizontal();
        }
    }
}