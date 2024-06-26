﻿using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public partial class CameraRenderer
    {
        // 当 Pass 没有 LightMode 标签时，使用此标签值作为默认值。
        private static readonly ShaderTagId UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
        private static readonly ShaderTagId LitShaderTagId = new ShaderTagId("CustomLit");
        
        private ScriptableRenderContext _context;
        private Camera _camera;
        private bool _useDynamicBatching;
        private bool _useGPUInstancing;

        private readonly Lighting _lighting = new();
        private readonly CommandBuffer _buffer = new();
        private CullingResults _cullingResults;

        private string BufferName
        {
            get => _buffer.name;
            set => _buffer.name = value;
        }
        
        public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
        {
            _context = context;
            _camera = camera;
            _useDynamicBatching = useDynamicBatching;
            _useGPUInstancing = useGPUInstancing;

            PrepareBuffer();
            PrepareForSceneWindow();
            
            if (Cull(shadowSettings.maxDistance) == false)
                return;
            
            BeginSample();
            {
                _lighting.Setup(context, _cullingResults, shadowSettings);
            }
            EndSample();
            
            Setup();
            
            BeginSample();
            {
                DrawVisibleGeometry();
                DrawUnsupportedShaders();
                DrawGizmos();
            }
            EndSample();
            
            _lighting.Cleanup();
            Submit();
        }

        private bool Cull(float maxShadowDistance)
        {
            // 尝试获取剔除参数
            if (_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
            {
                p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
                _cullingResults = _context.Cull(ref p);     // 实际执行剔除
                return true;
            }

            return false;
        }
        
        private void Setup()
        {
            // 设置摄像机的全局着色器变量，例如视图投影矩阵 unity_MatrixVP。
            _context.SetupCameraProperties(_camera);
            ClearRenderTarget();
        }

        /** 在 SetupCameraProperties 之后调用，则使用 Clear 命令更高效。否则使用 Draw GL 命令清除。 */
        private void ClearRenderTarget()
        {
            CameraClearFlags flags = _camera.clearFlags;
            
            _buffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags <= CameraClearFlags.SolidColor,
                flags == CameraClearFlags.SolidColor ? _camera.backgroundColor.linear : Color.clear);
            
            ExecuteBuffer();
        }

        private void DrawVisibleGeometry()
        {
            DrawOpaque();

            // 该 Camera 参数仅为确定是否应该绘制天空盒（ClearFlag 字段）
            _context.DrawSkybox(_camera);

            DrawTransparent();
        }

        private void DrawOpaque()
        {
            SortingSettings sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };
            DrawingSettings drawingSettings = new DrawingSettings(UnlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = _useDynamicBatching,
                enableInstancing = _useGPUInstancing,
                perObjectData = PerObjectData.Lightmaps 
                                | PerObjectData.LightProbe 
                                | PerObjectData.LightProbeProxyVolume 
                                | PerObjectData.ShadowMask
                                | PerObjectData.OcclusionProbe
                                | PerObjectData.OcclusionProbeProxyVolume
            };
            drawingSettings.SetShaderPassName(1, LitShaderTagId);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
            
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void DrawTransparent()
        {
            SortingSettings sortingSettings = new SortingSettings(_camera)
            {
                criteria = SortingCriteria.CommonTransparent
            };
            DrawingSettings drawingSettings = new DrawingSettings(UnlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = _useDynamicBatching,
                enableInstancing = _useGPUInstancing,
                perObjectData = PerObjectData.Lightmaps 
                                | PerObjectData.LightProbe 
                                | PerObjectData.LightProbeProxyVolume 
                                | PerObjectData.ShadowMask
                                | PerObjectData.OcclusionProbe
                                | PerObjectData.OcclusionProbeProxyVolume
            };
            drawingSettings.SetShaderPassName(1, LitShaderTagId);
            FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.transparent);
            
            _context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void Submit()
        {
            _context.Submit();
        }

        private void BeginSample()
        {
            _buffer.BeginSample(BufferName);
            ExecuteBuffer();
        }
        
        private void EndSample()
        {
            _buffer.EndSample(BufferName);
            ExecuteBuffer();
        }
        
        private void ExecuteBuffer() 
        {
            // 执行 Buffer（从 Buffer 中复制命令但不清除它）
            _context.ExecuteCommandBuffer(_buffer);
            // 清除 Buffer，以便下次使用
            _buffer.Clear();
        }
    }
}