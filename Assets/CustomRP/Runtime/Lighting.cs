using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class Lighting
    {
        private const string BufferName = "Lighting";
        private const int MaxDirLightCount = 4;     // 最大方向光数量
        
        private static readonly int DirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
        private static readonly int DirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
        private static readonly int DirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
        
        private static readonly Vector4[] DirLightColors = new Vector4[MaxDirLightCount];
        private static readonly Vector4[] DirLightDirections = new Vector4[MaxDirLightCount];

        private readonly CommandBuffer _buffer = new() { name = BufferName };
        private readonly Shadows _shadows = new();
        private CullingResults _cullingResults;

        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            _cullingResults = cullingResults;
            
            _buffer.BeginSample(BufferName);
            {
                _shadows.Setup(context, _cullingResults, shadowSettings);
                SetupLights();
            }
            _buffer.EndSample(BufferName);
            
            context.ExecuteCommandBuffer(_buffer);
            _buffer.Clear();
        }

        private void SetupLights()
        {
            int dirLightCount = 0;
            NativeArray<VisibleLight> lights = _cullingResults.visibleLights;

            for (int i = 0; i < lights.Length; i++)
            {
                VisibleLight light = lights[i];
                if (light.lightType == LightType.Directional)
                    SetupDirectionalLight(dirLightCount++, ref light);

                if (dirLightCount >= MaxDirLightCount)
                    break;
            }

            _buffer.SetGlobalInt(DirLightCountId, dirLightCount);
            _buffer.SetGlobalVectorArray(DirLightColorsId, DirLightColors);
            _buffer.SetGlobalVectorArray(DirLightDirectionsId, DirLightDirections);
        }

        // VisibleLight 结构相当大，使用 ref 引用传递避免拷贝
        private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
        {
            DirLightColors[index] = visibleLight.finalColor;
            DirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        }
    }
}