using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
        }
        
        private const string BufferName = "Shadows";
        private const int MaxShadowedDirectionalLightCount = 1;
        
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ShadowSettings _shadowSettings;
        
        private readonly CommandBuffer _buffer = new() { name = BufferName };
        
        private int _shadowedDirectionalLightCount;
        private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights =
            new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            _context = context;
            _cullingResults = cullingResults;
            _shadowSettings = shadowSettings;

            _shadowedDirectionalLightCount = 0;
        }

        /** 尝试为指定方向光在阴影图集中保留空间，并存储渲染它们所需的信息 */
        public void ReserveDirectionalShadow(Light light, int visibleLightIndex)
        {
            if (_shadowedDirectionalLightCount >= MaxShadowedDirectionalLightCount)
                return;

            if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
                return;
            
            // 如果光源影响了至少一个阴影投射对象，则返回 true
            if (_cullingResults.GetShadowCasterBounds(visibleLightIndex, out _) == false)
                return;
            
            _shadowedDirectionalLights[_shadowedDirectionalLightCount++] = new ShadowedDirectionalLight()
            {
                visibleLightIndex = visibleLightIndex
            };
        }
        
        private void ExecuteBuffer() 
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
    }
}