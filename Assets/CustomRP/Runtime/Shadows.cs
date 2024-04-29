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

        /**
         * 尝试为指定方向光在阴影图集中保留空间，并存储渲染它们所需的信息
         * 返回 Vector2, x 表示光源的阴影强度，y 表示在阴影图集中的索引
         */
        public Vector2 ReserveDirectionalShadow(Light light, int visibleLightIndex)
        {
            if (_shadowedDirectionalLightCount >= MaxShadowedDirectionalLightCount)
                return Vector2.zero;

            if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
                return Vector2.zero;
            
            // 如果光源影响了至少一个阴影投射对象，则返回 true
            if (_cullingResults.GetShadowCasterBounds(visibleLightIndex, out _) == false)
                return Vector2.zero;
            
            _shadowedDirectionalLights[_shadowedDirectionalLightCount] = new ShadowedDirectionalLight()
            {
                visibleLightIndex = visibleLightIndex
            };
            
            return new Vector2(light.shadowStrength, _shadowedDirectionalLightCount++);
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
            Vector2 offset = SetTileViewport(index, split, tileSize);
            _dirShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, split);
            _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            
            ExecuteBuffer();
            _context.DrawShadows(ref shadowDrawingSettings);
        }

        private Vector2 SetTileViewport(int index, int split, int tileSize)
        {
            Vector2 offset = new Vector2(index % split, index / split);
            _buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
            return offset;
        }

        private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            Matrix4x4 matrix1 = new Matrix4x4(new Vector4(0.5f, 0.0f, 0.0f, 0.5f),
                                             new Vector4(0.0f, 0.5f, 0.0f, 0.5f),
                                             new Vector4(0.0f, 0.0f, 0.5f, 0.5f),
                                             new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            float scale = 1.0f / split;
            Matrix4x4 matrix2 = new Matrix4x4(new Vector4(scale, 0f, 0f, scale * offset.x),
                                            new Vector4(0f, scale, 0f, scale * offset.y),
                                            new Vector4(0f, 0f, 1f, 0f),
                                            new Vector4(0f, 0f, 0f, 1f));
            
            m = matrix2 * matrix1 * m;
            return m;
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