using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class CustomRenderPipeline : RenderPipeline
    {
        private readonly bool _useDynamicBatching;
        private readonly bool _useGPUInstancing;
        
        private readonly CameraRenderer _renderer = new CameraRenderer();

        public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSrpBatching)
        {
            _useDynamicBatching = useDynamicBatching;
            _useGPUInstancing = useGPUInstancing;
            GraphicsSettings.useScriptableRenderPipelineBatching = useSrpBatching;
        }
        
        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            // keep empty
        }

        protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
        {
            // 暂时让一个 CameraRenderer 处理所有 Camera
            // 后续可以让不同的 CameraRenderer 处理不同的 Camera
            foreach (Camera camera in cameras)
            {
                _renderer.Render(context, camera, _useDynamicBatching, _useGPUInstancing);
            }
        }
    }
}
