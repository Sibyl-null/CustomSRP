using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Editor
{
    public class CustomShaderGUI : ShaderGUI
    {
        private enum ShadowMode
        {
            On, Clip, Dither, Off
        }
        
        private MaterialEditor _editor;
        private List<Material> _materials;
        private MaterialProperty[] _properties;

        private bool _showPresets;

        private bool Clipping
        {
            set => SetProperty("_Clipping", "_CLIPPING", value);
        }

        private bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

        private bool PremultiplyAlpha
        {
            set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value);
        }

        private BlendMode SrcBlend
        {
            set => SetProperty("_SrcBlend", (float)value);
        }

        private BlendMode DstBlend
        {
            set => SetProperty("_DstBlend", (float)value);
        }

        private bool ZWrite
        {
            set => SetProperty("_ZWrite", value ? 1 : 0);
        }

        private RenderQueue RenderQueue
        {
            set => _materials.ForEach(m => m.renderQueue = (int)value);
        }

        private ShadowMode Shadows
        {
            set
            {
                if (SetProperty("_Shadows", (float)value))
                {
                    SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                    SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
                }
            }
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            EditorGUI.BeginChangeCheck();
            
            base.OnGUI(materialEditor, properties);
            _editor = materialEditor;
            _materials = materialEditor.targets.Cast<Material>().ToList();
            _properties = properties;

            BakedEmission();

            EditorGUILayout.Space();
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
            if (_showPresets)
            {
                OpaquePreset();
                ClipPreset();
                FadePreset();
                TransparentPreset();
            }

            if (EditorGUI.EndChangeCheck())
            {
                SetShadowCasterPass();
                CopyLightMappingProperties();
            }
        }

        private void BakedEmission()
        {
            EditorGUI.BeginChangeCheck();
            _editor.LightmapEmissionProperty();
            if (EditorGUI.EndChangeCheck())
            {
                _materials.ForEach(m => m.globalIlluminationFlags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack);
            }
        }

        private void CopyLightMappingProperties()
        {
            MaterialProperty mainTex = FindProperty("_MainTex", _properties, false);
            MaterialProperty baseMap = FindProperty("_BaseMap", _properties, false);
            if (mainTex != null && baseMap != null)
            {
                mainTex.textureValue = baseMap.textureValue;
                mainTex.textureScaleAndOffset = baseMap.textureScaleAndOffset;
            }

            MaterialProperty color = FindProperty("_Color", _properties, false);
            MaterialProperty baseColor = FindProperty("_BaseColor", _properties, false);
            if (color != null && baseColor != null)
            {
                color.colorValue = baseColor.colorValue;
            }
        }

        private bool SetProperty(string name, float value)
        {
            MaterialProperty property = FindProperty(name, _properties, false);
            if (property != null)
            {
                property.floatValue = value;
                return true;
            }

            return false;
        }

        private void SetKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                _materials.ForEach(m => m.EnableKeyword(keyword));
            }
            else
            {
                _materials.ForEach(m => m.DisableKeyword(keyword));
            }
        }

        private void SetProperty(string name, string keyword, bool value)
        {
            if (SetProperty(name, value ? 1 : 0))
                SetKeyword(keyword, value);
        }
        
        private bool HasProperty(string name)
        {
            return FindProperty(name, _properties, false) != null;
        }

        private void SetShadowCasterPass()
        {
            MaterialProperty shadows = FindProperty("_Shadows", _properties, false);
            // hasMixedValue 表示同时选中的多个材质的属性值不一致
            if (shadows == null || shadows.hasMixedValue)
                return;

            bool enabled = shadows.floatValue < (float)ShadowMode.Off;
            foreach (Material material in _materials)
            {
                // 在材质级别上启用或禁用着色器通道
                material.SetShaderPassEnabled("ShadowCaster", enabled);
            }
        }


        // ----------------------------------------------------------------------
        // Preset Buttons
        // ----------------------------------------------------------------------

        private bool PresetButton(string name)
        {
            if (GUILayout.Button(name))
            {
                _editor.RegisterPropertyChangeUndo(name);
                return true;
            }

            return false;
        }

        /** 标准不透明物体 */
        private void OpaquePreset()
        {
            if (PresetButton("Opaque"))
            {
                Clipping = false;
                Shadows = ShadowMode.On;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.Zero;
                ZWrite = true;
                RenderQueue = RenderQueue.Geometry;
            }
        }

        /** 使用 AlphaClip 的不透明物体 */
        private void ClipPreset()
        {
            if (PresetButton("Clip"))
            {
                Clipping = true;
                Shadows = ShadowMode.Clip;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.Zero;
                ZWrite = true;
                RenderQueue = RenderQueue.AlphaTest;
            }
        }

        /** 标准半透明物体，但光照不正确 */
        private void FadePreset()
        {
            if (PresetButton("Fade"))
            {
                Clipping = false;
                Shadows = ShadowMode.Dither;
                PremultiplyAlpha = false;
                SrcBlend = BlendMode.SrcAlpha;
                DstBlend = BlendMode.OneMinusSrcAlpha;
                ZWrite = false;
                RenderQueue = RenderQueue.Transparent;
            }
        }

        /** 具有正确光照的半透明物体 */
        private void TransparentPreset()
        {
            if (HasPremultiplyAlpha && PresetButton("Transparent"))
            {
                Clipping = false;
                Shadows = ShadowMode.Dither;
                PremultiplyAlpha = true;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.OneMinusSrcAlpha;
                ZWrite = false;
                RenderQueue = RenderQueue.Transparent;
            }
        }
    }
}