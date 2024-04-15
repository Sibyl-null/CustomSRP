using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class CameraRenderer
    {
        private const string BufferName = "Render Camera";
        
        private ScriptableRenderContext _context;
        private Camera _camera;

        // name 必须与下面 BeginSample 和 EndSample 的名称相同
        private readonly CommandBuffer _buffer = new() { name = BufferName };
        
        public void Render(ScriptableRenderContext context, Camera camera)
        {
            _context = context;
            _camera = camera;
            
            Setup();
            
            BeginSample();
            {
                DrawVisibleGeometry();
            }
            EndSample();
            
            Submit();
        }
        
        private void Setup()
        {
            // 设置摄像机的全局着色器变量，例如视图投影矩阵 unity_MatrixVP。
            _context.SetupCameraProperties(_camera);
            
            // 在 SetupCameraProperties 之后调用，则使用 Clear 命令更高效。否则使用 Draw GL 命令清除。
            _buffer.ClearRenderTarget(true, true, Color.clear);
            ExecuteBuffer();
        }

        private void DrawVisibleGeometry()
        {
            // 该 Camera 参数仅为确定是否应该绘制天空盒（ClearFlag 字段）
            _context.DrawSkybox(_camera);
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