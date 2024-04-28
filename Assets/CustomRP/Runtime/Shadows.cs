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
        private const int MaxShadowedDirectionalLightCount = 4;
        private static readonly int DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static readonly int DirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ShadowSettings _shadowSettings;
        
        private readonly CommandBuffer _buffer = new() { name = BufferName };
        
        private int _shadowedDirectionalLightCount;
        private readonly ShadowedDirectionalLight[] _shadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
        private readonly Matrix4x4[] _dirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount];

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
        
        public void Render()
        {
            if (_shadowedDirectionalLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            else
            {
                // 无阴影时创建一个 1x1 的临时阴影贴图，防止 WebGL 2.0 出现问题。
                _buffer.GetTemporaryRT(DirShadowAtlasId, 1, 1, 32, 
                    FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }
        }

        private void RenderDirectionalShadows()
        {
            int atlasSize = (int)_shadowSettings.directional.atlasSize;
            _buffer.GetTemporaryRT(DirShadowAtlasId, atlasSize, atlasSize, 32, 
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            _buffer.SetRenderTarget(DirShadowAtlasId, RenderBufferLoadAction.DontCare, 
                RenderBufferStoreAction.Store);
            _buffer.ClearRenderTarget(true, false, Color.clear);
            
            _buffer.BeginSample(BufferName);
            ExecuteBuffer();
            {
                int split = _shadowedDirectionalLightCount <= 1 ? 1 : 2;
                int tileSize = atlasSize / split;
                
                for (int i = 0; i < _shadowedDirectionalLightCount; i++)
                    RenderDirectionalShadowInAtlas(i, split, tileSize);
                
                _buffer.SetGlobalMatrixArray(DirShadowMatricesId, _dirShadowMatrices);
            }
            _buffer.EndSample(BufferName);
            ExecuteBuffer();
        }

        private void RenderDirectionalShadowInAtlas(int index, int split, int tileSize)
        {
            ShadowedDirectionalLight light = _shadowedDirectionalLights[index];
            var shadowDrawingSettings = new ShadowDrawingSettings(_cullingResults, light.visibleLightIndex,
                BatchCullingProjectionType.Orthographic);

            _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1,
                Vector3.zero, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData);
            
            shadowDrawingSettings.splitData = splitData;
            SetTileViewport(index, split, tileSize);
            _dirShadowMatrices[index] = projectionMatrix * viewMatrix;
            _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            
            ExecuteBuffer();
            _context.DrawShadows(ref shadowDrawingSettings);
        }

        private void SetTileViewport(int index, int split, int tileSize)
        {
            int x = index % split;
            int y = index / split;
            _buffer.SetViewport(new Rect(x * tileSize, y * tileSize, tileSize, tileSize));
        }

        public void Cleanup()
        {
            _buffer.ReleaseTemporaryRT(DirShadowAtlasId);
            ExecuteBuffer();
        }

        private void ExecuteBuffer() 
        {
            _context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }
    }
}