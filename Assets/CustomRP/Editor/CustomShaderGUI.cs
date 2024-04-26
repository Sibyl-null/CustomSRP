using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Editor
{
    public class CustomShaderGUI : ShaderGUI
    {
        private MaterialEditor _editor;
        private List<Material> _materials;
        private MaterialProperty[] _properties;

        private bool Clipping
        {
            set => SetProperty("_Clipping", "_CLIPPING", value);
        }

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
         
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);
            _editor = materialEditor;
            _materials = materialEditor.targets.Cast<Material>().ToList();
            _properties = properties;
        }

        private void SetProperty(string name, float value)
        {
            FindProperty(name, _properties).floatValue = value;
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
            SetProperty(name, value ? 1 : 0);
            SetKeyword(keyword, value);
        }
    }
}