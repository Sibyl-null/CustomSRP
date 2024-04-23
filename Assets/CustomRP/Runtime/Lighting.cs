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

        public void Setup(ScriptableRenderContext context)
        {
            _buffer.BeginSample(BufferName);
            {
                SetupDirectionalLight();
            }
            _buffer.EndSample(BufferName);
            
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        private void SetupDirectionalLight()
        {
            Light light = RenderSettings.sun;
            _buffer.SetGlobalVector(DirLightColorId, light.color.linear * light.intensity);
            _buffer.SetGlobalVector(DirLightDirectionId, -light.transform.forward);
        }
    }
}