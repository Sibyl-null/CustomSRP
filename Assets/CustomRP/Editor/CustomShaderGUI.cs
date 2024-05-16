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
            base.OnGUI(materialEditor, properties);
            _editor = materialEditor;
            _materials = materialEditor.targets.Cast<Material>().ToList();
            _properties = properties;

            EditorGUILayout.Space();
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
            if (_showPresets)
            {
                OpaquePreset();
                ClipPreset();
                FadePreset();
                TransparentPreset();
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
                PremultiplyAlpha = true;
                SrcBlend = BlendMode.One;
                DstBlend = BlendMode.OneMinusSrcAlpha;
                ZWrite = false;
                RenderQueue = RenderQueue.Transparent;
            }
        }
    }
}