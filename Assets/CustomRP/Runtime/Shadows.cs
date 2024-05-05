using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Shadows
    {
        private struct ShadowedDirectionalLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
        }
        
        private const string BufferName = "Shadows";
        private const int MaxShadowedDirectionalLightCount = 4;
        private const int MaxCascadeCount = 4;
        
        private static readonly int DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        private static readonly int DirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        private static readonly int CascadeCountId = Shader.PropertyToID("_CascadeCount");
        private static readonly int CascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        private static readonly int CascadeDataId = Shader.PropertyToID("_CascadeData");
        private static readonly int ShadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
        
        private static readonly ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];
        private static readonly Matrix4x4[] DirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount * MaxCascadeCount];
        private static readonly Vector4[] CascadeCullingSpheres = new Vector4[MaxCascadeCount];
        private static readonly Vector4[] CascadeData = new Vector4[MaxCascadeCount];
        
        private ScriptableRenderContext _context;
        private CullingResults _cullingResults;
        private ShadowSettings _shadowSettings;
        
        private readonly CommandBuffer _buffer = new() { name = BufferName };
        
        private int _shadowedDirectionalLightCount;

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
            
            ShadowedDirectionalLights[_shadowedDirectionalLightCount] = new ShadowedDirectionalLight()
            {
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias
            };

            return new Vector2(light.shadowStrength,
                _shadowSettings.directional.cascadesCount * _shadowedDirectionalLightCount++);
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
                int tiles = _shadowedDirectionalLightCount * _shadowSettings.directional.cascadesCount;
                int split = tiles <= 1 ? 1 : (tiles <= 4 ? 2 : 4);
                int tileSize = atlasSize / split;
                
                for (int i = 0; i < _shadowedDirectionalLightCount; i++)
                    RenderDirectionalShadowInAtlas(i, split, tileSize);
                
                _buffer.SetGlobalInt(CascadeCountId, _shadowSettings.directional.cascadesCount);
                _buffer.SetGlobalVectorArray(CascadeCullingSpheresId, CascadeCullingSpheres);
                _buffer.SetGlobalVectorArray(CascadeDataId, CascadeData);
                _buffer.SetGlobalMatrixArray(DirShadowMatricesId, DirShadowMatrices);

                float f = 1f - _shadowSettings.directional.cascadeFade;
                _buffer.SetGlobalVector(ShadowDistanceFadeId, new Vector4(
                    1f / _shadowSettings.maxDistance,
                    1f / _shadowSettings.distanceFade,
                    1f / (1 - f * f)));
            }
            _buffer.EndSample(BufferName);
            ExecuteBuffer();
        }

        private void RenderDirectionalShadowInAtlas(int index, int split, int tileSize)
        {
            ShadowedDirectionalLight light = ShadowedDirectionalLights[index];
            var shadowDrawingSettings = new ShadowDrawingSettings(_cullingResults, light.visibleLightIndex,
                BatchCullingProjectionType.Orthographic);

            int cascadeCount = _shadowSettings.directional.cascadesCount;
            int tileOffset = index * cascadeCount;
            Vector3 ratios = _shadowSettings.directional.CascadeRatios;

            for (int i = 0; i < cascadeCount; ++i)
            {
                _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 
                    i, cascadeCount, ratios, 
                    tileSize, 0f,
                    out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
                    out ShadowSplitData splitData);
            
                shadowDrawingSettings.splitData = splitData;
                if (index == 0)
                {
                    // 只需要对第一束光这样做，因为所有光的级联都是等效的
                    // cullingSphere.xyz 表示球体的坐标，w 表示球体的半径
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }
                
                int tileIndex = tileOffset + i;
                Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
                DirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, split);
                _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            
                // _buffer.SetGlobalDepthBias(0f, 3f);
                ExecuteBuffer();
                _context.DrawShadows(ref shadowDrawingSettings);
                // _buffer.SetGlobalDepthBias(0f, 0f);
            }
        }

        private void SetCascadeData(int index, Vector4 cullingSphere, float tileSize)
        {
            float texelSize = 2f * cullingSphere.w / tileSize;
            cullingSphere.w *= cullingSphere.w;

            CascadeData[index] = new Vector4(1f / cullingSphere.w, texelSize * 1.4142136f);
            CascadeCullingSpheres[index] = cullingSphere;
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

            float scale = 1f / split;
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
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