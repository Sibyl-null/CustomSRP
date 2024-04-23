using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Lighting
    {
        private const string BufferName = "Lighting";
        private static readonly int DirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
        private static readonly int DirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

        private readonly CommandBuffer _buffer = new() { name = BufferName };
        private CullingResults _cullingResults;

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
        {
            _cullingResults = cullingResults;
            
            _buffer.BeginSample(BufferName);
            {
                SetupLights();
            }
            _buffer.EndSample(BufferName);
            
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        private void SetupLights()
        {
            NativeArray<VisibleLight> lights = _cullingResults.visibleLights;
        }

        private void SetupDirectionalLight()
        {
            Light light = RenderSettings.sun;
            _buffer.SetGlobalVector(DirLightColorId, light.color.linear * light.intensity);
            _buffer.SetGlobalVector(DirLightDirectionId, -light.transform.forward);
        }
    }
}