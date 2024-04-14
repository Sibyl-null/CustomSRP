using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime
{
    public class CameraRenderer
    {
        private const string BufferName = "Render Camera";
        
        private ScriptableRenderContext _context;
        private Camera _camera;

        private readonly CommandBuffer _buffer = new() { name = BufferName };
        
        public void Render(ScriptableRenderContext context, Camera camera)
        {
            _context = context;
            _camera = camera;

            BeginSample();
            {
                Setup();
                DrawVisibleGeometry();
            }
            EndSample();
            
            Submit();
        }

        private void Setup()
        {
            // 设置摄像机的全局着色器变量，例如视图投影矩阵 unity_MatrixVP。
            _context.SetupCameraProperties(_camera);
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