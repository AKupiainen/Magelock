#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace BrawlLine.Graphics.Editor
{
    [System.Serializable]
    public class ToonShaderSettings
    {
        public Color color = Color.white;
        
        public float shadowThreshold = 0.5f;
        public float shadowSmoothness = 0.1f;
        public Color shadowColor = new(0.3f, 0.3f, 0.3f, 1f);
        public float lightRampOffset;
        public float posterizeLevels = 3f;
        public float posterizePower = 1f;
        public float useRimLight = 1f;
        public Color rimColor = Color.white;
        public float rimPower = 2f;
        public float rimIntensity = 1f;
        public float rimSmoothness = 0.5f;
        public float useSpecular = 1f;
        public Color specularColor = Color.white;
        public float specularSize = 0.1f;
        public float specularSmoothness = 0.5f;
        public float specularSteps = 1f;
        public float useOutline = 1f;
        public Color outlineColor = Color.black;
        public float outlineWidth = 0.005f;
        public float outlineAdaptive;
        public float useHalftone;
        public float halftoneScale = 10f;
        public float halftoneThreshold = 0.5f;
        public float halftoneSmoothness = 0.1f;
        public float indirectLightStrength = 0.3f;
        public float lightWrapAround;
        public float receiveShadows = 1f;
    }

    public class ToonShaderGUI : ShaderGUI
    {
        private MaterialProperty mainTex;
        private MaterialProperty color;
        private MaterialProperty shadowThreshold;
        private MaterialProperty shadowSmoothness;
        private MaterialProperty shadowColor;
        private MaterialProperty lightRampTex;
        private MaterialProperty lightRampOffset;
        private MaterialProperty posterizeLevels;
        private MaterialProperty posterizePower;
        private MaterialProperty useRimLight;
        private MaterialProperty rimColor;
        private MaterialProperty rimPower;
        private MaterialProperty rimIntensity;
        private MaterialProperty rimSmoothness;
        private MaterialProperty useSpecular;
        private MaterialProperty specularColor;
        private MaterialProperty specularSize;
        private MaterialProperty specularSmoothness;
        private MaterialProperty specularSteps;
        private MaterialProperty useOutline;
        private MaterialProperty outlineColor;
        private MaterialProperty outlineWidth;
        private MaterialProperty outlineAdaptive;
        private MaterialProperty useHalftone;
        private MaterialProperty halftoneTex;
        private MaterialProperty halftoneScale;
        private MaterialProperty halftoneThreshold;
        private MaterialProperty halftoneSmoothness;
        private MaterialProperty indirectLightStrength;
        private MaterialProperty lightWrapAround;
        private MaterialProperty receiveShadows;

        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle presetButtonStyle;
        private GUIStyle randomizeButtonStyle;
        private GUIStyle toggleButtonStyle;

        private readonly Color accentColor = new(0.3f, 0.7f, 1f);
        private readonly Color warningColor = new(1f, 0.6f, 0.2f);
        private readonly Color successColor = new(0.3f, 0.8f, 0.3f);
        private readonly Color errorColor = new(1f, 0.3f, 0.3f);

        private static bool _showMainSettings = true;
        private static bool _showShadingSettings = true;
        private static bool _showRimSettings = true;
        private static bool _showSpecularSettings = true;
        private static bool _showOutlineSettings = true;
        private static bool _showHalftoneSettings;
        private static bool _showAdvancedSettings;
        private static bool _showPresets = true;
        private bool stylesInitialized;
        
        public ToonShaderGUI() { }

        public ToonShaderGUI(MaterialProperty halftoneScale)
        {
            this.halftoneScale = halftoneScale;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            if (!stylesInitialized)
            {
                SetupStyles();
                stylesInitialized = true;
            }

            Material material = materialEditor.target as Material;

            EditorGUI.BeginChangeCheck();

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawPresetSection(material);
            EditorGUILayout.Space(5);

            DrawMainSection(materialEditor);
            DrawShadingSection(materialEditor);
            DrawRimLightSection(materialEditor);
            DrawSpecularSection(materialEditor);
            DrawOutlineSection(materialEditor);
            DrawHalftoneSection(materialEditor);
            DrawAdvancedSection(materialEditor);

            EditorGUILayout.Space(10);
            DrawUtilityButtons(material);

            if (EditorGUI.EndChangeCheck())
            {
                UpdateShaderKeywords(material);
            }
        }

        void FindProperties(MaterialProperty[] props)
        {
            mainTex = FindProperty("_MainTex", props, false);
            color = FindProperty("_Color", props, false);
            shadowThreshold = FindProperty("_ShadowThreshold", props, false);
            shadowSmoothness = FindProperty("_ShadowSmoothness", props, false);
            shadowColor = FindProperty("_ShadowColor", props, false);
            lightRampTex = FindProperty("_LightRampTex", props, false);
            lightRampOffset = FindProperty("_RampOffset", props, false);
            posterizeLevels = FindProperty("_PosterizeLevels", props, false);
            posterizePower = FindProperty("_PosterizePower", props, false);
            useRimLight = FindProperty("_UseRimLight", props, false);
            rimColor = FindProperty("_RimColor", props, false);
            rimPower = FindProperty("_RimPower", props, false);
            rimIntensity = FindProperty("_RimIntensity", props, false);
            rimSmoothness = FindProperty("_RimSmoothness", props, false);
            useSpecular = FindProperty("_UseSpecular", props, false);
            specularColor = FindProperty("_SpecularColor", props, false);
            specularSize = FindProperty("_SpecularSize", props, false);
            specularSmoothness = FindProperty("_SpecularSmoothness", props, false);
            specularSteps = FindProperty("_SpecularSteps", props, false);
            useOutline = FindProperty("_UseOutline", props, false);
            outlineColor = FindProperty("_OutlineColor", props, false);
            outlineWidth = FindProperty("_OutlineWidth", props, false);
            outlineAdaptive = FindProperty("_OutlineAdaptive", props, false);
            useHalftone = FindProperty("_UseHalftone", props, false);
            halftoneTex = FindProperty("_HalftoneTex", props, false);
            halftoneScale = FindProperty("_HalftoneScale", props, false);
            halftoneThreshold = FindProperty("_HalftoneThreshold", props, false);
            halftoneSmoothness = FindProperty("_HalftoneSmoothness", props, false);
            indirectLightStrength = FindProperty("_IndirectLightStrength", props, false);
            lightWrapAround = FindProperty("_LightWrapAround", props, false);
            receiveShadows = FindProperty("_ReceiveShadows", props, false);
        }

        void SetupStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 18;
            headerStyle.normal.textColor = accentColor;
            headerStyle.alignment = TextAnchor.MiddleCenter;

            sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionStyle.fontSize = 12;
            sectionStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            presetButtonStyle = new GUIStyle(GUI.skin.button);
            presetButtonStyle.fontSize = 10;
            presetButtonStyle.fontStyle = FontStyle.Bold;
            presetButtonStyle.fixedHeight = 25;

            randomizeButtonStyle = new GUIStyle(GUI.skin.button);
            randomizeButtonStyle.fontSize = 9;
            randomizeButtonStyle.fontStyle = FontStyle.Bold;
            randomizeButtonStyle.fixedHeight = 18;
            randomizeButtonStyle.fixedWidth = 65;

            toggleButtonStyle = new GUIStyle(GUI.skin.button);
            toggleButtonStyle.fontSize = 10;
            toggleButtonStyle.fixedHeight = 20;
        }

        void DrawHeader()
        {
            EditorGUILayout.Space(5);

            var headerRect = GUILayoutUtility.GetRect(0, 35, GUILayout.ExpandWidth(true));
            var originalColor = GUI.color;

            GUI.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.2f);
            GUI.DrawTexture(headerRect, EditorGUIUtility.whiteTexture);
            GUI.color = originalColor;

            GUI.Label(headerRect, "🎨 TOON SHADER", headerStyle);

            EditorGUILayout.Space(5);
            DrawSeparator();
        }

        void DrawPresetSection(Material material)
        {
            DrawSectionHeader("🎯 Style Presets", ref _showPresets, successColor);

            if (_showPresets)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField("Quick style configurations:", EditorStyles.miniLabel);
                    EditorGUILayout.Space(3);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawPresetButton("🎌 Anime", () => ApplyAnimePreset(material));
                        DrawPresetButton("📚 Comic", () => ApplyComicPreset(material));
                        DrawPresetButton("🎮 Game", () => ApplyGamePreset(material));
                        DrawPresetButton("🕹️ Retro", () => ApplyRetroPreset(material));
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawPresetButton("🎨 Soft", () => ApplySoftPreset(material));
                        DrawPresetButton("⚡ Neon", () => ApplyNeonPreset(material));
                        DrawPresetButton("🌙 Moody", () => ApplyMoodyPreset(material));
                        DrawPresetButton("☀️ Bright", () => ApplyBrightPreset(material));
                    }
                }
            }
        }

        void DrawMainSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("🎨 Main Color", ref _showMainSettings, accentColor);

            if (_showMainSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (mainTex != null)
                    {
                        materialEditor.ShaderProperty(mainTex, "Main Texture");
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (color != null)
                        {
                            materialEditor.ShaderProperty(color, "Base Color");
                        }

                        DrawRandomizeButton(() =>
                        {
                            if (color != null)
                            {
                                color.colorValue = Random.ColorHSV(0f, 1f, 0.4f, 1f, 0.6f, 1f);
                            }
                        }, warningColor);
                    }
                }
            }
        }

        void DrawShadingSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("☀️ Shading", ref _showShadingSettings, new Color(1f, 0.8f, 0.4f));

            if (_showShadingSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Shadow Settings", sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(() => RandomizeShadingSettings(), new Color(1f, 0.7f, 0.3f));
                    }

                    if (shadowThreshold != null)
                    {
                        materialEditor.ShaderProperty(shadowThreshold, "Shadow Threshold");
                    }
                    if (shadowSmoothness != null)
                    {
                        materialEditor.ShaderProperty(shadowSmoothness, "Shadow Smoothness");
                    }
                    if (shadowColor != null)
                    {
                        materialEditor.ShaderProperty(shadowColor, "Shadow Color");
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Ramp Shading", sectionStyle);
                    if (lightRampTex != null)
                    {
                        materialEditor.ShaderProperty(lightRampTex, "Light Ramp Texture");
                    }
                    if (lightRampOffset != null)
                    {
                        materialEditor.ShaderProperty(lightRampOffset, "Light Ramp Offset");
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Posterize Shading", sectionStyle);
                    if (posterizeLevels != null)
                    {
                        materialEditor.ShaderProperty(posterizeLevels, "Posterize Levels");
                    }
                    if (posterizePower != null)
                    {
                        materialEditor.ShaderProperty(posterizePower, "Posterize Power");
                    }
                }
            }
        }

        void DrawRimLightSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("✨ Rim Lighting", ref _showRimSettings, new Color(0.4f, 0.8f, 1f), useRimLight);

            if (_showRimSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Rim Light Settings", sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(() => RandomizeRimSettings(), new Color(0.3f, 0.8f, 1f));
                    }

                    bool useRim = useRimLight != null && useRimLight.floatValue > 0.5f;
                    using (new EditorGUI.DisabledScope(!useRim))
                    {
                        if (rimColor != null)
                        {
                            materialEditor.ShaderProperty(rimColor, "Rim Color");
                        }
                        if (rimPower != null)
                        {
                            materialEditor.ShaderProperty(rimPower, "Rim Power");
                        }
                        if (rimIntensity != null)
                        {
                            materialEditor.ShaderProperty(rimIntensity, "Rim Intensity");
                        }
                        if (rimSmoothness != null)
                        {
                            materialEditor.ShaderProperty(rimSmoothness, "Rim Smoothness");
                        }
                    }
                }
            }
        }

        void DrawSpecularSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("💎 Specular", ref _showSpecularSettings, new Color(1f, 1f, 0.4f), useSpecular);

            if (_showSpecularSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Specular Settings", sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(RandomizeSpecularSettings, new Color(1f, 1f, 0.3f));
                    }

                    bool useSpec = useSpecular != null && useSpecular.floatValue > 0.5f;
                    using (new EditorGUI.DisabledScope(!useSpec))
                    {
                        if (specularColor != null)
                        {
                            materialEditor.ShaderProperty(specularColor, "Specular Color");
                        }
                        if (specularSize != null)
                        {
                            materialEditor.ShaderProperty(specularSize, "Specular Size");
                        }
                        if (specularSmoothness != null)
                        {
                            materialEditor.ShaderProperty(specularSmoothness, "Specular Smoothness");
                        }
                        if (specularSteps != null)
                        {
                            materialEditor.ShaderProperty(specularSteps, "Specular Steps");
                        }
                    }
                }
            }
        }

        void DrawOutlineSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("🖼️ Outline", ref _showOutlineSettings, new Color(0.8f, 0.4f, 1f), useOutline);

            if (_showOutlineSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Outline Settings", sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(RandomizeOutlineSettings, new Color(0.8f, 0.3f, 1f));
                    }

                    bool useOutline = this.useOutline != null && this.useOutline.floatValue > 0.5f;
                    using (new EditorGUI.DisabledScope(!useOutline))
                    {
                        if (outlineColor != null)
                        {
                            materialEditor.ShaderProperty(outlineColor, "Outline Color");
                        }
                        if (outlineWidth != null)
                        {
                            materialEditor.ShaderProperty(outlineWidth, "Outline Width");
                        }
                        if (outlineAdaptive != null)
                        {
                            materialEditor.ShaderProperty(outlineAdaptive, "Outline Adaptive");
                        }
                    }
                }
            }
        }

        void DrawHalftoneSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("🔲 Halftone", ref _showHalftoneSettings, new Color(1f, 0.6f, 0.2f), useHalftone);

            if (_showHalftoneSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Halftone Settings", sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(() => RandomizeHalftoneSettings(), new Color(1f, 0.5f, 0.1f));
                    }

                    bool useHalftone = this.useHalftone != null && this.useHalftone.floatValue > 0.5f;
                    using (new EditorGUI.DisabledScope(!useHalftone))
                    {
                        if (halftoneTex != null)
                        {
                            materialEditor.ShaderProperty(halftoneTex, "Halftone Texture");
                        }
                        if (halftoneScale != null)
                        {
                            materialEditor.ShaderProperty(halftoneScale, "Halftone Scale");
                        }
                        if (halftoneThreshold != null)
                        {
                            materialEditor.ShaderProperty(halftoneThreshold, "Halftone Threshold");
                        }
                        if (halftoneSmoothness != null)
                        {
                            materialEditor.ShaderProperty(halftoneSmoothness, "Halftone Smoothness");
                        }
                    }
                }
            }
        }

        void DrawAdvancedSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("⚙️ Advanced", ref _showAdvancedSettings, new Color(0.6f, 0.6f, 0.6f));

            if (_showAdvancedSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Advanced Settings", sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(RandomizeAdvancedSettings, new Color(0.5f, 0.9f, 0.5f));
                    }

                    if (indirectLightStrength != null)
                    {
                        materialEditor.ShaderProperty(indirectLightStrength, "Indirect Light Strength");
                    }
                    if (lightWrapAround != null)
                    {
                        materialEditor.ShaderProperty(lightWrapAround, "Light Wrap Around");
                    }
                    if (receiveShadows != null)
                    {
                        materialEditor.ShaderProperty(receiveShadows, "Receive Shadows");
                    }
                }
            }
        }

        void DrawUtilityButtons(Material material)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("🛠️ Utilities", sectionStyle);
                EditorGUILayout.Space(3);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var randomAllColor = GUI.backgroundColor;
                    GUI.backgroundColor = errorColor;
                    if (GUILayout.Button("🎲 RANDOMIZE ALL", GUILayout.Height(30)))
                    {
                        RandomizeAllSettings();
                        UpdateShaderKeywords(material);
                    }
                    GUI.backgroundColor = randomAllColor;

                    var resetColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.7f, 0.7f, 0.7f);
                    if (GUILayout.Button("🔄 Reset", GUILayout.Height(30)))
                    {
                        ResetToDefault(material);
                    }
                    GUI.backgroundColor = resetColor;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("📋 Copy Settings", GUILayout.Height(25)))
                    {
                        CopySettings(material);
                    }

                    if (GUILayout.Button("📄 Paste Settings", GUILayout.Height(25)))
                    {
                        PasteSettings(material);
                    }
                }
            }
        }

        void DrawSectionHeader(string title, ref bool foldout, Color color, MaterialProperty toggleProp = null)
        {
            EditorGUILayout.Space(3);

            var headerRect = GUILayoutUtility.GetRect(0, 25, GUILayout.ExpandWidth(true));

            var originalColor = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, 0.3f);
            GUI.DrawTexture(headerRect, EditorGUIUtility.whiteTexture);
            GUI.color = originalColor;

            var foldoutRect = new Rect(headerRect.x + 5, headerRect.y + 3, 15, 15);
            foldout = EditorGUI.Foldout(foldoutRect, foldout, "");

            var titleRect = new Rect(headerRect.x + 25, headerRect.y + 3, headerRect.width - 100, 20);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.normal.textColor = color;
            GUI.Label(titleRect, title, titleStyle);

            if (toggleProp != null)
            {
                var toggleRect = new Rect(headerRect.x + headerRect.width - 60, headerRect.y + 2, 50, 20);
                var oldColor = GUI.backgroundColor;
                bool isOn = toggleProp.floatValue > 0.5f;
                GUI.backgroundColor = isOn ? successColor : new Color(0.5f, 0.5f, 0.5f);

                if (GUI.Button(toggleRect, isOn ? "ON" : "OFF", toggleButtonStyle))
                {
                    toggleProp.floatValue = isOn ? 0f : 1f;
                }

                GUI.backgroundColor = oldColor;
            }
        }

        void DrawPresetButton(string label, System.Action action)
        {
            if (GUILayout.Button(label, presetButtonStyle))
            {
                action.Invoke();
            }
        }

        void DrawRandomizeButton(System.Action action, Color color)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            if (GUILayout.Button("🎲", randomizeButtonStyle))
            {
                action.Invoke();
            }

            GUI.backgroundColor = oldColor;
        }

        void DrawSeparator()
        {
            EditorGUILayout.Space(2);
            var rect = GUILayoutUtility.GetRect(0f, 1f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(2);
        }

        void UpdateShaderKeywords(Material material)
        {
            if (useRimLight != null)
            {
                SetKeyword(material, "_USERIMLIGHT_ON", useRimLight.floatValue > 0.5f);
            }
            if (useSpecular != null)
            {
                SetKeyword(material, "_USESPECULAR_ON", useSpecular.floatValue > 0.5f);
            }
            if (useOutline != null)
            {
                SetKeyword(material, "_USEOUTLINE_ON", useOutline.floatValue > 0.5f);
            }
            if (useHalftone != null)
            {
                SetKeyword(material, "_USEHALFTONE_ON", useHalftone.floatValue > 0.5f);
            }
            if (receiveShadows != null)
            {
                SetKeyword(material, "_RECEIVESHADOWS_ON", receiveShadows.floatValue > 0.5f);
            }
        }

        void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }

        #region Preset Methods

        void ApplyAnimePreset(Material material)
        {
            if (color != null) { color.colorValue = new Color(0.95f, 0.9f, 0.85f); }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.6f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.15f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.4f, 0.4f, 0.6f); }
            if (useRimLight != null) { useRimLight.floatValue = 1f; }
            if (rimColor != null) { rimColor.colorValue = Color.cyan; }
            if (rimPower != null) { rimPower.floatValue = 3f; }
            if (rimIntensity != null) { rimIntensity.floatValue = 2f; }
            if (useOutline != null) { useOutline.floatValue = 1f; }
            if (outlineWidth != null) { outlineWidth.floatValue = 0.008f; }
            if (outlineColor != null) { outlineColor.colorValue = Color.black; }
            UpdateShaderKeywords(material);
        }

        void ApplyComicPreset(Material material)
        {
            if (color != null) { color.colorValue = new Color(0.8f, 0.8f, 0.9f); }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.4f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.05f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.2f, 0.2f, 0.4f); }
            if (useRimLight != null) { useRimLight.floatValue = 1f; }
            if (rimColor != null) { rimColor.colorValue = Color.white; }
            if (rimPower != null) { rimPower.floatValue = 4f; }
            if (rimIntensity != null) { rimIntensity.floatValue = 1.5f; }
            if (useSpecular != null) { useSpecular.floatValue = 1f; }
            if (specularSteps != null) { specularSteps.floatValue = 3f; }
            if (useOutline != null) { useOutline.floatValue = 1f; }
            if (outlineWidth != null) { outlineWidth.floatValue = 0.012f; }
            UpdateShaderKeywords(material);
        }

        void ApplyGamePreset(Material material)
        {
            if (color != null) { color.colorValue = new Color(0.8f, 0.9f, 0.7f); }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.5f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.2f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.3f, 0.3f, 0.5f); }
            if (useRimLight != null) { useRimLight.floatValue = 1f; }
            if (rimColor != null) { rimColor.colorValue = Color.yellow; }
            if (rimPower != null) { rimPower.floatValue = 2f; }
            if (rimIntensity != null) { rimIntensity.floatValue = 1.2f; }
            if (useOutline != null) { useOutline.floatValue = 1f; }
            if (outlineWidth != null) { outlineWidth.floatValue = 0.006f; }
            if (lightWrapAround != null) { lightWrapAround.floatValue = 0.3f; }
            UpdateShaderKeywords(material);
        }

        void ApplyRetroPreset(Material material)
        {
            if (color != null) { color.colorValue = new Color(0.9f, 0.7f, 0.8f); }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.3f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.02f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.2f, 0.1f, 0.3f); }
            if (useRimLight != null) { useRimLight.floatValue = 0f; }
            if (useSpecular != null) { useSpecular.floatValue = 1f; }
            if (specularSteps != null) { specularSteps.floatValue = 4f; }
            if (useOutline != null) { useOutline.floatValue = 1f; }
            if (outlineWidth != null) { outlineWidth.floatValue = 0.015f; }
            UpdateShaderKeywords(material);
        }

        void ApplySoftPreset(Material material)
        {
            if (color != null) { color.colorValue = new Color(0.9f, 0.85f, 0.8f); }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.7f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.3f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.5f, 0.4f, 0.6f); }
            if (useRimLight != null) { useRimLight.floatValue = 1f; }
            if (rimColor != null) { rimColor.colorValue = new Color(1f, 0.8f, 0.6f); }
            if (rimPower != null) { rimPower.floatValue = 1.5f; }
            if (rimIntensity != null) { rimIntensity.floatValue = 0.8f; }
            if (rimSmoothness != null) { rimSmoothness.floatValue = 0.8f; }
            if (useOutline != null) { useOutline.floatValue = 0f; }
            if (lightWrapAround != null) { lightWrapAround.floatValue = 0.5f; }
            UpdateShaderKeywords(material);
        }

        void ApplyNeonPreset(Material material)
        {
            if (color != null) { color.colorValue = new Color(0.1f, 0.1f, 0.2f); }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.2f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.1f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.05f, 0.05f, 0.1f); }
            if (useRimLight != null) { useRimLight.floatValue = 1f; }
            if (rimColor != null) { rimColor.colorValue = new Color(0f, 1f, 1f); }
            if (rimPower != null) { rimPower.floatValue = 2f; }
            if (rimIntensity != null) { rimIntensity.floatValue = 3f; }
            if (useSpecular != null) { useSpecular.floatValue = 1f; }
            if (specularColor != null) { specularColor.colorValue = new Color(1f, 0f, 1f); }
            if (useOutline != null) { useOutline.floatValue = 1f; }
            if (outlineColor != null) { outlineColor.colorValue = new Color(0f, 0.5f, 1f); }
            if (outlineWidth != null) { outlineWidth.floatValue = 0.01f; }
            UpdateShaderKeywords(material);
        }

        void ApplyMoodyPreset(Material material)
        {
            if (color != null) { color.colorValue = new Color(0.4f, 0.3f, 0.5f); }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.8f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.25f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.1f, 0.1f, 0.2f); }
            if (useRimLight != null) { useRimLight.floatValue = 1f; }
            if (rimColor != null) { rimColor.colorValue = new Color(0.8f, 0.4f, 0.2f); }
            if (rimPower != null) { rimPower.floatValue = 4f; }
            if (rimIntensity != null) { rimIntensity.floatValue = 1f; }
            if (indirectLightStrength != null) { indirectLightStrength.floatValue = 0.1f; }
            if (useOutline != null) { useOutline.floatValue = 1f; }
            if (outlineColor != null) { outlineColor.colorValue = new Color(0.1f, 0.1f, 0.1f); }
            UpdateShaderKeywords(material);
        }

        void ApplyBrightPreset(Material material)
        {
            if (color != null) { color.colorValue = Color.white; }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.3f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.2f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.7f, 0.7f, 0.8f); }
            if (useRimLight != null) { useRimLight.floatValue = 1f; }
            if (rimColor != null) { rimColor.colorValue = new Color(1f, 1f, 0.8f); }
            if (rimPower != null) { rimPower.floatValue = 1f; }
            if (rimIntensity != null) { rimIntensity.floatValue = 1f; }
            if (indirectLightStrength != null) { indirectLightStrength.floatValue = 0.8f; }
            if (lightWrapAround != null) { lightWrapAround.floatValue = 0.4f; }
            if (useOutline != null) { useOutline.floatValue = 0f; }
            UpdateShaderKeywords(material);
        }

        #endregion

        #region Randomization Methods

        void RandomizeShadingSettings()
        {
            if (shadowThreshold != null) { shadowThreshold.floatValue = Random.Range(0.2f, 0.8f); }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = Random.Range(0.05f, 0.3f); }
            if (shadowColor != null) { shadowColor.colorValue = Random.ColorHSV(0f, 1f, 0.3f, 0.7f, 0.2f, 0.8f); }
            if (lightRampOffset != null) { lightRampOffset.floatValue = Random.Range(-0.5f, 0.5f); }
            if (posterizeLevels != null) { posterizeLevels.floatValue = Random.Range(2f, 8f); }
            if (posterizePower != null) { posterizePower.floatValue = Random.Range(0.5f, 2.5f); }
        }

        void RandomizeRimSettings()
        {
            if (rimColor != null) { rimColor.colorValue = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f); }
            if (rimPower != null) { rimPower.floatValue = Random.Range(1f, 8f); }
            if (rimIntensity != null) { rimIntensity.floatValue = Random.Range(0.5f, 3f); }
            if (rimSmoothness != null) { rimSmoothness.floatValue = Random.Range(0.1f, 0.8f); }
        }

        void RandomizeSpecularSettings()
        {
            if (specularColor != null) { specularColor.colorValue = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.9f, 1f); }
            if (specularSize != null) { specularSize.floatValue = Random.Range(0.05f, 0.4f); }
            if (specularSmoothness != null) { specularSmoothness.floatValue = Random.Range(0.1f, 0.9f); }
            if (specularSteps != null) { specularSteps.floatValue = Random.Range(1f, 5f); }
        }

        void RandomizeOutlineSettings()
        {
            if (outlineColor != null) { outlineColor.colorValue = Random.ColorHSV(0f, 1f, 0f, 0.3f, 0f, 0.5f); }
            if (outlineWidth != null) { outlineWidth.floatValue = Random.Range(0.002f, 0.015f); }
            if (outlineAdaptive != null) { outlineAdaptive.floatValue = Random.Range(0f, 0.8f); }
        }

        void RandomizeHalftoneSettings()
        {
            if (halftoneScale != null) { halftoneScale.floatValue = Random.Range(5f, 20f); }
            if (halftoneThreshold != null) { halftoneThreshold.floatValue = Random.Range(0.2f, 0.8f); }
            if (halftoneSmoothness != null) { halftoneSmoothness.floatValue = Random.Range(0.05f, 0.3f); }
        }

        void RandomizeAdvancedSettings()
        {
            if (indirectLightStrength != null) { indirectLightStrength.floatValue = Random.Range(0.1f, 0.8f); }
            if (lightWrapAround != null) { lightWrapAround.floatValue = Random.Range(0f, 0.6f); }
            if (receiveShadows != null) { receiveShadows.floatValue = Random.value > 0.3f ? 1f : 0f; }
        }

        void RandomizeAllSettings()
        {
            if (color != null) { color.colorValue = Random.ColorHSV(0f, 1f, 0.4f, 1f, 0.6f, 1f); }

            if (useRimLight != null) { useRimLight.floatValue = Random.value > 0.3f ? 1f : 0f; }
            if (useSpecular != null) { useSpecular.floatValue = Random.value > 0.4f ? 1f : 0f; }
            if (useOutline != null) { useOutline.floatValue = Random.value > 0.2f ? 1f : 0f; }
            if (useHalftone != null) { useHalftone.floatValue = Random.value > 0.7f ? 1f : 0f; }

            RandomizeShadingSettings();
            RandomizeRimSettings();
            RandomizeSpecularSettings();
            RandomizeOutlineSettings();
            RandomizeHalftoneSettings();
            RandomizeAdvancedSettings();
        }

        void ResetToDefault(Material material)
        {
            if (color != null) { color.colorValue = Color.white; }
            if (shadowThreshold != null) { shadowThreshold.floatValue = 0.5f; }
            if (shadowSmoothness != null) { shadowSmoothness.floatValue = 0.1f; }
            if (shadowColor != null) { shadowColor.colorValue = new Color(0.3f, 0.3f, 0.3f, 1f); }
            if (lightRampOffset != null) { lightRampOffset.floatValue = 0f; }
            if (posterizeLevels != null) { posterizeLevels.floatValue = 3f; }
            if (posterizePower != null) { posterizePower.floatValue = 1f; }
            if (useRimLight != null) { useRimLight.floatValue = 1f; }
            if (rimColor != null) { rimColor.colorValue = Color.white; }
            if (rimPower != null) { rimPower.floatValue = 2f; }
            if (rimIntensity != null) { rimIntensity.floatValue = 1f; }
            if (rimSmoothness != null) { rimSmoothness.floatValue = 0.5f; }
            if (useSpecular != null) { useSpecular.floatValue = 1f; }
            if (specularColor != null) { specularColor.colorValue = Color.white; }
            if (specularSize != null) { specularSize.floatValue = 0.1f; }
            if (specularSmoothness != null) { specularSmoothness.floatValue = 0.5f; }
            if (specularSteps != null) { specularSteps.floatValue = 1f; }
            if (useOutline != null) { useOutline.floatValue = 1f; }
            if (outlineColor != null) { outlineColor.colorValue = Color.black; }
            if (outlineWidth != null) { outlineWidth.floatValue = 0.005f; }
            if (outlineAdaptive != null) { outlineAdaptive.floatValue = 0f; }
            if (useHalftone != null) { useHalftone.floatValue = 0f; }
            if (halftoneScale != null) { halftoneScale.floatValue = 10f; }
            if (halftoneThreshold != null) { halftoneThreshold.floatValue = 0.5f; }
            if (halftoneSmoothness != null) { halftoneSmoothness.floatValue = 0.1f; }
            if (indirectLightStrength != null) { indirectLightStrength.floatValue = 0.3f; }
            if (lightWrapAround != null) { lightWrapAround.floatValue = 0f; }
            if (receiveShadows != null) { receiveShadows.floatValue = 1f; }

            UpdateShaderKeywords(material);
        }

        #endregion

        #region Utility Methods

        void CopySettings(Material material)
        {
            var settings = new ToonShaderSettings();

            if (color != null) settings.color = color.colorValue;
            if (shadowThreshold != null) settings.shadowThreshold = shadowThreshold.floatValue;
            if (shadowSmoothness != null) settings.shadowSmoothness = shadowSmoothness.floatValue;
            if (shadowColor != null) settings.shadowColor = shadowColor.colorValue;
            if (lightRampOffset != null) settings.lightRampOffset = lightRampOffset.floatValue;
            if (posterizeLevels != null) settings.posterizeLevels = posterizeLevels.floatValue;
            if (posterizePower != null) settings.posterizePower = posterizePower.floatValue;

            if (useRimLight != null) settings.useRimLight = useRimLight.floatValue;
            if (rimColor != null) settings.rimColor = rimColor.colorValue;
            if (rimPower != null) settings.rimPower = rimPower.floatValue;
            if (rimIntensity != null) settings.rimIntensity = rimIntensity.floatValue;
            if (rimSmoothness != null) settings.rimSmoothness = rimSmoothness.floatValue;

            if (useSpecular != null) settings.useSpecular = useSpecular.floatValue;
            if (specularColor != null) settings.specularColor = specularColor.colorValue;
            if (specularSize != null) settings.specularSize = specularSize.floatValue;
            if (specularSmoothness != null) settings.specularSmoothness = specularSmoothness.floatValue;
            if (specularSteps != null) settings.specularSteps = specularSteps.floatValue;

            if (useOutline != null) settings.useOutline = useOutline.floatValue;
            if (outlineColor != null) settings.outlineColor = outlineColor.colorValue;
            if (outlineWidth != null) settings.outlineWidth = outlineWidth.floatValue;
            if (outlineAdaptive != null) settings.outlineAdaptive = outlineAdaptive.floatValue;

            if (useHalftone != null) settings.useHalftone = useHalftone.floatValue;
            if (halftoneScale != null) settings.halftoneScale = halftoneScale.floatValue;
            if (halftoneThreshold != null) settings.halftoneThreshold = halftoneThreshold.floatValue;
            if (halftoneSmoothness != null) settings.halftoneSmoothness = halftoneSmoothness.floatValue;

            if (indirectLightStrength != null) settings.indirectLightStrength = indirectLightStrength.floatValue;
            if (lightWrapAround != null) settings.lightWrapAround = lightWrapAround.floatValue;
            if (receiveShadows != null) settings.receiveShadows = receiveShadows.floatValue;

            string json = JsonUtility.ToJson(settings, true);
            EditorGUIUtility.systemCopyBuffer = json;
            Debug.Log("Toon Shader settings copied to clipboard!");
        }

        void PasteSettings(Material material)
        {
            try
            {
                string json = EditorGUIUtility.systemCopyBuffer;

                if (string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning("Clipboard is empty!");
                    return;
                }

                ToonShaderSettings settings = JsonUtility.FromJson<ToonShaderSettings>(json);

                if (settings == null)
                {
                    Debug.LogWarning("Failed to parse clipboard data as ToonShaderSettings!");
                    return;
                }

                if (color != null) color.colorValue = settings.color;
                if (shadowThreshold != null) shadowThreshold.floatValue = settings.shadowThreshold;
                if (shadowSmoothness != null) shadowSmoothness.floatValue = settings.shadowSmoothness;
                if (shadowColor != null) shadowColor.colorValue = settings.shadowColor;
                if (lightRampOffset != null) lightRampOffset.floatValue = settings.lightRampOffset;
                if (posterizeLevels != null) posterizeLevels.floatValue = settings.posterizeLevels;
                if (posterizePower != null) posterizePower.floatValue = settings.posterizePower;

                if (useRimLight != null) useRimLight.floatValue = settings.useRimLight;
                if (rimColor != null) rimColor.colorValue = settings.rimColor;
                if (rimPower != null) rimPower.floatValue = settings.rimPower;
                if (rimIntensity != null) rimIntensity.floatValue = settings.rimIntensity;
                if (rimSmoothness != null) rimSmoothness.floatValue = settings.rimSmoothness;

                if (useSpecular != null) useSpecular.floatValue = settings.useSpecular;
                if (specularColor != null) specularColor.colorValue = settings.specularColor;
                if (specularSize != null) specularSize.floatValue = settings.specularSize;
                if (specularSmoothness != null) specularSmoothness.floatValue = settings.specularSmoothness;
                if (specularSteps != null) specularSteps.floatValue = settings.specularSteps;

                if (useOutline != null) useOutline.floatValue = settings.useOutline;
                if (outlineColor != null) outlineColor.colorValue = settings.outlineColor;
                if (outlineWidth != null) outlineWidth.floatValue = settings.outlineWidth;
                if (outlineAdaptive != null) outlineAdaptive.floatValue = settings.outlineAdaptive;

                if (useHalftone != null) useHalftone.floatValue = settings.useHalftone;
                if (halftoneScale != null) halftoneScale.floatValue = settings.halftoneScale;
                if (halftoneThreshold != null) halftoneThreshold.floatValue = settings.halftoneThreshold;
                if (halftoneSmoothness != null) halftoneSmoothness.floatValue = settings.halftoneSmoothness;

                if (indirectLightStrength != null) indirectLightStrength.floatValue = settings.indirectLightStrength;
                if (lightWrapAround != null) lightWrapAround.floatValue = settings.lightWrapAround;
                if (receiveShadows != null) receiveShadows.floatValue = settings.receiveShadows;

                UpdateShaderKeywords(material);
                Debug.Log("Toon Shader settings pasted successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to paste settings: " + e.Message);
            }
        }

        #endregion
    }
    #endif
}