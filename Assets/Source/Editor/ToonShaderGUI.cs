#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace MageLock.Graphics.Editor
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
        private MaterialProperty _mainTex;
        private MaterialProperty _color;
        private MaterialProperty _shadowThreshold;
        private MaterialProperty _shadowSmoothness;
        private MaterialProperty _shadowColor;
        private MaterialProperty _lightRampTex;
        private MaterialProperty _lightRampOffset;
        private MaterialProperty _posterizeLevels;
        private MaterialProperty _posterizePower;
        private MaterialProperty _useRimLight;
        private MaterialProperty _rimColor;
        private MaterialProperty _rimPower;
        private MaterialProperty _rimIntensity;
        private MaterialProperty _rimSmoothness;
        private MaterialProperty _useSpecular;
        private MaterialProperty _specularColor;
        private MaterialProperty _specularSize;
        private MaterialProperty _specularSmoothness;
        private MaterialProperty _specularSteps;
        private MaterialProperty _useOutline;
        private MaterialProperty _outlineColor;
        private MaterialProperty _outlineWidth;
        private MaterialProperty _outlineAdaptive;
        private MaterialProperty _useHalftone;
        private MaterialProperty _halftoneTex;
        private MaterialProperty _halftoneScale;
        private MaterialProperty _halftoneThreshold;
        private MaterialProperty _halftoneSmoothness;
        private MaterialProperty _indirectLightStrength;
        private MaterialProperty _lightWrapAround;
        private MaterialProperty _receiveShadows;

        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _presetButtonStyle;
        private GUIStyle _randomizeButtonStyle;
        private GUIStyle _toggleButtonStyle;

        private readonly Color _accentColor = new(0.3f, 0.7f, 1f);
        private readonly Color _warningColor = new(1f, 0.6f, 0.2f);
        private readonly Color _successColor = new(0.3f, 0.8f, 0.3f);
        private readonly Color _errorColor = new(1f, 0.3f, 0.3f);

        private static bool _showMainSettings = true;
        private static bool _showShadingSettings = true;
        private static bool _showRimSettings = true;
        private static bool _showSpecularSettings = true;
        private static bool _showOutlineSettings = true;
        private static bool _showHalftoneSettings;
        private static bool _showAdvancedSettings;
        private static bool _showPresets = true;
        private bool _stylesInitialized;
        
        public ToonShaderGUI() { }
        
        public ToonShaderGUI(MaterialProperty specularSize)
        {
            _specularSize = specularSize;
        }

        public ToonShaderGUI(MaterialProperty halftoneScale, MaterialProperty specularSize)
        {
            _halftoneScale = halftoneScale;
            _specularSize = specularSize;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            if (!_stylesInitialized)
            {
                SetupStyles();
                _stylesInitialized = true;
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
            _mainTex = FindProperty("_MainTex", props, false);
            _color = FindProperty("_Color", props, false);
            _shadowThreshold = FindProperty("_ShadowThreshold", props, false);
            _shadowSmoothness = FindProperty("_ShadowSmoothness", props, false);
            _shadowColor = FindProperty("_ShadowColor", props, false);
            _lightRampTex = FindProperty("_LightRampTex", props, false);
            _lightRampOffset = FindProperty("_RampOffset", props, false);
            _posterizeLevels = FindProperty("_PosterizeLevels", props, false);
            _posterizePower = FindProperty("_PosterizePower", props, false);
            _useRimLight = FindProperty("_UseRimLight", props, false);
            _rimColor = FindProperty("_RimColor", props, false);
            _rimPower = FindProperty("_RimPower", props, false);
            _rimIntensity = FindProperty("_RimIntensity", props, false);
            _rimSmoothness = FindProperty("_RimSmoothness", props, false);
            _useSpecular = FindProperty("_UseSpecular", props, false);
            _specularColor = FindProperty("_SpecularColor", props, false);
            _specularSize = FindProperty("_SpecularSize", props, false);
            _specularSmoothness = FindProperty("_SpecularSmoothness", props, false);
            _specularSteps = FindProperty("_SpecularSteps", props, false);
            _useOutline = FindProperty("_UseOutline", props, false);
            _outlineColor = FindProperty("_OutlineColor", props, false);
            _outlineWidth = FindProperty("_OutlineWidth", props, false);
            _outlineAdaptive = FindProperty("_OutlineAdaptive", props, false);
            _useHalftone = FindProperty("_UseHalftone", props, false);
            _halftoneTex = FindProperty("_HalftoneTex", props, false);
            _halftoneScale = FindProperty("_HalftoneScale", props, false);
            _halftoneThreshold = FindProperty("_HalftoneThreshold", props, false);
            _halftoneSmoothness = FindProperty("_HalftoneSmoothness", props, false);
            _indirectLightStrength = FindProperty("_IndirectLightStrength", props, false);
            _lightWrapAround = FindProperty("_LightWrapAround", props, false);
            _receiveShadows = FindProperty("_ReceiveShadows", props, false);
        }

        void SetupStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel);
            _headerStyle.fontSize = 18;
            _headerStyle.normal.textColor = _accentColor;
            _headerStyle.alignment = TextAnchor.MiddleCenter;

            _sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            _sectionStyle.fontSize = 12;
            _sectionStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

            _presetButtonStyle = new GUIStyle(GUI.skin.button);
            _presetButtonStyle.fontSize = 10;
            _presetButtonStyle.fontStyle = FontStyle.Bold;
            _presetButtonStyle.fixedHeight = 25;

            _randomizeButtonStyle = new GUIStyle(GUI.skin.button);
            _randomizeButtonStyle.fontSize = 9;
            _randomizeButtonStyle.fontStyle = FontStyle.Bold;
            _randomizeButtonStyle.fixedHeight = 18;
            _randomizeButtonStyle.fixedWidth = 65;

            _toggleButtonStyle = new GUIStyle(GUI.skin.button);
            _toggleButtonStyle.fontSize = 10;
            _toggleButtonStyle.fixedHeight = 20;
        }

        void DrawHeader()
        {
            EditorGUILayout.Space(5);

            var headerRect = GUILayoutUtility.GetRect(0, 35, GUILayout.ExpandWidth(true));
            var originalColor = GUI.color;

            GUI.color = new Color(_accentColor.r, _accentColor.g, _accentColor.b, 0.2f);
            GUI.DrawTexture(headerRect, EditorGUIUtility.whiteTexture);
            GUI.color = originalColor;

            GUI.Label(headerRect, "🎨 TOON SHADER", _headerStyle);

            EditorGUILayout.Space(5);
            DrawSeparator();
        }

        void DrawPresetSection(Material material)
        {
            DrawSectionHeader("🎯 Style Presets", ref _showPresets, _successColor);

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
            DrawSectionHeader("🎨 Main Color", ref _showMainSettings, _accentColor);

            if (_showMainSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (_mainTex != null)
                    {
                        materialEditor.ShaderProperty(_mainTex, "Main Texture");
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (_color != null)
                        {
                            materialEditor.ShaderProperty(_color, "Base Color");
                        }

                        DrawRandomizeButton(() =>
                        {
                            if (_color != null)
                            {
                                _color.colorValue = Random.ColorHSV(0f, 1f, 0.4f, 1f, 0.6f, 1f);
                            }
                        }, _warningColor);
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
                        EditorGUILayout.LabelField("Shadow Settings", _sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(() => RandomizeShadingSettings(), new Color(1f, 0.7f, 0.3f));
                    }

                    if (_shadowThreshold != null)
                    {
                        materialEditor.ShaderProperty(_shadowThreshold, "Shadow Threshold");
                    }
                    if (_shadowSmoothness != null)
                    {
                        materialEditor.ShaderProperty(_shadowSmoothness, "Shadow Smoothness");
                    }
                    if (_shadowColor != null)
                    {
                        materialEditor.ShaderProperty(_shadowColor, "Shadow Color");
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Ramp Shading", _sectionStyle);
                    if (_lightRampTex != null)
                    {
                        materialEditor.ShaderProperty(_lightRampTex, "Light Ramp Texture");
                    }
                    if (_lightRampOffset != null)
                    {
                        materialEditor.ShaderProperty(_lightRampOffset, "Light Ramp Offset");
                    }

                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Posterize Shading", _sectionStyle);
                    if (_posterizeLevels != null)
                    {
                        materialEditor.ShaderProperty(_posterizeLevels, "Posterize Levels");
                    }
                    if (_posterizePower != null)
                    {
                        materialEditor.ShaderProperty(_posterizePower, "Posterize Power");
                    }
                }
            }
        }

        void DrawRimLightSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("✨ Rim Lighting", ref _showRimSettings, new Color(0.4f, 0.8f, 1f), _useRimLight);

            if (_showRimSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Rim Light Settings", _sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(() => RandomizeRimSettings(), new Color(0.3f, 0.8f, 1f));
                    }

                    bool useRim = _useRimLight != null && _useRimLight.floatValue > 0.5f;
                    using (new EditorGUI.DisabledScope(!useRim))
                    {
                        if (_rimColor != null)
                        {
                            materialEditor.ShaderProperty(_rimColor, "Rim Color");
                        }
                        if (_rimPower != null)
                        {
                            materialEditor.ShaderProperty(_rimPower, "Rim Power");
                        }
                        if (_rimIntensity != null)
                        {
                            materialEditor.ShaderProperty(_rimIntensity, "Rim Intensity");
                        }
                        if (_rimSmoothness != null)
                        {
                            materialEditor.ShaderProperty(_rimSmoothness, "Rim Smoothness");
                        }
                    }
                }
            }
        }

        void DrawSpecularSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("💎 Specular", ref _showSpecularSettings, new Color(1f, 1f, 0.4f), _useSpecular);

            if (_showSpecularSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Specular Settings", _sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(RandomizeSpecularSettings, new Color(1f, 1f, 0.3f));
                    }

                    bool useSpec = _useSpecular != null && _useSpecular.floatValue > 0.5f;
                    using (new EditorGUI.DisabledScope(!useSpec))
                    {
                        if (_specularColor != null)
                        {
                            materialEditor.ShaderProperty(_specularColor, "Specular Color");
                        }
                        if (_specularSize != null)
                        {
                            materialEditor.ShaderProperty(_specularSize, "Specular Size");
                        }
                        if (_specularSmoothness != null)
                        {
                            materialEditor.ShaderProperty(_specularSmoothness, "Specular Smoothness");
                        }
                        if (_specularSteps != null)
                        {
                            materialEditor.ShaderProperty(_specularSteps, "Specular Steps");
                        }
                    }
                }
            }
        }

        void DrawOutlineSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("🖼️ Outline", ref _showOutlineSettings, new Color(0.8f, 0.4f, 1f), _useOutline);

            if (_showOutlineSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Outline Settings", _sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(RandomizeOutlineSettings, new Color(0.8f, 0.3f, 1f));
                    }

                    bool useOutline = this._useOutline != null && this._useOutline.floatValue > 0.5f;
                    using (new EditorGUI.DisabledScope(!useOutline))
                    {
                        if (_outlineColor != null)
                        {
                            materialEditor.ShaderProperty(_outlineColor, "Outline Color");
                        }
                        if (_outlineWidth != null)
                        {
                            materialEditor.ShaderProperty(_outlineWidth, "Outline Width");
                        }
                        if (_outlineAdaptive != null)
                        {
                            materialEditor.ShaderProperty(_outlineAdaptive, "Outline Adaptive");
                        }
                    }
                }
            }
        }

        void DrawHalftoneSection(MaterialEditor materialEditor)
        {
            DrawSectionHeader("🔲 Halftone", ref _showHalftoneSettings, new Color(1f, 0.6f, 0.2f), _useHalftone);

            if (_showHalftoneSettings)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Halftone Settings", _sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(() => RandomizeHalftoneSettings(), new Color(1f, 0.5f, 0.1f));
                    }

                    bool useHalftone = this._useHalftone != null && this._useHalftone.floatValue > 0.5f;
                    using (new EditorGUI.DisabledScope(!useHalftone))
                    {
                        if (_halftoneTex != null)
                        {
                            materialEditor.ShaderProperty(_halftoneTex, "Halftone Texture");
                        }
                        if (_halftoneScale != null)
                        {
                            materialEditor.ShaderProperty(_halftoneScale, "Halftone Scale");
                        }
                        if (_halftoneThreshold != null)
                        {
                            materialEditor.ShaderProperty(_halftoneThreshold, "Halftone Threshold");
                        }
                        if (_halftoneSmoothness != null)
                        {
                            materialEditor.ShaderProperty(_halftoneSmoothness, "Halftone Smoothness");
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
                        EditorGUILayout.LabelField("Advanced Settings", _sectionStyle, GUILayout.Width(120));
                        GUILayout.FlexibleSpace();
                        DrawRandomizeButton(RandomizeAdvancedSettings, new Color(0.5f, 0.9f, 0.5f));
                    }

                    if (_indirectLightStrength != null)
                    {
                        materialEditor.ShaderProperty(_indirectLightStrength, "Indirect Light Strength");
                    }
                    if (_lightWrapAround != null)
                    {
                        materialEditor.ShaderProperty(_lightWrapAround, "Light Wrap Around");
                    }
                    if (_receiveShadows != null)
                    {
                        materialEditor.ShaderProperty(_receiveShadows, "Receive Shadows");
                    }
                }
            }
        }

        void DrawUtilityButtons(Material material)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("🛠️ Utilities", _sectionStyle);
                EditorGUILayout.Space(3);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var randomAllColor = GUI.backgroundColor;
                    GUI.backgroundColor = _errorColor;
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
                GUI.backgroundColor = isOn ? _successColor : new Color(0.5f, 0.5f, 0.5f);

                if (GUI.Button(toggleRect, isOn ? "ON" : "OFF", _toggleButtonStyle))
                {
                    toggleProp.floatValue = isOn ? 0f : 1f;
                }

                GUI.backgroundColor = oldColor;
            }
        }

        void DrawPresetButton(string label, System.Action action)
        {
            if (GUILayout.Button(label, _presetButtonStyle))
            {
                action.Invoke();
            }
        }

        void DrawRandomizeButton(System.Action action, Color color)
        {
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            if (GUILayout.Button("🎲", _randomizeButtonStyle))
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
            if (_useRimLight != null)
            {
                SetKeyword(material, "_USERIMLIGHT_ON", _useRimLight.floatValue > 0.5f);
            }
            if (_useSpecular != null)
            {
                SetKeyword(material, "_USESPECULAR_ON", _useSpecular.floatValue > 0.5f);
            }
            if (_useOutline != null)
            {
                SetKeyword(material, "_USEOUTLINE_ON", _useOutline.floatValue > 0.5f);
            }
            if (_useHalftone != null)
            {
                SetKeyword(material, "_USEHALFTONE_ON", _useHalftone.floatValue > 0.5f);
            }
            if (_receiveShadows != null)
            {
                SetKeyword(material, "_RECEIVESHADOWS_ON", _receiveShadows.floatValue > 0.5f);
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
            if (_color != null) { _color.colorValue = new Color(0.95f, 0.9f, 0.85f); }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.6f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.15f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.4f, 0.4f, 0.6f); }
            if (_useRimLight != null) { _useRimLight.floatValue = 1f; }
            if (_rimColor != null) { _rimColor.colorValue = Color.cyan; }
            if (_rimPower != null) { _rimPower.floatValue = 3f; }
            if (_rimIntensity != null) { _rimIntensity.floatValue = 2f; }
            if (_useOutline != null) { _useOutline.floatValue = 1f; }
            if (_outlineWidth != null) { _outlineWidth.floatValue = 0.008f; }
            if (_outlineColor != null) { _outlineColor.colorValue = Color.black; }
            UpdateShaderKeywords(material);
        }

        void ApplyComicPreset(Material material)
        {
            if (_color != null) { _color.colorValue = new Color(0.8f, 0.8f, 0.9f); }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.4f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.05f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.2f, 0.2f, 0.4f); }
            if (_useRimLight != null) { _useRimLight.floatValue = 1f; }
            if (_rimColor != null) { _rimColor.colorValue = Color.white; }
            if (_rimPower != null) { _rimPower.floatValue = 4f; }
            if (_rimIntensity != null) { _rimIntensity.floatValue = 1.5f; }
            if (_useSpecular != null) { _useSpecular.floatValue = 1f; }
            if (_specularSteps != null) { _specularSteps.floatValue = 3f; }
            if (_useOutline != null) { _useOutline.floatValue = 1f; }
            if (_outlineWidth != null) { _outlineWidth.floatValue = 0.012f; }
            UpdateShaderKeywords(material);
        }

        void ApplyGamePreset(Material material)
        {
            if (_color != null) { _color.colorValue = new Color(0.8f, 0.9f, 0.7f); }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.5f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.2f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.3f, 0.3f, 0.5f); }
            if (_useRimLight != null) { _useRimLight.floatValue = 1f; }
            if (_rimColor != null) { _rimColor.colorValue = Color.yellow; }
            if (_rimPower != null) { _rimPower.floatValue = 2f; }
            if (_rimIntensity != null) { _rimIntensity.floatValue = 1.2f; }
            if (_useOutline != null) { _useOutline.floatValue = 1f; }
            if (_outlineWidth != null) { _outlineWidth.floatValue = 0.006f; }
            if (_lightWrapAround != null) { _lightWrapAround.floatValue = 0.3f; }
            UpdateShaderKeywords(material);
        }

        void ApplyRetroPreset(Material material)
        {
            if (_color != null) { _color.colorValue = new Color(0.9f, 0.7f, 0.8f); }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.3f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.02f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.2f, 0.1f, 0.3f); }
            if (_useRimLight != null) { _useRimLight.floatValue = 0f; }
            if (_useSpecular != null) { _useSpecular.floatValue = 1f; }
            if (_specularSteps != null) { _specularSteps.floatValue = 4f; }
            if (_useOutline != null) { _useOutline.floatValue = 1f; }
            if (_outlineWidth != null) { _outlineWidth.floatValue = 0.015f; }
            UpdateShaderKeywords(material);
        }

        void ApplySoftPreset(Material material)
        {
            if (_color != null) { _color.colorValue = new Color(0.9f, 0.85f, 0.8f); }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.7f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.3f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.5f, 0.4f, 0.6f); }
            if (_useRimLight != null) { _useRimLight.floatValue = 1f; }
            if (_rimColor != null) { _rimColor.colorValue = new Color(1f, 0.8f, 0.6f); }
            if (_rimPower != null) { _rimPower.floatValue = 1.5f; }
            if (_rimIntensity != null) { _rimIntensity.floatValue = 0.8f; }
            if (_rimSmoothness != null) { _rimSmoothness.floatValue = 0.8f; }
            if (_useOutline != null) { _useOutline.floatValue = 0f; }
            if (_lightWrapAround != null) { _lightWrapAround.floatValue = 0.5f; }
            UpdateShaderKeywords(material);
        }

        void ApplyNeonPreset(Material material)
        {
            if (_color != null) { _color.colorValue = new Color(0.1f, 0.1f, 0.2f); }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.2f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.1f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.05f, 0.05f, 0.1f); }
            if (_useRimLight != null) { _useRimLight.floatValue = 1f; }
            if (_rimColor != null) { _rimColor.colorValue = new Color(0f, 1f, 1f); }
            if (_rimPower != null) { _rimPower.floatValue = 2f; }
            if (_rimIntensity != null) { _rimIntensity.floatValue = 3f; }
            if (_useSpecular != null) { _useSpecular.floatValue = 1f; }
            if (_specularColor != null) { _specularColor.colorValue = new Color(1f, 0f, 1f); }
            if (_useOutline != null) { _useOutline.floatValue = 1f; }
            if (_outlineColor != null) { _outlineColor.colorValue = new Color(0f, 0.5f, 1f); }
            if (_outlineWidth != null) { _outlineWidth.floatValue = 0.01f; }
            UpdateShaderKeywords(material);
        }

        void ApplyMoodyPreset(Material material)
        {
            if (_color != null) { _color.colorValue = new Color(0.4f, 0.3f, 0.5f); }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.8f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.25f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.1f, 0.1f, 0.2f); }
            if (_useRimLight != null) { _useRimLight.floatValue = 1f; }
            if (_rimColor != null) { _rimColor.colorValue = new Color(0.8f, 0.4f, 0.2f); }
            if (_rimPower != null) { _rimPower.floatValue = 4f; }
            if (_rimIntensity != null) { _rimIntensity.floatValue = 1f; }
            if (_indirectLightStrength != null) { _indirectLightStrength.floatValue = 0.1f; }
            if (_useOutline != null) { _useOutline.floatValue = 1f; }
            if (_outlineColor != null) { _outlineColor.colorValue = new Color(0.1f, 0.1f, 0.1f); }
            UpdateShaderKeywords(material);
        }

        void ApplyBrightPreset(Material material)
        {
            if (_color != null) { _color.colorValue = Color.white; }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.3f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.2f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.7f, 0.7f, 0.8f); }
            if (_useRimLight != null) { _useRimLight.floatValue = 1f; }
            if (_rimColor != null) { _rimColor.colorValue = new Color(1f, 1f, 0.8f); }
            if (_rimPower != null) { _rimPower.floatValue = 1f; }
            if (_rimIntensity != null) { _rimIntensity.floatValue = 1f; }
            if (_indirectLightStrength != null) { _indirectLightStrength.floatValue = 0.8f; }
            if (_lightWrapAround != null) { _lightWrapAround.floatValue = 0.4f; }
            if (_useOutline != null) { _useOutline.floatValue = 0f; }
            UpdateShaderKeywords(material);
        }

        #endregion

        #region Randomization Methods

        void RandomizeShadingSettings()
        {
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = Random.Range(0.2f, 0.8f); }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = Random.Range(0.05f, 0.3f); }
            if (_shadowColor != null) { _shadowColor.colorValue = Random.ColorHSV(0f, 1f, 0.3f, 0.7f, 0.2f, 0.8f); }
            if (_lightRampOffset != null) { _lightRampOffset.floatValue = Random.Range(-0.5f, 0.5f); }
            if (_posterizeLevels != null) { _posterizeLevels.floatValue = Random.Range(2f, 8f); }
            if (_posterizePower != null) { _posterizePower.floatValue = Random.Range(0.5f, 2.5f); }
        }

        void RandomizeRimSettings()
        {
            if (_rimColor != null) { _rimColor.colorValue = Random.ColorHSV(0f, 1f, 0.7f, 1f, 0.8f, 1f); }
            if (_rimPower != null) { _rimPower.floatValue = Random.Range(1f, 8f); }
            if (_rimIntensity != null) { _rimIntensity.floatValue = Random.Range(0.5f, 3f); }
            if (_rimSmoothness != null) { _rimSmoothness.floatValue = Random.Range(0.1f, 0.8f); }
        }

        void RandomizeSpecularSettings()
        {
            if (_specularColor != null) { _specularColor.colorValue = Random.ColorHSV(0f, 1f, 0.8f, 1f, 0.9f, 1f); }
            if (_specularSize != null) { _specularSize.floatValue = Random.Range(0.05f, 0.4f); }
            if (_specularSmoothness != null) { _specularSmoothness.floatValue = Random.Range(0.1f, 0.9f); }
            if (_specularSteps != null) { _specularSteps.floatValue = Random.Range(1f, 5f); }
        }

        void RandomizeOutlineSettings()
        {
            if (_outlineColor != null) { _outlineColor.colorValue = Random.ColorHSV(0f, 1f, 0f, 0.3f, 0f, 0.5f); }
            if (_outlineWidth != null) { _outlineWidth.floatValue = Random.Range(0.002f, 0.015f); }
            if (_outlineAdaptive != null) { _outlineAdaptive.floatValue = Random.Range(0f, 0.8f); }
        }

        void RandomizeHalftoneSettings()
        {
            if (_halftoneScale != null) { _halftoneScale.floatValue = Random.Range(5f, 20f); }
            if (_halftoneThreshold != null) { _halftoneThreshold.floatValue = Random.Range(0.2f, 0.8f); }
            if (_halftoneSmoothness != null) { _halftoneSmoothness.floatValue = Random.Range(0.05f, 0.3f); }
        }

        void RandomizeAdvancedSettings()
        {
            if (_indirectLightStrength != null) { _indirectLightStrength.floatValue = Random.Range(0.1f, 0.8f); }
            if (_lightWrapAround != null) { _lightWrapAround.floatValue = Random.Range(0f, 0.6f); }
            if (_receiveShadows != null) { _receiveShadows.floatValue = Random.value > 0.3f ? 1f : 0f; }
        }

        void RandomizeAllSettings()
        {
            if (_color != null) { _color.colorValue = Random.ColorHSV(0f, 1f, 0.4f, 1f, 0.6f, 1f); }

            if (_useRimLight != null) { _useRimLight.floatValue = Random.value > 0.3f ? 1f : 0f; }
            if (_useSpecular != null) { _useSpecular.floatValue = Random.value > 0.4f ? 1f : 0f; }
            if (_useOutline != null) { _useOutline.floatValue = Random.value > 0.2f ? 1f : 0f; }
            if (_useHalftone != null) { _useHalftone.floatValue = Random.value > 0.7f ? 1f : 0f; }

            RandomizeShadingSettings();
            RandomizeRimSettings();
            RandomizeSpecularSettings();
            RandomizeOutlineSettings();
            RandomizeHalftoneSettings();
            RandomizeAdvancedSettings();
        }

        void ResetToDefault(Material material)
        {
            if (_color != null) { _color.colorValue = Color.white; }
            if (_shadowThreshold != null) { _shadowThreshold.floatValue = 0.5f; }
            if (_shadowSmoothness != null) { _shadowSmoothness.floatValue = 0.1f; }
            if (_shadowColor != null) { _shadowColor.colorValue = new Color(0.3f, 0.3f, 0.3f, 1f); }
            if (_lightRampOffset != null) { _lightRampOffset.floatValue = 0f; }
            if (_posterizeLevels != null) { _posterizeLevels.floatValue = 3f; }
            if (_posterizePower != null) { _posterizePower.floatValue = 1f; }
            if (_useRimLight != null) { _useRimLight.floatValue = 1f; }
            if (_rimColor != null) { _rimColor.colorValue = Color.white; }
            if (_rimPower != null) { _rimPower.floatValue = 2f; }
            if (_rimIntensity != null) { _rimIntensity.floatValue = 1f; }
            if (_rimSmoothness != null) { _rimSmoothness.floatValue = 0.5f; }
            if (_useSpecular != null) { _useSpecular.floatValue = 1f; }
            if (_specularColor != null) { _specularColor.colorValue = Color.white; }
            if (_specularSize != null) { _specularSize.floatValue = 0.1f; }
            if (_specularSmoothness != null) { _specularSmoothness.floatValue = 0.5f; }
            if (_specularSteps != null) { _specularSteps.floatValue = 1f; }
            if (_useOutline != null) { _useOutline.floatValue = 1f; }
            if (_outlineColor != null) { _outlineColor.colorValue = Color.black; }
            if (_outlineWidth != null) { _outlineWidth.floatValue = 0.005f; }
            if (_outlineAdaptive != null) { _outlineAdaptive.floatValue = 0f; }
            if (_useHalftone != null) { _useHalftone.floatValue = 0f; }
            if (_halftoneScale != null) { _halftoneScale.floatValue = 10f; }
            if (_halftoneThreshold != null) { _halftoneThreshold.floatValue = 0.5f; }
            if (_halftoneSmoothness != null) { _halftoneSmoothness.floatValue = 0.1f; }
            if (_indirectLightStrength != null) { _indirectLightStrength.floatValue = 0.3f; }
            if (_lightWrapAround != null) { _lightWrapAround.floatValue = 0f; }
            if (_receiveShadows != null) { _receiveShadows.floatValue = 1f; }

            UpdateShaderKeywords(material);
        }

        #endregion

        #region Utility Methods

        void CopySettings(Material material)
        {
            var settings = new ToonShaderSettings();

            if (_color != null) settings.color = _color.colorValue;
            if (_shadowThreshold != null) settings.shadowThreshold = _shadowThreshold.floatValue;
            if (_shadowSmoothness != null) settings.shadowSmoothness = _shadowSmoothness.floatValue;
            if (_shadowColor != null) settings.shadowColor = _shadowColor.colorValue;
            if (_lightRampOffset != null) settings.lightRampOffset = _lightRampOffset.floatValue;
            if (_posterizeLevels != null) settings.posterizeLevels = _posterizeLevels.floatValue;
            if (_posterizePower != null) settings.posterizePower = _posterizePower.floatValue;

            if (_useRimLight != null) settings.useRimLight = _useRimLight.floatValue;
            if (_rimColor != null) settings.rimColor = _rimColor.colorValue;
            if (_rimPower != null) settings.rimPower = _rimPower.floatValue;
            if (_rimIntensity != null) settings.rimIntensity = _rimIntensity.floatValue;
            if (_rimSmoothness != null) settings.rimSmoothness = _rimSmoothness.floatValue;

            if (_useSpecular != null) settings.useSpecular = _useSpecular.floatValue;
            if (_specularColor != null) settings.specularColor = _specularColor.colorValue;
            if (_specularSize != null) settings.specularSize = _specularSize.floatValue;
            if (_specularSmoothness != null) settings.specularSmoothness = _specularSmoothness.floatValue;
            if (_specularSteps != null) settings.specularSteps = _specularSteps.floatValue;

            if (_useOutline != null) settings.useOutline = _useOutline.floatValue;
            if (_outlineColor != null) settings.outlineColor = _outlineColor.colorValue;
            if (_outlineWidth != null) settings.outlineWidth = _outlineWidth.floatValue;
            if (_outlineAdaptive != null) settings.outlineAdaptive = _outlineAdaptive.floatValue;

            if (_useHalftone != null) settings.useHalftone = _useHalftone.floatValue;
            if (_halftoneScale != null) settings.halftoneScale = _halftoneScale.floatValue;
            if (_halftoneThreshold != null) settings.halftoneThreshold = _halftoneThreshold.floatValue;
            if (_halftoneSmoothness != null) settings.halftoneSmoothness = _halftoneSmoothness.floatValue;

            if (_indirectLightStrength != null) settings.indirectLightStrength = _indirectLightStrength.floatValue;
            if (_lightWrapAround != null) settings.lightWrapAround = _lightWrapAround.floatValue;
            if (_receiveShadows != null) settings.receiveShadows = _receiveShadows.floatValue;

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

                if (_color != null) _color.colorValue = settings.color;
                if (_shadowThreshold != null) _shadowThreshold.floatValue = settings.shadowThreshold;
                if (_shadowSmoothness != null) _shadowSmoothness.floatValue = settings.shadowSmoothness;
                if (_shadowColor != null) _shadowColor.colorValue = settings.shadowColor;
                if (_lightRampOffset != null) _lightRampOffset.floatValue = settings.lightRampOffset;
                if (_posterizeLevels != null) _posterizeLevels.floatValue = settings.posterizeLevels;
                if (_posterizePower != null) _posterizePower.floatValue = settings.posterizePower;

                if (_useRimLight != null) _useRimLight.floatValue = settings.useRimLight;
                if (_rimColor != null) _rimColor.colorValue = settings.rimColor;
                if (_rimPower != null) _rimPower.floatValue = settings.rimPower;
                if (_rimIntensity != null) _rimIntensity.floatValue = settings.rimIntensity;
                if (_rimSmoothness != null) _rimSmoothness.floatValue = settings.rimSmoothness;

                if (_useSpecular != null) _useSpecular.floatValue = settings.useSpecular;
                if (_specularColor != null) _specularColor.colorValue = settings.specularColor;
                if (_specularSize != null) _specularSize.floatValue = settings.specularSize;
                if (_specularSmoothness != null) _specularSmoothness.floatValue = settings.specularSmoothness;
                if (_specularSteps != null) _specularSteps.floatValue = settings.specularSteps;

                if (_useOutline != null) _useOutline.floatValue = settings.useOutline;
                if (_outlineColor != null) _outlineColor.colorValue = settings.outlineColor;
                if (_outlineWidth != null) _outlineWidth.floatValue = settings.outlineWidth;
                if (_outlineAdaptive != null) _outlineAdaptive.floatValue = settings.outlineAdaptive;

                if (_useHalftone != null) _useHalftone.floatValue = settings.useHalftone;
                if (_halftoneScale != null) _halftoneScale.floatValue = settings.halftoneScale;
                if (_halftoneThreshold != null) _halftoneThreshold.floatValue = settings.halftoneThreshold;
                if (_halftoneSmoothness != null) _halftoneSmoothness.floatValue = settings.halftoneSmoothness;

                if (_indirectLightStrength != null) _indirectLightStrength.floatValue = settings.indirectLightStrength;
                if (_lightWrapAround != null) _lightWrapAround.floatValue = settings.lightWrapAround;
                if (_receiveShadows != null) _receiveShadows.floatValue = settings.receiveShadows;

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