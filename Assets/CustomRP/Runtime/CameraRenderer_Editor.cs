using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public partial class CameraRenderer
    {
        partial void DrawUnsupportedShaders();
        partial void DrawGizmos();
        
#if UNITY_EDITOR
        private static readonly Material ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        private static readonly ShaderTagId[] LegacyShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };
        
        partial void DrawUnsupportedShaders()
        {
            DrawingSettings drawingSettings = new DrawingSettings(LegacyShaderTagIds[0], new SortingSettings(_camera))
            {
                overrideMaterial = ErrorMaterial
            };
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            for (int i = 1; i < LegacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, LegacyShaderTagIds[i]);
            }

            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        partial void DrawGizmos()
        {
            if (UnityEditor.Handles.ShouldRenderGizmos())
            {
                _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
                _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
            }
        }
#endif
    }
}